using DrJaw.Models;

namespace DrJaw.Services
{
    public sealed class UserLoginResult
    {
        public MSSQLUser User { get; set; } = default!;
        public MSSQLMart? Mart { get; set; }
        public string? AdminPassword { get; set; }
    }
}
