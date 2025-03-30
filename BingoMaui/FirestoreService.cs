using Firebase.Auth;
using Google.Cloud.Firestore;
// using AndroidX.Annotations;

namespace BingoMaui.Services
{

    // Här skapas alla firestoreDB aktioner
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly FirebaseAuthProvider _authProvider;
        // private readonly string playerColor = "";

        public FirestoreService()
        {
            try
            {
                string jsonPath = Path.Combine(FileSystem.AppDataDirectory, "credentials", "bingomaui28990.json");
                Console.WriteLine($"Försöker använda JSON-nyckeln på: {jsonPath}");
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);

                if (Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") == null)
                {
                    Console.WriteLine("Miljövariabeln GOOGLE_APPLICATION_CREDENTIALS är inte inställd.");
                }
                else
                {
                    Console.WriteLine("Miljövariabeln GOOGLE_APPLICATION_CREDENTIALS är korrekt inställd.");
                    Console.WriteLine($"Environment Variable Path: {Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")}");
                    string envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                    Console.WriteLine($"Environment Variable Path: {envPath}");
                    if (!File.Exists(envPath))
                    {
                        Console.WriteLine("Credential file not found at the specified path!");
                    }

                }

                // Initiera Firestore-klienten
                _firestoreDb = FirestoreDb.Create("bingomaui-28990");
                _authProvider = new FirebaseAuthProvider(new FirebaseConfig("YOUR_API_KEY"));
                Console.WriteLine("Firestore-databasen är ansluten!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AppDataDirectory: {FileSystem.AppDataDirectory}");
                Console.WriteLine($"Fel vid anslutning till Firestore: {ex.Message}");
                throw;
            }

        }

        // Lägg till ett nytt BingoGame
        public async Task CreateBingoGameAsync(BingoGame bingoGame)
        {
            try
            {
                var collection = _firestoreDb.Collection("BingoGames");
                await collection.AddAsync(bingoGame);
                Console.WriteLine($"Bingo game {bingoGame.GameName} created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating game: {ex.Message}");
            }
        }



        // Hämta alla BingoGames
        /*public async Task<List<BingoGame>> GetBingoGamesAsync()
        {
            var games = new List<BingoGame>();

            try
            {
                var collection = _firestoreDb.Collection("bingoGames");
                var querySnapshot = await collection.GetSnapshotAsync();

                foreach (var document in querySnapshot.Documents)
                {
                    var game = document.ToObject<BingoGame>();
                    games.Add(game);
                }

                Console.WriteLine($"Fetched {games.Count} games.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching games: {ex.Message}");
            }

            return games;
        }*/

        // Uppdatera status på ett BingoGame
        public async Task UpdateBingoGameStatusAsync(string gameId, string newStatus)
        {
            try
            {
                var documentRef = _firestoreDb.Collection("BingoGames").Document(gameId);
                await documentRef.UpdateAsync("Status", newStatus);
                Console.WriteLine($"Bingo game {gameId} status updated to {newStatus}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating game status: {ex.Message}");
            }
        }

        // Ta bort ett BingoGame
        public async Task DeleteBingoGameAsync(string gameId)
        {
            try
            {
                var documentRef = _firestoreDb.Collection("BingoGames").Document(gameId);
                await documentRef.DeleteAsync();
                Console.WriteLine($"Bingo game {gameId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting game: {ex.Message}");
            }
        }
        public async Task<BingoGame> GetGameByInviteCodeAsync(string inviteCode)
        {
            var collection = _firestoreDb.Collection("BingoGames");
            var query = collection.WhereEqualTo("InviteCode", inviteCode);
            var querySnapshot = await query.GetSnapshotAsync();

            if (querySnapshot.Documents.Count > 0)
            {
                try
                {
                    var document = querySnapshot.Documents[0];
                    var bingoGame = document.ConvertTo<BingoGame>();
                    bingoGame.DocumentId = document.Id; // Tilldela dokumentets ID

                    return bingoGame;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"🔥 Error parsing BingoGame from Firestore: {ex.Message}");
                    throw;
                }

            }

            return null; // Om spelet inte hittades
        }
        public async Task AddPlayerToGameAsync(string documentId, string playerId, string gameName, string playerColor, string nickname)
        {
            var docRef = _firestoreDb.Collection("BingoGames").Document(documentId);

            // Skapa eller uppdatera PlayerInfo
            var updateData = new Dictionary<string, object>
            {
                { $"PlayerInfo.{playerId}", new Dictionary<string, object>
                    {
                        { "Color", playerColor },
                        { "Points", 0 },
                        { "Nickname", nickname }
                    }
                },// ✅ Lägg till användaren i PlayerIds-arrayen
                { "PlayerIds", FieldValue.ArrayUnion(playerId) } // ✅ Slås ihop här
            };
            
            // Uppdatera Firestore med merge-liknande uppdatering
            await docRef.UpdateAsync(updateData);

            Console.WriteLine($"✅ Player {playerId} added to game '{gameName}' in PlayerInfo with color {playerColor} and 0 points.");
        }
        public async Task<List<Dictionary<string, object>>> GetRandomChallengesAsync(int count)
        {
            var challengesCollection = _firestoreDb.Collection("Challenges");
            var snapshot = await challengesCollection.GetSnapshotAsync();

            // Konvertera dokument till en lista
            var allChallenges = snapshot.Documents.Select(doc => doc.ToDictionary()).ToList();

            // Kontrollera om det finns färre än det begärda antalet
            if (allChallenges.Count < count)
            {
                Console.WriteLine($"Endast {allChallenges.Count} utmaningar tillgängliga. Fyller inte hela bingobrickan.");
            }

            // Slumpa utmaningar
            var random = new Random();
            return allChallenges.OrderBy(x => random.Next()).Take(Math.Min(count, allChallenges.Count)).ToList();
        }
        public async Task<List<BingoGame>> GGetGamesForUserAsync(string userId)
        {
            var gamesRef = _firestoreDb.Collection("BingoGames");
            var snapshot = await gamesRef.GetSnapshotAsync();

            var games = new List<BingoGame>();
            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();
                if (data.ContainsKey("PlayerInfo") && data["PlayerInfo"] is Dictionary<string, object> pi && pi.ContainsKey(userId))
                {
                    var game = doc.ConvertTo<BingoGame>();
                    game.DocumentId = doc.Id;
                    games.Add(game);
                }
            }

            return games;
        }
        public async Task<List<BingoGame>> GetGamesForUserAsync(string userId)
        {
            var gamesRef = _firestoreDb.Collection("BingoGames");
            var query = gamesRef.WhereArrayContains("PlayerIds", userId);
            var snapshot = await query.GetSnapshotAsync();

            var games = new List<BingoGame>();
            foreach (var doc in snapshot.Documents)
            {
                var game = doc.ConvertTo<BingoGame>();
                game.DocumentId = doc.Id;
                games.Add(game);
            }

            return games;
        }
        public List<Challenge> ConvertBingoCardsToChallenges(List<BingoCard> bingoCards)
        {
            var challenges = new List<Challenge>();

            foreach (var bingoCard in bingoCards)
            {
                challenges.Add(new Challenge
                {
                    ChallengeId = bingoCard.CardId, // Om du vill använda samma ID
                    Title = bingoCard.Title,
                    Description = bingoCard.Description,
                    Category = bingoCard.Category,
                    CompletedBy = bingoCard.CompletedBy ?? new List<CompletedInfo>()
                });
            }

            return challenges;
        }
        public async Task<BingoGame?> GetGameByIdAsync(string gameId)
        {
            var collection = _firestoreDb.Collection("BingoGames");
            var query = collection.WhereEqualTo("GameId", gameId);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                Console.WriteLine($"Game with ID {gameId} not found.");
                return null;
            }

            var doc = snapshot.Documents.First();
            // Här gör Firestore automapping till BingoGame och dess nested-objekt
            var game = doc.ConvertTo<BingoGame>();
            // Sätt manuellt DocumentId
            game.DocumentId = doc.Id;

            return game;
        }
        public async Task MarkChallengeAsCompletedAsync(string gameId, string title, string playerId, int pointUpdate = 1)
        {
            try
            {
                // Hämta spelet från Firestore
                var game = await GetGameByIdAsync(gameId);

                if (game == null || string.IsNullOrEmpty(game.DocumentId))
                {
                    Console.WriteLine($"Game with ID {gameId} not found or DocumentId is missing.");
                    return;
                }

                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
                var gameSnapshot = await gameRef.GetSnapshotAsync();

                if (!gameSnapshot.Exists)
                {
                    Console.WriteLine($"Game with DocumentId {game.DocumentId} not found.");
                    return;
                }

                var gameData = gameSnapshot.ToDictionary();
                if (!gameData.TryGetValue("Cards", out var cardsObject) || cardsObject is not List<object> cardsList)
                {
                    Console.WriteLine("Cards data is invalid.");
                    return;
                }

                // Hitta kortet med rätt titel
                var targetCard = cardsList
                    .OfType<Dictionary<string, object>>()
                    .FirstOrDefault(card => card.TryGetValue("Title", out var cardTitle) && cardTitle.ToString() == title);

                if (targetCard == null)
                {
                    Console.WriteLine($"Card with Title '{title}' not found in game {gameId}.");
                    return;
                }

                // ----- Ersätt den gamla PlayerProgress-logiken med CompletedBy -----

                // Hämta eller skapa CompletedBy-listan
                List<Dictionary<string, object>> completedByList;
                if (!targetCard.TryGetValue("CompletedBy", out var completedByObject) || !(completedByObject is List<object> rawCompletedBy))
                {
                    completedByList = new List<Dictionary<string, object>>();
                    targetCard["CompletedBy"] = completedByList;
                }
                else
                {
                    completedByList = new List<Dictionary<string, object>>();

                    // Försök iterera med en for-loop
                    for (int i = 0; i < rawCompletedBy.Count; i++)
                    {
                        try
                        {
                            var item = rawCompletedBy[i];
                            if (item == null)
                            {
                                Console.WriteLine($"Element {i} är null.");
                                continue;
                            }
                            Console.WriteLine($"Element {i} typ: {item.GetType().FullName}");
                            if (item is Dictionary<string, object> dict)
                            {
                                completedByList.Add(dict);
                            }
                            else
                            {
                                Console.WriteLine($"Element {i} har oväntad typ: {item.GetType().FullName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Fel vid bearbetning av element {i}: {ex.Message}");
                        }
                    }
                    // Sätt tillbaka den nya listan till targetCard så att vi jobbar med den
                    targetCard["CompletedBy"] = completedByList;
                }

                // Kontrollera om spelaren redan finns i listan
                bool playerAlreadyCompleted = completedByList.Any(entry =>
                    entry.ContainsKey("PlayerId") && entry["PlayerId"].ToString() == playerId);

                if (!playerAlreadyCompleted)
                {
                    // Här används en placeholder för spelarens färg.
                    // Ersätt med din logik för att hämta spelarens färg, exempelvis från en Player-modell.
                    string currentUserColor = "#FFFFFF"; // fallback

                    if (gameData.ContainsKey("PlayerInfo") && gameData["PlayerInfo"] is Dictionary<string, object> playerInfoDict)
                    {
                        if (playerInfoDict.TryGetValue(playerId, out var playerDataObj) && playerDataObj is Dictionary<string, object> playerData)
                        {
                            if (playerData.TryGetValue("Color", out var colorObj))
                            {
                                currentUserColor = colorObj.ToString();
                            }
                        }
                    }

                    var completedInfo = new Dictionary<string, object>
                    {
                        { "PlayerId", playerId },
                        { "UserColor", currentUserColor },
                        { "Nickname", game.PlayerInfo[playerId].Nickname }
                    };
                    completedByList.Add(completedInfo);

                    // Uppdatera kortet i Firestore med den nya CompletedBy-listan
                    await gameRef.UpdateAsync("Cards", cardsList);
                    Console.WriteLine($"Player {playerId} marked card with Title '{title}' as completed (using CompletedBy).");

                    // ----- Uppdatera PlayerInfo -----
                    Dictionary<string, object> playerInfo;
                    if (gameData.ContainsKey("PlayerInfo") && gameData["PlayerInfo"] is Dictionary<string, object> pi)
                    {
                        playerInfo = new Dictionary<string, object>(pi);
                    }
                    else
                    {
                        playerInfo = new Dictionary<string, object>();
                    }

                    if (playerInfo.ContainsKey(playerId))
                    {
                        if (playerInfo[playerId] is Dictionary<string, object> playerData)
                        {
                            // Hämta nuvarande poäng, öka med 1
                            int points = playerData.ContainsKey("Points") ? Convert.ToInt32(playerData["Points"]) : 0;
                            // pointUpdate innehåller värdet som brickan är värd att markera
                            points += pointUpdate;
                            playerData["Points"] = points;

                            playerInfo[playerId] = playerData;
                        }
                    }
                    else
                    {
                        // Om spelaren inte finns, lägg till med initiala värden
                        playerInfo[playerId] = new Dictionary<string, object>
                    {
                        { "Color", App.CurrentUserProfile.PlayerColor },
                        { "Points", 1 }
                    };
                    }

                    // Uppdatera PlayerInfo-fältet i Firestore
                    await gameRef.UpdateAsync("PlayerInfo", playerInfo);
                    Console.WriteLine($"PlayerInfo updated for player {playerId}.");
                }



                // ----- Uppdatera lokal cache -----
                if (!App.CompletedChallengesCache.ContainsKey(gameId))
                {
                    App.CompletedChallengesCache[gameId] = new Dictionary<string, List<string>>();
                }

                if (!App.CompletedChallengesCache[gameId].ContainsKey(title))
                {
                    App.CompletedChallengesCache[gameId][title] = new List<string>();
                }

                var nickname = game.PlayerInfo[playerId].Nickname;
                if (!App.CompletedChallengesCache[gameId][title].Contains(nickname))
                {
                    App.CompletedChallengesCache[gameId][title].Add(nickname);
                    Console.WriteLine($"Player {nickname} added to cache for challenge '{title}'.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating challenge progress: {ex.Message}");
            }
        }
        public async Task UnmarkChallengeAsCompletedAsync(string gameId, string title, string playerId)
        {
            var game = await GetGameByIdAsync(gameId);
            if (game == null || string.IsNullOrEmpty(game.DocumentId)) return;

            var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
            var gameSnapshot = await gameRef.GetSnapshotAsync();
            var gameData = gameSnapshot.ToDictionary();

            if (!gameData.TryGetValue("Cards", out var cardsObj) || cardsObj is not List<object> cardsList)
                return;

            foreach (var cardObj in cardsList.OfType<Dictionary<string, object>>())
            {
                if (cardObj.TryGetValue("Title", out var t) && t.ToString() == title &&
                    cardObj.TryGetValue("CompletedBy", out var cb) && cb is List<object> cbList)
                {
                    var updatedList = cbList
                        .OfType<Dictionary<string, object>>()
                        .Where(entry => entry.TryGetValue("PlayerId", out var pid) && pid.ToString() != playerId)
                        .ToList();

                    cardObj["CompletedBy"] = updatedList;
                    break;
                }
            }

            // Minska poäng i PlayerInfo
            if (gameData.TryGetValue("PlayerInfo", out var playerInfoObj) &&
                playerInfoObj is Dictionary<string, object> playerInfoDict &&
                playerInfoDict.TryGetValue(playerId, out var playerStatsObj) &&
                playerStatsObj is Dictionary<string, object> statsDict &&
                statsDict.TryGetValue("Points", out var pointsObj))
            {
                int points = Convert.ToInt32(pointsObj);
                statsDict["Points"] = Math.Max(points - 1, 0); // undvik negativa poäng
                playerInfoDict[playerId] = statsDict;
            }

            await gameRef.UpdateAsync(new Dictionary<string, object>
            {
                { "Cards", cardsList },
                { "PlayerInfo", gameData["PlayerInfo"] }
            });

            Console.WriteLine($"Removed {playerId} from CompletedBy for '{title}' and updated PlayerInfo.");
        }
        public async Task UpdatePlayerColorInGameAsync(string gameId, string playerId, string newColor)
        {
            var game = await GetGameByIdAsync(gameId);
            if (game == null || string.IsNullOrEmpty(game.DocumentId))
                return;

            var docRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);

            // 1. Uppdatera PlayerInfo (huvudfärg för spelaren i spelet)
            if (game.PlayerInfo != null && game.PlayerInfo.ContainsKey(playerId))
            {
                game.PlayerInfo[playerId].Color = newColor;
            }
            if (game.PlayerInfo == null)
            {
                Console.WriteLine("PlayerInfo not found in game DB");
                return;
            }
            if (game.Cards == null)
            {
                Console.WriteLine("Cards not found in game DB");
                return;
            }
            // 2. Uppdatera CompletedBy-fältet i varje kort där spelaren finns
            bool cardsModified = false;

            if (game.Cards != null)
            {
                foreach (var card in game.Cards)
                {
                    if (card.CompletedBy != null)
                    {
                        var entry = card.CompletedBy.FirstOrDefault(c => c.PlayerId == playerId);
                        if (entry != null)
                        {
                            entry.UserColor = newColor;
                            cardsModified = true;
                            
                        }
                    }
                }
            }

            // 3. Spara ändringarna till Firestore
            var updates = new Dictionary<string, object>
            {
                { "PlayerInfo", game.PlayerInfo }
            };

            if (cardsModified)
            {
                updates["Cards"] = game.Cards;
                App.ShouldRefreshChallenges = true;
            }

            await docRef.UpdateAsync(updates);
            Console.WriteLine($"Spelarens färg uppdaterad till '{newColor}' i PlayerInfo och CompletedBy för spelet {gameId}.");
        }
        public async Task<List<Comment>> GetCommentsAsync(string gameId)
        {
            var comments = new List<Comment>();
            // Hämta spelet med hjälp av GetGameByIdAsync
            var game = await GetGameByIdAsync(gameId);
            var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
            var querySnapshot = await gameRef.Collection("Comments")
                                             .OrderBy("Timestamp")
                                             .GetSnapshotAsync();

            foreach (var doc in querySnapshot.Documents)
            {
                var comment = doc.ConvertTo<Comment>();
                comments.Add(comment);
            }

            return comments;
        }
        public async Task PostCommentAsync(string gameId, string userId, string message)
        {
            try
            {
                // Förbered kommentarsdatan
                var comment = new Dictionary<string, object>
                {
                    { "UserId", userId },
                    { "Nickname", App.CurrentUserProfile.Nickname },
                    { "Message", message },
                    { "Timestamp", Timestamp.GetCurrentTimestamp() }
                };

                // Hämta spelet med hjälp av GetGameByIdAsync
                var game = await GetGameByIdAsync(gameId);
                if (game == null || string.IsNullOrEmpty(game.DocumentId))
                {
                    Console.WriteLine($"Game with ID {gameId} not found or missing DocumentId.");
                    return;
                }

                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
                await gameRef.Collection("Comments").AddAsync(comment);

                Console.WriteLine("Comment posted successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting comment: {ex.Message}");
            }
        }
        public async Task SetUserAsync(string userId, string email, string nickname)
        {
            var userRef = _firestoreDb.Collection("users").Document(userId);

            var snapshot = await userRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                var newUserProfile = new UserProfile
                {
                    UserId = userId,
                    Email = email,
                    Nickname = nickname,
                    PlayerColor = "#FF5733" // Default färg
                }; 

                await userRef.SetAsync(newUserProfile);
            }
        }
        public async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            try
            {
                var docRef = _firestoreDb.Collection("users").Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    return snapshot.ConvertTo<UserProfile>();
                }
                else
                {
                    Console.WriteLine($"User profile with ID {userId} not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user profile: {ex.Message}");
                return null;
            }
        }
        public async Task<string> GetUserNicknameAsync(string userId)
        {
            var userRef = _firestoreDb.Collection("users").Document(userId);
            var snapshot = await userRef.GetSnapshotAsync();

            if (snapshot.Exists && snapshot.ContainsField("Nickname"))
            {
                return snapshot.GetValue<string>("Nickname");
            }

            return userId; // Fallback till UserId om Nickname inte finns
        }
        public string FormatTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;

            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.Seconds} sekund{(timeSpan.Seconds > 1 ? "er" : "")} sedan";
            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes} minut{(timeSpan.Minutes > 1 ? "er" : "")} sedan";
            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours} timm{(timeSpan.Hours == 1 ? "e" : "ar")} sedan";
            if (timeSpan.TotalDays < 30)
                return $"{timeSpan.Days} dag{(timeSpan.Days > 1 ? "ar" : "")} sedan";

            return timestamp.ToString("yyyy-MM-dd HH:mm");
        }
        public async Task UpdatePlayerColorAsync(string userId, string newColor)
        {
            var docRef = _firestoreDb.Collection("users").Document(userId);
            await docRef.UpdateAsync("PlayerColor", newColor);
        }
        public async Task UpdateUserNicknameAsync(string userId, string newNickname)
        {
            try
            {
                // Uppdatera användarnamn i "users" samlingen
                var userRef = _firestoreDb.Collection("users").Document(userId);
                await userRef.UpdateAsync(new Dictionary<string, object> { { "Nickname", newNickname } });

                Console.WriteLine($"Nickname updated for user {userId}");

                // Hämta alla BingoGames
                var gamesRef = _firestoreDb.Collection("BingoGames");
                var gamesSnapshot = await gamesRef.GetSnapshotAsync();

                foreach (var gameDoc in gamesSnapshot.Documents)
                {
                    var gameData = gameDoc.ToDictionary();

                    // --- Uppdatera kommentarer ---
                    var commentsRef = gameDoc.Reference.Collection("Comments");
                    var commentsSnapshot = await commentsRef.GetSnapshotAsync();

                    var batch = _firestoreDb.StartBatch();
                    bool hasChanges = false;

                    foreach (var commentDoc in commentsSnapshot.Documents)
                    {
                        var commentData = commentDoc.ToDictionary();

                        if (commentData.ContainsKey("UserId") && commentData["UserId"].ToString() == userId)
                        {
                            batch.Update(commentDoc.Reference, new Dictionary<string, object> { { "Nickname", newNickname } });
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        await batch.CommitAsync();
                        Console.WriteLine($"Updated nickname for user {userId} in comments of BingoGame {gameDoc.Id}");
                    }

                    // --- Uppdatera PlayerInfo ---
                    if (gameData.TryGetValue("PlayerInfo", out var playerInfoObj) && playerInfoObj is Dictionary<string, object> playerInfo)
                    {
                        if (playerInfo.TryGetValue(userId, out var playerDataObj) && playerDataObj is Dictionary<string, object> playerData)
                        {
                            playerData["Nickname"] = newNickname;
                            playerInfo[userId] = playerData;

                            await gameDoc.Reference.UpdateAsync("PlayerInfo", playerInfo);
                            Console.WriteLine($"Updated PlayerInfo nickname in game {gameDoc.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating nickname across system: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, int>> GetLeaderboardAsync(string gameId)
        {
            // Hämta spelet
            var game = await GetGameByIdAsync(gameId);
            if (game == null)
            {
                Console.WriteLine($"Game with ID {gameId} not found.");
                return new Dictionary<string, int>();
            }

            // Skapa en dictionary för leaderboarden
            Dictionary<string, int> leaderboard = new Dictionary<string, int>();

            // Om PlayerInfo finns, iterera över den och hämta poängen
            if (game.PlayerInfo != null)
            {
                foreach (var kvp in game.PlayerInfo)
                {
                    // kvp.Key = playerId, kvp.Value = spelarens data (t.ex. en Dictionary<string, object>)
                    // Om vi använder PlayerStats-klassen
                    if (kvp.Value is PlayerStats stats)
                    {
                        leaderboard[kvp.Key] = stats.Points;
                    }
                    else
                    {
                        leaderboard[kvp.Key] = 0;
                    }
                }
            }
            else if (game.PlayerInfo != null)
            {
                // Fallback: om ingen PlayerInfo finns, sätt 0 poäng för varje spelare
                foreach (var kvp in game.PlayerInfo)
                {
                    var playerId = kvp.Key;
                    var playerStats = kvp.Value;

                    if (!leaderboard.ContainsKey(playerId))
                    {
                        leaderboard[playerId] = playerStats?.Points ?? 0;
                    }
                }
            }

            return leaderboard;
        }
        public async Task UpdateUserProfileAsync(UserProfile profile)
        {
            try
            {
                var userDoc = _firestoreDb.Collection("users").Document(profile.UserId);
                await userDoc.SetAsync(profile, SetOptions.MergeAll);
                Console.WriteLine($"User profile for {profile.UserId} updated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user profile: {ex.Message}");
                throw;
            }
        }
        //FUNKTION FÖR BETALTJÄNST FIREBASE
        //public async Task<string> UploadProfileImageAsync(string userId, Stream imageStream)
        //{
        //    try
        //    {
        //        // FirebaseStorage kräver ett separat paket, t.ex. Plugin.Firebase.Storage (eller REST via HttpClient)
        //        var storage = new FirebaseStorage("your-app.appspot.com"); // byt till din Storage URL

        //        var imageName = $"profile_images/{userId}_{Guid.NewGuid()}.jpg";

        //        var imageUrl = await storage
        //            .Child(imageName)
        //            .PutAsync(imageStream);

        //        Console.WriteLine($"✅ Profilbild uppladdad: {imageUrl}");
        //        return imageUrl;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error uploading profile image: {ex.Message}");
        //        return null;
        //    }
        //}


    }
}