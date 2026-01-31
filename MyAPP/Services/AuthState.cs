namespace MyAPP.Services
{
    public static class AuthState
    {
        private static System.String? _accessToken;
        private static System.Guid? _userId; 

        public static void SetToken(System.String? token)
        {
            _accessToken = token;
        }

        public static System.String? GetToken()
        {
            return _accessToken;
        }

        public static void SetUser(System.Guid? userId)
        {
            _userId = userId;
        }

        public static System.Guid? GetUser()
        {
            return _userId;
        }

        public static void ClearToken()
        {
            _accessToken = null;
            _userId = null;
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