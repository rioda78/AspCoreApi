using AspCoreApi.Helpers;

namespace AspCoreApi.Filters
{
    public class UserFilter
    {
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
