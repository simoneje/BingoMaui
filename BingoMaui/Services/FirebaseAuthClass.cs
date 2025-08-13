using Firebase.Auth;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace BingoMaui.Services
{
    public class FirebaseAuthService
    {
        public FirebaseAuthService()
        {

        }
        public string GetLoggedInNickname()
        {
            if (App.CurrentUserProfile.Nickname == null)
            {
                return "Anonym";
            }
            return App.CurrentUserProfile.Nickname; // Default till "Anonym Användare" om inget finns
        }


        // Metod för att skapa en ny användare (registrera sig)
        public async Task<string> RegisterUserAsync(string email, string password, string nickname = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nickname))
                    nickname = email.Split('@')[0];

                var request = new
                {
                    Email = email,
                    Password = password,
                    Nickname = nickname
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await BackendServices.HttpClient.PostAsync("https://backendbingoapi.onrender.com/api/auth/register", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Register failed: {errorMsg}");
                    return $"Error: {errorMsg}";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RegisterResponse>(responseJson);

                // Spara credentials lokalt
                await SecureStorage.SetAsync("UserId", result.UserId);
                await SecureStorage.SetAsync("IdToken", result.IdToken);
                await SecureStorage.SetAsync("IsLoggedIn", "true");

                BackendServices.UpdateToken(result.IdToken);

                return result.UserId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Registration error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }


        // Metod för att logga in en användare (e-post & lösenord)
        public async Task<string> LoginUserAsync(string email, string password)
        {
            try
            {
                var request = new
                {
                    Email = email,
                    Password = password
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await BackendServices.HttpClient.PostAsync("https://backendbingoapi.onrender.com/api/auth/login", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Login failed: {errorMsg}");
                    return $"Error: {errorMsg}";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LoginResponse>(responseJson);

                // Spara info lokalt
                await SecureStorage.SetAsync("UserId", result.LocalId);
                await SecureStorage.SetAsync("IdToken", result.IdToken);
                await SecureStorage.SetAsync("RefreshToken", result.RefreshToken);

                await SecureStorage.SetAsync("IsLoggedIn", "true");

                BackendServices.UpdateToken(result.IdToken);

                App.CurrentUserProfile ??= new UserProfile();
                App.CurrentUserProfile.UserId = result.LocalId;

                return result.IdToken;
            }
            catch (Exception ex)
            {
                return $"Error logging in: {ex.Message}";
            }
        }
        public static async Task<string> RefreshIdTokenAsync(string refreshToken)
        {
            try
            {
                var client = new HttpClient();
                var request = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };

                var content = new FormUrlEncodedContent(request);
                var response = await client.PostAsync(
                $"https://securetoken.googleapis.com/v1/token?key=AIzaSyCuGa8fDtOtjPUc8wQV0kJ1YFi21AY3nr8",
                content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("❌ Refresh token request failed.");
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var refreshData = JsonSerializer.Deserialize<RefreshTokenResponse>(responseJson);

                if (string.IsNullOrEmpty(refreshData?.id_token)) return null;

                await SecureStorage.SetAsync("IdToken", refreshData.id_token);
                if (!string.IsNullOrEmpty(refreshData.refresh_token))
                    await SecureStorage.SetAsync("RefreshToken", refreshData.refresh_token);

                return refreshData.id_token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception during token refresh: {ex.Message}");
                return null;
            }
        }

        public class RegisterResponse
        {
            [JsonPropertyName("userId")] 
            public string UserId { get; set; }
            [JsonPropertyName("idToken")] 
            public string IdToken { get; set; }
            // Lägg till om din backend returnerar den:
            [JsonPropertyName("refreshToken")] 
            public string? RefreshToken { get; set; }
        }
        public class RefreshTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string access_token { get; set; }

            [JsonPropertyName("expires_in")]
            public string expires_in { get; set; }

            [JsonPropertyName("token_type")]
            public string token_type { get; set; }

            [JsonPropertyName("refresh_token")]
            public string refresh_token { get; set; }

            [JsonPropertyName("id_token")]
            public string id_token { get; set; }

            [JsonPropertyName("user_id")]
            public string user_id { get; set; }

            [JsonPropertyName("project_id")]
            public string project_id { get; set; }
        }

        public class LoginResponse
        {
            [JsonPropertyName("localId")]
            public string LocalId { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("idToken")]
            public string IdToken { get; set; }

            [JsonPropertyName("refreshToken")]
            public string RefreshToken { get; set; }

            [JsonPropertyName("expiresIn")]
            public string ExpiresIn { get; set; }

            [JsonPropertyName("registered")]
            public bool Registered { get; set; }
        }
    }
}
