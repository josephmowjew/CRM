namespace UCS_CRM.Core.Helpers
{
    public class CursorParams
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public string SearchTerm { get; set; }
        public string SortColum { get; set; }
        public string SortDirection { get; set; }
    }
}
