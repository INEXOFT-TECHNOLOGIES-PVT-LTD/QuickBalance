namespace QuickBalance.API.DTOs
{
    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class CountryLookupItem : LookupItem
    {
        public int VatNoLimit { get; set; } = 15;
        public bool VatCharAllow { get; set; }
    }
}