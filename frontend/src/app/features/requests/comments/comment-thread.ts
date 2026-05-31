import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { CommentsService } from './comments.service';
import { CommentVm } from './comments.models';
import { AuthService } from '../../../core/auth/auth.service';

/** Discussion thread for a request: lists comments and posts new ones. Append-only — there is
 *  no edit/delete (matches the API). Author identity is the JWT sub; "You" is a display hint. */
@Component({
  selector: 'app-comment-thread',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './comment-thread.html',
  styleUrl: './comment-thread.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommentThread implements OnInit {
  readonly requestId = input.required<string>();

  private readonly service = inject(CommentsService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  protected readonly comments = signal<readonly CommentVm[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly posting = signal(false);
  protected readonly postError = signal(false);

  /** Comments with a display label resolved in the signal graph ("You" for the current user,
   *  otherwise a short id). Keeping it a computed (not a template method) keeps it reactive. */
  protected readonly labelledComments = computed(() =>
    this.comments().map((comment) => ({
      ...comment,
      authorLabel: comment.authorId === this.auth.userId() ? 'You' : comment.authorId.slice(0, 8),
    })),
  );

  protected readonly body = this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(4000)]);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.error.set(false);
    this.service
      .list(this.requestId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (comments) => {
          this.comments.set(comments);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  protected post(): void {
    if (this.posting() || this.body.invalid) {
      this.body.markAsTouched();
      return;
    }
    this.posting.set(true);
    this.postError.set(false);
    this.service
      .add(this.requestId(), this.body.value.trim())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (comment) => {
          this.comments.update((list) => [...list, comment]);
          this.body.reset('');
          this.posting.set(false);
        },
        error: () => {
          this.postError.set(true);
          this.posting.set(false);
        },
      });
  }
}
