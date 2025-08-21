namespace DrJaw.Models
{
    public sealed class AppConfig
    {
        public MssqlConfig Mssql { get; set; } = new();
    }

    public sealed class MssqlConfig
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string? PasswordEnc { get; set; } // пароль, зашифрованный DPAPI (base64)
    }
}
