﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace DrJaw.Services.Config
{
    public static class Secret
    {
        public static string Protect(string plain)
        {
            var bytes = Encoding.UTF8.GetBytes(plain ?? "");
            var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(enc);
        }

        public static string Unprotect(string? b64)
        {
            if (string.IsNullOrWhiteSpace(b64)) return "";
            try
            {
                var enc = Convert.FromBase64String(b64);
                var bytes = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch { return ""; }
        }
    }
}
