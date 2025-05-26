using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BingoMaui.Services
{
    internal class AccountServices
    {
        private const string CachePrefix = "cached_game_";
        public static void SaveGameToCache(BingoGame game)
        {
            var json = JsonSerializer.Serialize(game);
            Preferences.Set(CachePrefix + game.GameId, json);
        }
        public static BingoGame? LoadGameFromCache(string gameId)
        {
            var key = CachePrefix + gameId;
            if (!Preferences.ContainsKey(key)) return null;

            var json = Preferences.Get(key, "");
            return string.IsNullOrWhiteSpace(json) ? null :
                   JsonSerializer.Deserialize<BingoGame>(json);
        }
        public static void ClearGameCacheOnLogout()
        {
            var ids = Preferences.Get("cachedGameIds", "");
            var gameIds = ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id));

            foreach (var gameId in gameIds)
            {
                Preferences.Remove($"cachedGame_{gameId}");
            }

            Preferences.Remove("cachedGameIds");
        }
        public static void ClearAllUserData()
        {
            Preferences.Clear(); // detta tar bort *allt*
            SecureStorage.RemoveAll();
        }
        public static async Task LogoutAsync()
        {
            try
            {
                // 1. Rensa SecureStorage
                SecureStorage.Remove("IdToken");
                SecureStorage.Remove("UserId");
                SecureStorage.Remove("IsLoggedIn");

                // 2. Rensa appdata
                App.CurrentUserProfile = null;
                App.CompletedChallengesCache.Clear();
                AccountServices.ClearGameCacheOnLogout();

                // 3. Återställ backendtoken (valfritt)
                BackendServices.ResetToken();

                // 4. Navigera till inloggningssida
                Application.Current.MainPage = new NavigationPage(new MainPage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Logout error: {ex.Message}");
            }
        }

    }
}
