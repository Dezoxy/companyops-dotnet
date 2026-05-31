/** Raw API shape (CommentDto). Mapped to the view model below in the service. */
export interface CommentDto {
  readonly id: string;
  readonly requestId: string;
  readonly authorId: string;
  readonly body: string;
  readonly createdAtUtc: string;
}

// `requestId` is intentionally omitted — the thread component already has it via its input.
export interface CommentVm {
  readonly id: string;
  readonly authorId: string;
  readonly body: string;
  readonly createdAt: Date;
}
