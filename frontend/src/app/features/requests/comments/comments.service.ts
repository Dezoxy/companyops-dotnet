import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CommentDto, CommentVm } from './comments.models';

/** Map an API comment DTO → view model. */
export function mapComment(dto: CommentDto): CommentVm {
  return { id: dto.id, authorId: dto.authorId, body: dto.body, createdAt: new Date(dto.createdAtUtc) };
}

/** Owns the `/requests/{id}/comments` HTTP. The token is attached by the global interceptor. */
@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/requests`;

  list(requestId: string): Observable<CommentVm[]> {
    return this.http
      .get<CommentDto[]>(`${this.baseUrl}/${requestId}/comments`)
      .pipe(map((dtos) => dtos.map(mapComment)));
  }

  add(requestId: string, body: string): Observable<CommentVm> {
    return this.http.post<CommentDto>(`${this.baseUrl}/${requestId}/comments`, { body }).pipe(map(mapComment));
  }
}
