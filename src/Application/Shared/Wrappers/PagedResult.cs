namespace Application.Shared.Wrappers
{
    public class PagedResult<T> : ApiResult<IEnumerable<T>>
    {
        public PaginationMeta Pagination { get; set; } = new();

        public static PagedResult<T> Success(
            IEnumerable<T> data,
            int totalCount,
            int pageNumber,
            int pageSize,
            string message = "Operation successful.")
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 1;
            if (totalCount < 0) totalCount = 0;

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<T>
            {
                Succeeded = true,
                StatusCode = 200,
                Message = message,
                Data = data,
                Pagination = new PaginationMeta
                {
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNext = pageNumber < totalPages,
                    HasPrevious = pageNumber > 1
                }
            };
        }

        public static PagedResult<T> Fail(
            string message,
            int statusCode,
            int page,
            int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 1;

            return new PagedResult<T>
            {
                Succeeded = false,
                StatusCode = statusCode,
                Message = message,
                Data = Enumerable.Empty<T>(),
                Pagination = new PaginationMeta
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0,
                    HasNext = false,
                    HasPrevious = page > 1
                }
            };
        }

        public static PagedResult<T> Forbidden(
            string message,
            int page,
            int pageSize)
            => Fail(message, 403, page, pageSize);
    }

    public class PaginationMeta
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}
