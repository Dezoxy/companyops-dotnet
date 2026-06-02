/** A page of a list response: the items plus the unpaged total and the window that produced it.
 *  Mirrors the server's PagedResult<T>. Shared across features (requests, assets, …). */
export interface PagedResultDto<T> {
  readonly items: readonly T[];
  readonly total: number;
  readonly page: number;
  readonly pageSize: number;
}
