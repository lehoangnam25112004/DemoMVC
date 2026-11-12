namespace DemoMVC.Models
{
    public class StudentIndexViewModel
    {
        public IEnumerable<Student> Items { get; set; } = Enumerable.Empty<Student>();

        public string? SearchTerm { get; set; }

        public string SortBy { get; set; } = "StudentCode";

        public string SortDirection { get; set; } = "asc";

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalItems { get; set; }

        public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);

        public int[] AvailablePageSizes { get; } = [10, 20, 50];
    }
}
