namespace DrJaw.Models
{
    public static class Storage
    {
        private const string SettingsFilePath = "DrJaw.conf";
        public static List<MSSQLUser> Users { get; set; } = new List<MSSQLUser>();
        public static MSSQLUser? CurrentUser;
        public static List<MSSQLMart> Marts { get; set; } = new List<MSSQLMart>();
        public static MSSQLMart? CurrentMart;
        public static List<MSSQLMetal> Metals { get; set; } = new List<MSSQLMetal>();
        public static MSSQLMetal? CurrentMetal;
        public static MSSQLRepository Repo { get; } = new MSSQLRepository();
        public static WooManager WooManager { get; set; }
        public static WooRepository WooRepo { get; set; }
    }
}
