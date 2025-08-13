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
        private const string IndexKey = "cached_games_index";
        private const int MaxCachedGames = 20;
        public static void SaveGameToCache(BingoGame game)
        {
            if (game == null || string.IsNullOrWhiteSpace(game.GameId)) return;

            var payload = JsonSerializer.Serialize(new CacheWrapper<BingoGame>
            {
                CachedAt = DateTimeOffset.UtcNow,
                Data = game
            });

            Preferences.Set(CachePrefix + game.GameId, payload);
            UpsertIndex(game.GameId);
            TrimCacheIfNeeded();
        }
        public static BingoGame? LoadGameFromCache(string gameId)
        {
            var key = CachePrefix + gameId;
            if (!Preferences.ContainsKey(key)) return null;

            var json = Preferences.Get(key, "");
            if (string.IsNullOrWhiteSpace(json)) return null;

            var wrapper = JsonSerializer.Deserialize<CacheWrapper<BingoGame>>(json);
            return wrapper?.Data;
        }
        // --- Logout/cleanup ---
        public static void ClearGameCacheOnLogout()
        {
            var ids = LoadIndex();
            foreach (var id in ids)
                Preferences.Remove(CachePrefix + id);

            Preferences.Remove(IndexKey);
        }

        public static void ClearAllUserData()
        {
            // OBS: om du har andra app-preferenser som inte ska rensas,
            // ta bort denna rad och rensa selektivt istället.
            Preferences.Clear();
            SecureStorage.RemoveAll();
        }
        public static async Task LogoutAsync()
        {
            try
            {
                // 1) Rensa SecureStorage (inkl. RefreshToken!)
                SecureStorage.Remove("IdToken");
                SecureStorage.Remove("RefreshToken"); // 🔐 viktig
                SecureStorage.Remove("UserId");
                SecureStorage.Remove("IsLoggedIn");

                // 2) Rensa appdata
                App.CurrentUserProfile = null;
                App.CompletedChallengesCache.Clear();
                ClearGameCacheOnLogout();

                // 3) Återställ backendtoken (om du har sådan metod)
                BackendServices.ResetToken();

                // 4) Navigera till login
                Application.Current.MainPage = new NavigationPage(new MainPage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Logout error: {ex.Message}");
            }
        }


        // --- Interna hjälpmetoder ---
        private static void UpsertIndex(string gameId)
        {
            var list = LoadIndex();
            list.Remove(gameId);
            list.Insert(0, gameId);
            SaveIndex(list);
        }

        private static void TrimCacheIfNeeded()
        {
            var list = LoadIndex();
            while (list.Count > MaxCachedGames)
            {
                var last = list[^1];
                list.RemoveAt(list.Count - 1);
                Preferences.Remove(CachePrefix + last);
            }
            SaveIndex(list);
        }
        private static List<string> LoadIndex()
        {
            var json = Preferences.Get(IndexKey, "[]");
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }

        private static void SaveIndex(List<string> ids)
        {
            Preferences.Set(IndexKey, JsonSerializer.Serialize(ids));
        }

        private class CacheWrapper<T>
        {
            public DateTimeOffset CachedAt { get; set; }
            public T? Data { get; set; }
        }
    }
}
