using System.Collections;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Regression guard for the shipped tracing config: <c>AddSource("RabbitMQ.Client*")</c>.
/// An architecture review flagged the <c>*</c> as a literal that captures nothing — it is
/// not. OpenTelemetry supports wildcards in <c>AddSource</c>, and the RabbitMQ.Client 7
/// ActivitySource is named <c>RabbitMQ.Client</c>, which the pattern matches. This test
/// builds a TracerProvider with that exact source string, fully approves a request (which
/// publishes RequestApproved to real RabbitMQ via the outbox relay), and asserts the broker
/// client span is captured. If a future client/SDK bump renames the source or drops wildcard
/// support, this goes red instead of silently losing RabbitMQ traces.
/// </summary>
[Collection("Integration")]
public sealed class ObservabilityTracingTests(ApiFactory factory)
{
    [Fact]
    public async Task PublishingToRabbitMq_EmitsAClientSpan_CapturedByTheWildcardSource()
    {
        var captured = new ConcurrentActivitySink();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("RabbitMQ.Client*") // the exact wildcard the API and Worker ship
            .AddInMemoryExporter(captured)
            .Build();

        // Fully approving a Procurement request enqueues RequestApproved to the outbox; the
        // API's relay then publishes it to RabbitMQ — that publish is the span we expect.
        await factory.FullyApproveRequestAsync();

        // The relay polls, so the publish is asynchronous; wait for the span to be exported.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline && !HasRabbitMqSpan(captured))
        {
            await Task.Delay(250);
        }

        tracerProvider.ForceFlush();
        Assert.True(HasRabbitMqSpan(captured),
            "expected a 'RabbitMQ.Client*' span — the wildcard source captured no broker spans");
    }

    private static bool HasRabbitMqSpan(IEnumerable<Activity> activities) =>
        activities.Any(a => a.Source.Name.StartsWith("RabbitMQ.Client", StringComparison.Ordinal));

    // The in-memory exporter Adds from the relay's publishing thread while the test reads
    // from its own; a plain List would race, so every access is guarded. Only Add, Count,
    // and enumeration (via snapshot) are exercised — the rest are not needed by the exporter.
    private sealed class ConcurrentActivitySink : ICollection<Activity>
    {
        private readonly List<Activity> _items = [];
        private readonly Lock _gate = new();

        public void Add(Activity item)
        {
            lock (_gate)
            {
                _items.Add(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _items.Count;
                }
            }
        }

        public IEnumerator<Activity> GetEnumerator()
        {
            lock (_gate)
            {
                return _items.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsReadOnly => false;
        public void Clear() => throw new NotSupportedException();
        public bool Contains(Activity item) => throw new NotSupportedException();
        public void CopyTo(Activity[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(Activity item) => throw new NotSupportedException();
    }
}
