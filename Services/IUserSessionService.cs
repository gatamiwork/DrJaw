using System;
using DrJaw.Models; // где лежит MSSQLUser

namespace DrJaw.Services
{
    public interface IUserSessionService
    {
        MSSQLUser? CurrentUser { get; }
        MSSQLMart? CurrentMart { get; }

        event EventHandler? Changed;

        void SignIn(MSSQLUser user, MSSQLMart? mart = null);
        void SignOut();

        bool IsInRole(string role);
    }
}
