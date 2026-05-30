# 7. Asynchronous messaging via RabbitMQ with a transactional outbox

Date: 2026-05-30
Status: Accepted

## Context

Phase 5 introduces asynchronous processing: when a request becomes `Approved`, the
system should react out-of-band (Phase 5 simulates a notification; later phases may
do more). This means the API must hand work to a separate **Worker** process over
**RabbitMQ** (the locked-stack broker).

The hard problem is the **dual write**: the approval is a row change in Postgres, and
the event is a message in RabbitMQ — two systems with no shared transaction. Publish
inline and the process can commit the DB change then die before publishing (lost
event), or publish then fail to commit (phantom event). Either way the two stores
disagree.

## Decision

**Publish integration events through a transactional outbox, consume them in a .NET
Worker, and treat delivery as at-least-once.**

1. **Producer (API) — write to an outbox in the same transaction.** A handler that
   produces an event does not call RabbitMQ. It enqueues an integration event through
   an `IIntegrationEventPublisher` port; the Infrastructure implementation serialises
   it into an `outbox_messages` row added to the **same `DbContext`**, so the state
   change and the event commit atomically via the existing unit of work. No event is
   ever lost relative to the state change, and none is published for a rolled-back one.

2. **Relay — a background poller publishes the outbox.** A hosted service polls
   `outbox_messages` for unprocessed rows, publishes each to RabbitMQ, and stamps
   `ProcessedAtUtc`. It runs co-located with the producer (the API host) for Phase 5.
   A crash between publish and stamp re-publishes on the next poll → **at-least-once**.

3. **Consumer (Worker) — idempotent, with retry.** `CompanyOps.Worker` consumes the
   queue and simulates the notification. Because delivery is at-least-once, consumers
   must tolerate duplicates (idempotent side effects). Transient failures are retried
   (nack/requeue with a bounded count); poison messages are dead-lettered rather than
   looping forever.

4. **Events are integration contracts, versioned by name.** The event type
   (`RequestApproved`) lives in Application and carries only ids + the essentials —
   not the EF entity — so producer and consumer evolve independently.

## Options considered

- **Publish inline, after commit** — simplest, but the dual-write gap loses events on
  a crash between commit and publish. Rejected as the baseline because the whole point
  of an audit-grade workflow engine is not silently dropping "request approved".
- **Distributed transaction (2PC) across Postgres + RabbitMQ** — correct but heavy,
  poorly supported, and operationally painful. Rejected.
- **Transactional outbox** — chosen: no lost events, no phantom events, and the only
  cost is at-least-once duplicates (handled by idempotent consumers). The standard
  enterprise answer to the dual-write problem.

## Consequences

**Positive**
- The approval and its event are atomic; the trail can't silently drop events.
- Producer and consumer are decoupled and independently deployable/scalable.
- The outbox doubles as a built-in publish audit (what was emitted, when).

**Negative / costs**
- More moving parts than an inline publish: an `outbox_messages` table, a relay poller,
  and idempotent consumers. Accepted deliberately — retrofitting reliability after an
  inline publish is worse.
- **At-least-once, not exactly-once:** consumers must be idempotent. Phase 5's
  simulated notification is naturally safe; real side effects (Phase 6+) need a dedupe
  key or idempotent downstream calls.
- Polling adds latency (poll interval) and DB load; a high-throughput system would use
  listen/notify or a CDC-based relay — out of scope here.

## Affects

- **Phase 5** — `outbox_messages` table + migration; `IIntegrationEventPublisher`
  (enqueue) port; `RequestApproved` event published when a request reaches `Approved`;
  a relay hosted service; `CompanyOps.Worker` consumer; RabbitMQ in compose;
  Testcontainers RabbitMQ round-trip tests.
- **Phase 6** — the worker's external-integration calls must be idempotent / retry-safe.
- **Phase 10/11** — relay/consumer metrics, dead-letter monitoring, broker hardening.
