import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';

import { CommentThread } from './comment-thread';
import { CommentsService } from './comments.service';
import { AuthService } from '../../../core/auth/auth.service';
import { CommentVm } from './comments.models';

function vm(authorId: string, body: string): CommentVm {
  return { id: body, authorId, body, createdAt: new Date('2026-05-01T00:00:00Z') };
}

// Narrow view over the component's protected members, so the test can drive it without `any`.
interface ThreadHarness {
  post(): void;
  body: { setValue(value: string): void };
}

function setupWith(service: CommentsService, userId = 'me') {
  const auth = { userId: () => userId } as unknown as AuthService;
  TestBed.configureTestingModule({
    imports: [CommentThread],
    providers: [
      provideNoopAnimations(),
      { provide: CommentsService, useValue: service },
      { provide: AuthService, useValue: auth },
    ],
  });
  const fixture = TestBed.createComponent(CommentThread);
  fixture.componentRef.setInput('requestId', 'r1');
  return fixture;
}

function setup(comments: CommentVm[], userId = 'me') {
  return setupWith({ list: () => of(comments), add: () => of(vm('me', 'posted')) } as unknown as CommentsService, userId);
}

describe('CommentThread', () => {
  it('shows the empty state when there are no comments', async () => {
    const fixture = setup([]);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No comments yet');
  });

  it('labels the current user\'s own comments as "You"', async () => {
    const fixture = setup([vm('me', 'mine'), vm('other-1234', 'theirs')]);
    await fixture.whenStable();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('You');
    expect(text).toContain('mine');
    expect(text).toContain('theirs');
  });

  it('shows an error state when loading fails', async () => {
    const fixture = setupWith({
      list: () => throwError(() => new Error('boom')),
      add: () => of(vm('me', 'posted')),
    } as unknown as CommentsService);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain("Couldn't load comments");
  });

  it('appends a posted comment to the thread', async () => {
    const fixture = setup([]);
    await fixture.whenStable();
    const harness = fixture.componentInstance as unknown as ThreadHarness;

    harness.body.setValue('Adding a note');
    harness.post();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('posted'); // the body the fake add returns
  });

  it('surfaces a post error without losing the thread', async () => {
    const fixture = setupWith({
      list: () => of([vm('me', 'existing')]),
      add: () => throwError(() => new Error('boom')),
    } as unknown as CommentsService);
    await fixture.whenStable();
    const harness = fixture.componentInstance as unknown as ThreadHarness;

    harness.body.setValue('will fail');
    harness.post();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain("Couldn't post");
    expect(text).toContain('existing'); // the loaded thread is still shown
  });
});
