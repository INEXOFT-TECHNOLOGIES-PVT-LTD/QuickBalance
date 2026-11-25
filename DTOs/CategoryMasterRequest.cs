namespace QuickBalance.API.DTOs
{
    public class CategoryMasterRequest
    {
        public string CategoryName { get; set; } = "";
        public string SecondName { get; set; } = "";
        public string Alias { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}