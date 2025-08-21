using System;
using DrJaw.Models;

namespace DrJaw.Services
{
    public sealed class UserSessionService : IUserSessionService
    {
        public MSSQLUser? CurrentUser { get; private set; }
        public MSSQLMart? CurrentMart { get; private set; }
        public event EventHandler? Changed;

        public void SignIn(MSSQLUser user, MSSQLMart? mart = null)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            CurrentMart = mart;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SignOut()
        {
            CurrentUser = null;
            CurrentMart = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool IsInRole(string role) =>
            string.Equals(CurrentUser?.Role, role, StringComparison.OrdinalIgnoreCase);
    }
}
