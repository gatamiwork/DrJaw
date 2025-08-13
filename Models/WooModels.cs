namespace DrJaw.Models
{
    public class WooProduct
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_modified { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string stock_status { get; set; }
        public string description { get; set; }
        public string short_description { get; set; }
        public string sku { get; set; }
        public string price { get; set; }
        public string regular_price { get; set; }
        public List<WooAttribute> attributes { get; set; } = new List<WooAttribute>();
        public List<WooProductTag> tags { get; set; } = new List<WooProductTag>();
        public List<int> variations { get; set; } = new List<int>();
        public List<WooImage> images { get; set; } = new List<WooImage>();
    }
    public class WooImage
    {
        public string src { get; set; }
    }
    public class WooProductTag
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
    }
    public class WooVariation
    {
        public int id { get; set; }
        public string sku { get; set; }
        public string price { get; set; }
        public DateTime date_created { get; set; }
        public string stock_status { get; set; }
        public WooImage image { get; set; }
        public List<WooVariationAttribute> attributes { get; set; }
    }
    public class WooVariationAttribute
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string option { get; set; }
    }
    public class WooCategory
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public int parent { get; set; }
        public string description { get; set; }
        public string display { get; set; }
    }
    public class WooAttribute
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string type { get; set; }
        public List<string> options { get; set; }
        public string order_by { get; set; }
        public bool has_archives { get; set; }
        public List<WooAttributeTerm> terms { get; set; } = new List<WooAttributeTerm>();

    }
    public class WooAttributeTerm
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
    }

}
