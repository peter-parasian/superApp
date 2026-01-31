using Sys = System;
using Net = System.Net.Http;
using Json = System.Text.Json;
using Sec = System.Security;
using Interop = System.Runtime.InteropServices;
using Tasks = System.Threading.Tasks;

namespace MyAPP.Services
{
    public sealed class SupabaseAuthService
    {
        private const Sys.String SUPABASE_URL = "https://owonxxmsnfsruymmlzga.supabase.co";
        private const Sys.String SUPABASE_ANON_KEY = "sb_publishable_JmWdf5RLIbStzNiN06vsMw_owqrDffB";

        private static readonly Net.HttpClient _sharedClient = new Net.HttpClient();

        public sealed class AuthResult
        {
            public Sys.Boolean IsSuccess { get; set; }
            public Sys.String? AccessToken { get; set; }
            public Sys.Guid? UserId { get; set; } 
            public Sys.String? ErrorMessage { get; set; }
        }

        public async Tasks.Task<AuthResult> SignInAsync(Sys.String email, Sec.SecureString password)
        {
            if (Sys.String.IsNullOrWhiteSpace(email))
            {
                return new AuthResult() { IsSuccess = false, ErrorMessage = "Email cannot be empty." };
            }

            if (password == null || password.Length == 0)
            {
                return new AuthResult() { IsSuccess = false, ErrorMessage = "Password cannot be empty." };
            }

            Sys.IntPtr unmanagedString = Sys.IntPtr.Zero;
            Sys.String? plainPassword = null;

            try
            {
                unmanagedString = Interop.Marshal.SecureStringToGlobalAllocUnicode(password);
                plainPassword = Interop.Marshal.PtrToStringUni(unmanagedString);

                if (Sys.String.IsNullOrWhiteSpace(plainPassword))
                {
                    return new AuthResult() { IsSuccess = false, ErrorMessage = "Password is empty after processing." };
                }

                Sys.String endpoint = Sys.String.Concat(SUPABASE_URL.TrimEnd('/'), "/auth/v1/token?grant_type=password");

                var payload = new
                {
                    email = email,
                    password = plainPassword
                };

                Sys.String jsonPayload = Json.JsonSerializer.Serialize(payload);

                using var request = new Net.HttpRequestMessage(Net.HttpMethod.Post, endpoint);

                request.Content = new Net.StringContent(jsonPayload, Sys.Text.Encoding.UTF8, "application/json");

                request.Headers.Add("apikey", SUPABASE_ANON_KEY);
                request.Headers.Authorization = new Net.Headers.AuthenticationHeaderValue("Bearer", SUPABASE_ANON_KEY);

                using var response = await _sharedClient.SendAsync(request).ConfigureAwait(false);
                Sys.String responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode == false)
                {
                    return this.ParseErrorResponse(responseString, response.StatusCode);
                }

                return this.ParseSuccessResponse(responseString);
            }
            catch (Sys.Exception)
            {
                return new AuthResult() { IsSuccess = false, ErrorMessage = "Network or service error occurred." };
            }
            finally
            {
                if (unmanagedString != Sys.IntPtr.Zero)
                {
                    Interop.Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                    unmanagedString = Sys.IntPtr.Zero;
                }
                plainPassword = null;
            }
        }

        private AuthResult ParseErrorResponse(Sys.String responseString, Sys.Net.HttpStatusCode statusCode)
        {
            try
            {
                using var doc = Json.JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("error_description", out Json.JsonElement errDesc))
                {
                    return new AuthResult() { IsSuccess = false, ErrorMessage = errDesc.GetString() };
                }

                if (doc.RootElement.TryGetProperty("error", out Json.JsonElement err))
                {
                    return new AuthResult() { IsSuccess = false, ErrorMessage = err.GetString() };
                }
            }
            catch { }

            return new AuthResult() { IsSuccess = false, ErrorMessage = Sys.String.Concat("HTTP Error: ", statusCode.ToString()) };
        }

        private AuthResult ParseSuccessResponse(Sys.String responseString)
        {
            try
            {
                using var doc = Json.JsonDocument.Parse(responseString);

                Sys.String? accessToken = null;
                if (doc.RootElement.TryGetProperty("access_token", out Json.JsonElement tokenElement))
                {
                    accessToken = tokenElement.GetString();
                }

                Sys.Guid? userId = null;
                if (doc.RootElement.TryGetProperty("user", out Json.JsonElement userElement))
                {
                    if (userElement.TryGetProperty("id", out Json.JsonElement idElement))
                    {
                        if (Sys.Guid.TryParse(idElement.GetString(), out Sys.Guid parsedId))
                        {
                            userId = parsedId;
                        }
                    }
                }

                if (Sys.String.IsNullOrWhiteSpace(accessToken) == false)
                {
                    return new AuthResult() { IsSuccess = true, AccessToken = accessToken, UserId = userId };
                }
            }
            catch
            {
                return new AuthResult() { IsSuccess = false, ErrorMessage = "Failed to parse auth response." };
            }

            return new AuthResult() { IsSuccess = false, ErrorMessage = "No access token found in response." };
        }
    }
}