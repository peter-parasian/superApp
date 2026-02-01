using Sys = System;
using IO = System.IO;
using Json = System.Text.Json;
using Sec = System.Security.Cryptography;

namespace MyAPP.Services
{
    public static class AuthState
    {
        private static Sys.String? _accessToken;
        private static Sys.Guid? _userId;
        private static readonly Sys.String _sessionFilePath;

        static AuthState()
        {
            Sys.String appDir = IO.Path.Combine(
                Sys.Environment.GetFolderPath(Sys.Environment.SpecialFolder.LocalApplicationData),
                "MyAPP"
            );

            if (!IO.Directory.Exists(appDir))
            {
                IO.Directory.CreateDirectory(appDir);
            }

            _sessionFilePath = IO.Path.Combine(appDir, "session.dat");
        }

        public static void SetToken(Sys.String? token)
        {
            _accessToken = token;
        }

        public static Sys.String? GetToken()
        {
            return _accessToken;
        }

        public static void SetUser(Sys.Guid? userId)
        {
            _userId = userId;
        }

        public static Sys.Guid? GetUser()
        {
            return _userId;
        }

        public static void ClearToken()
        {
            _accessToken = null;
            _userId = null;
            DeleteSessionFile();
        }

        public static Sys.Boolean IsSignedIn()
        {
            if (_accessToken != null)
            {
                return true;
            }
            return false;
        }

        public static void SaveToDisk()
        {
            if (_accessToken == null || _userId == null)
            {
                return;
            }

            SessionData data = new SessionData
            {
                AccessToken = _accessToken,
                UserId = _userId.Value,
                ExpiryDate = Sys.DateTime.UtcNow.AddMonths(1)
            };

            try
            {
                Sys.String json = Json.JsonSerializer.Serialize(data);
                Sys.Byte[] bytes = Sys.Text.Encoding.UTF8.GetBytes(json);
                Sys.Byte[] encrypted = Sec.ProtectedData.Protect(bytes, null, Sec.DataProtectionScope.CurrentUser);
                IO.File.WriteAllBytes(_sessionFilePath, encrypted);
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public static Sys.Boolean TryLoadFromDisk()
        {
            if (!IO.File.Exists(_sessionFilePath))
            {
                return false;
            }

            try
            {
                Sys.Byte[] encrypted = IO.File.ReadAllBytes(_sessionFilePath);
                Sys.Byte[] bytes = Sec.ProtectedData.Unprotect(encrypted, null, Sec.DataProtectionScope.CurrentUser);
                Sys.String json = Sys.Text.Encoding.UTF8.GetString(bytes);
                SessionData? data = Json.JsonSerializer.Deserialize<SessionData>(json);

                if (data == null)
                {
                    return false;
                }

                if (data.ExpiryDate < Sys.DateTime.UtcNow)
                {
                    DeleteSessionFile();
                    return false;
                }

                _accessToken = data.AccessToken;
                _userId = data.UserId;
                return true;
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine($"Failed to load session: {ex.Message}");
                DeleteSessionFile();
                return false;
            }
        }

        private static void DeleteSessionFile()
        {
            try
            {
                if (IO.File.Exists(_sessionFilePath))
                {
                    IO.File.Delete(_sessionFilePath);
                }
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine($"Failed to delete session file: {ex.Message}");
            }
        }

        private sealed class SessionData
        {
            public Sys.String AccessToken { get; set; } = Sys.String.Empty;
            public Sys.Guid UserId { get; set; }
            public Sys.DateTime ExpiryDate { get; set; }
        }
    }
}