namespace MyAPP.Services
{
    public static class AuthState
    {
        private static System.String? _accessToken;

        public static void SetToken(System.String? token)
        {
            _accessToken = token;
        }

        public static System.String? GetToken()
        {
            return _accessToken;
        }

        public static void ClearToken()
        {
            _accessToken = null;
        }

        public static System.Boolean IsSignedIn()
        {
            if (_accessToken != null)
            {
                return true;
            }
            return false;
        }
    }
}