import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CommentsService, mapComment } from './comments.service';
import { CommentDto } from './comments.models';

function dto(): CommentDto {
  return { id: 'c1', requestId: 'r1', authorId: 'a1', body: 'hello', createdAtUtc: '2026-05-01T10:00:00Z' };
}

describe('mapComment', () => {
  it('parses the timestamp to a date', () => {
    expect(mapComment(dto()).createdAt).toEqual(new Date('2026-05-01T10:00:00Z'));
  });
});

describe('CommentsService', () => {
  let service: CommentsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CommentsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list GETs the request thread', () => {
    let count: number | undefined;
    service.list('r1').subscribe((c) => (count = c.length));
    const req = httpMock.expectOne('/api/requests/r1/comments');
    expect(req.request.method).toBe('GET');
    req.flush([dto()]);
    expect(count).toBe(1);
  });

  it('add POSTs the body to the thread', () => {
    service.add('r1', 'hello').subscribe();
    const req = httpMock.expectOne('/api/requests/r1/comments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ body: 'hello' });
    req.flush(dto());
  });
});
