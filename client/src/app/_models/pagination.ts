export interface Pagination {
    currentPage: number;
    itemsPerPage: number;
    totalItems: number;
    totalPages: number;
}

export class PaginattedResult<T> {
    result: T;
    pagination: Pagination;
}