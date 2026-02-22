namespace TPEmail.BusinessModels.ResponseModels
{
    public class DataPageResult<T>
    {
        public T Data { get; set; } = default!;
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public DataPageResult() { }

        public DataPageResult(T data, int pageNumber, int pageSize)
        {
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public static class DataPageResult
    {
        public static DataPageResult<List<T>> Create<T>(IList<T> data, ListDataRequest request, int totalRecords)
        {
            int totalPages = totalRecords > 0 ? (int)Math.Ceiling(totalRecords / (double)request.PageSize) : 0;
            return new DataPageResult<List<T>>(data.ToList(), request.PageNumber, request.PageSize)
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1
            };
        }
    }
}
