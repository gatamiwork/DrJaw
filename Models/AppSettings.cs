namespace DrJaw.Models
{
    public class AppSettings
    {
        public MSSQLConnectionSettings MSSQL { get; set; } = new();
        public WooConnectionSettings WooCommerce { get; set; } = new();
    }
    public class MSSQLConnectionSettings
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
    public class WooConnectionSettings
    {
        public string Domain { get; set; } = "";
        public string ConsumerKey { get; set; } = "";
        public string ConsumerSecret { get; set; } = "";
        public int SyncTime { get; set; } = 5;
    }
}
