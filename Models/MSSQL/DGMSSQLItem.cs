namespace DrJaw.Models
{
    public sealed class DGMSSQLItem
    {
        public bool IsSelected { get; set; }

        public string Type { get; set; } = "";
        public string Metal { get; set; } = "";
        public string Articul { get; set; } = "";

        public decimal Weight { get; set; }
        public string? Size { get; set; }
        public string? Stones { get; set; }
        public string? Comment { get; set; }
        public decimal Price { get; set; }
        public string? Manufacturer { get; set; }

        public List<int> Ids { get; } = new();
        public int ItemCount => Ids.Count;

    }
}
