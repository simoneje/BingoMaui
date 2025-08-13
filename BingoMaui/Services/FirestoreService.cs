//using Firebase.Auth;
//using Google.Cloud.Firestore;
//using Newtonsoft.Json;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Text.Json;
//// using AndroidX.Annotations;

//namespace BingoMaui.Services
//{

//    // Här skapas alla firestoreDB aktioner
//    public class FirestoreService
//    {
//        private readonly FirestoreDb _firestoreDb;
//        private readonly FirebaseAuthProvider _authProvider;
//        // private readonly string playerColor = "";

//        public FirestoreService()
//        {
//            try
//            {
//                string jsonPath = Path.Combine(FileSystem.AppDataDirectory, "credentials", "bingomaui28990.json");
//                Console.WriteLine($"Försöker använda JSON-nyckeln på: {jsonPath}");
//                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);

//                if (Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") == null)
//                {
//                    Console.WriteLine("Miljövariabeln GOOGLE_APPLICATION_CREDENTIALS är inte inställd.");
//                }
//                else
//                {
//                    Console.WriteLine("Miljövariabeln GOOGLE_APPLICATION_CREDENTIALS är korrekt inställd.");
//                    Console.WriteLine($"Environment Variable Path: {Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")}");
//                    string envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
//                    Console.WriteLine($"Environment Variable Path: {envPath}");
//                    if (!File.Exists(envPath))
//                    {
//                        Console.WriteLine("Credential file not found at the specified path!");
//                    }

//                }

//                // Initiera Firestore-klienten
//                _firestoreDb = FirestoreDb.Create("bingomaui-28990");
//                _authProvider = new FirebaseAuthProvider(new FirebaseConfig("YOUR_API_KEY"));
//                Console.WriteLine("Firestore-databasen är ansluten!");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"AppDataDirectory: {FileSystem.AppDataDirectory}");
//                Console.WriteLine($"Fel vid anslutning till Firestore: {ex.Message}");
//                throw;
//            }

//        }
//        public async Task<List<Dictionary<string, object>>> GetRandomChallengesAsync(int count)
//        {
//            var challengesCollection = _firestoreDb.Collection("Challenges");
//            var snapshot = await challengesCollection.GetSnapshotAsync();

//            // Konvertera dokument till en lista
//            var allChallenges = snapshot.Documents.Select(doc => doc.ToDictionary()).ToList();

//            // Kontrollera om det finns färre än det begärda antalet
//            if (allChallenges.Count < count)
//            {
//                Console.WriteLine($"Endast {allChallenges.Count} utmaningar tillgängliga. Fyller inte hela bingobrickan.");
//            }

//            // Slumpa utmaningar
//            var random = new Random();
//            return allChallenges.OrderBy(x => random.Next()).Take(Math.Min(count, allChallenges.Count)).ToList();
//        }

//        public async Task<BingoGame?> GetGameByIdAsync(string gameId)
//        {
//            var collection = _firestoreDb.Collection("BingoGames");
//            var query = collection.WhereEqualTo("GameId", gameId);
//            var snapshot = await query.GetSnapshotAsync();

//            if (snapshot.Documents.Count == 0)
//            {
//                Console.WriteLine($"Game with ID {gameId} not found.");
//                return null;
//            }

//            var doc = snapshot.Documents.First();
//            // Här gör Firestore automapping till BingoGame och dess nested-objekt
//            var game = doc.ConvertTo<BingoGame>();
//            // Sätt manuellt DocumentId
//            game.DocumentId = doc.Id;

//            return game;
//        }
//        public async Task MarkChallengeAsCompletedAsync(string gameId, string title, string playerId, int pointUpdate = 1)
//        {
//            try
//            {
//                // Hämta spelet från Firestore
//                var game = await GetGameByIdAsync(gameId);

//                if (game == null || string.IsNullOrEmpty(game.DocumentId))
//                {
//                    Console.WriteLine($"Game with ID {gameId} not found or DocumentId is missing.");
//                    return;
//                }

//                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
//                var gameSnapshot = await gameRef.GetSnapshotAsync();

//                if (!gameSnapshot.Exists)
//                {
//                    Console.WriteLine($"Game with DocumentId {game.DocumentId} not found.");
//                    return;
//                }

//                var gameData = gameSnapshot.ToDictionary();
//                if (!gameData.TryGetValue("Cards", out var cardsObject) || cardsObject is not List<object> cardsList)
//                {
//                    Console.WriteLine("Cards data is invalid.");
//                    return;
//                }

//                // Hitta kortet med rätt titel
//                var targetCard = cardsList
//                    .OfType<Dictionary<string, object>>()
//                    .FirstOrDefault(card => card.TryGetValue("Title", out var cardTitle) && cardTitle.ToString() == title);

//                if (targetCard == null)
//                {
//                    Console.WriteLine($"Card with Title '{title}' not found in game {gameId}.");
//                    return;
//                }

//                // ----- Ersätt den gamla PlayerProgress-logiken med CompletedBy -----

//                // Hämta eller skapa CompletedBy-listan
//                List<Dictionary<string, object>> completedByList;
//                if (!targetCard.TryGetValue("CompletedBy", out var completedByObject) || !(completedByObject is List<object> rawCompletedBy))
//                {
//                    completedByList = new List<Dictionary<string, object>>();
//                    targetCard["CompletedBy"] = completedByList;
//                }
//                else
//                {
//                    completedByList = new List<Dictionary<string, object>>();

//                    // Försök iterera med en for-loop
//                    for (int i = 0; i < rawCompletedBy.Count; i++)
//                    {
//                        try
//                        {
//                            var item = rawCompletedBy[i];
//                            if (item == null)
//                            {
//                                Console.WriteLine($"Element {i} är null.");
//                                continue;
//                            }
//                            Console.WriteLine($"Element {i} typ: {item.GetType().FullName}");
//                            if (item is Dictionary<string, object> dict)
//                            {
//                                completedByList.Add(dict);
//                            }
//                            else
//                            {
//                                Console.WriteLine($"Element {i} har oväntad typ: {item.GetType().FullName}");
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine($"Fel vid bearbetning av element {i}: {ex.Message}");
//                        }
//                    }
//                    // Sätt tillbaka den nya listan till targetCard så att vi jobbar med den
//                    targetCard["CompletedBy"] = completedByList;
//                }

//                // Kontrollera om spelaren redan finns i listan
//                bool playerAlreadyCompleted = completedByList.Any(entry =>
//                    entry.ContainsKey("PlayerId") && entry["PlayerId"].ToString() == playerId);

//                if (!playerAlreadyCompleted)
//                {
//                    // Här används en placeholder för spelarens färg.
//                    // Ersätt med din logik för att hämta spelarens färg, exempelvis från en Player-modell.
//                    string currentUserColor = "#FFFFFF"; // fallback

//                    if (gameData.ContainsKey("PlayerInfo") && gameData["PlayerInfo"] is Dictionary<string, object> playerInfoDict)
//                    {
//                        if (playerInfoDict.TryGetValue(playerId, out var playerDataObj) && playerDataObj is Dictionary<string, object> playerData)
//                        {
//                            if (playerData.TryGetValue("Color", out var colorObj))
//                            {
//                                currentUserColor = colorObj.ToString();
//                            }
//                        }
//                    }

//                    var completedInfo = new Dictionary<string, object>
//                    {
//                        { "PlayerId", playerId },
//                        { "UserColor", currentUserColor },
//                        { "Nickname", game.PlayerInfo[playerId].Nickname }
//                    };
//                    completedByList.Add(completedInfo);

//                    // Uppdatera kortet i Firestore med den nya CompletedBy-listan
//                    await gameRef.UpdateAsync("Cards", cardsList);
//                    Console.WriteLine($"Player {playerId} marked card with Title '{title}' as completed (using CompletedBy).");

//                    // ----- Uppdatera PlayerInfo -----
//                    Dictionary<string, object> playerInfo;
//                    if (gameData.ContainsKey("PlayerInfo") && gameData["PlayerInfo"] is Dictionary<string, object> pi)
//                    {
//                        playerInfo = new Dictionary<string, object>(pi);
//                    }
//                    else
//                    {
//                        playerInfo = new Dictionary<string, object>();
//                    }

//                    if (playerInfo.ContainsKey(playerId))
//                    {
//                        if (playerInfo[playerId] is Dictionary<string, object> playerData)
//                        {
//                            // Hämta nuvarande poäng, öka med 1
//                            int points = playerData.ContainsKey("Points") ? Convert.ToInt32(playerData["Points"]) : 0;
//                            // pointUpdate innehåller värdet som brickan är värd att markera
//                            points += pointUpdate;
//                            playerData["Points"] = points;

//                            playerInfo[playerId] = playerData;
//                        }
//                    }
//                    else
//                    {
//                        // Om spelaren inte finns, lägg till med initiala värden
//                        playerInfo[playerId] = new Dictionary<string, object>
//                    {
//                        { "Color", App.CurrentUserProfile.PlayerColor },
//                        { "Points", 1 }
//                    };
//                    }

//                    // Uppdatera PlayerInfo-fältet i Firestore
//                    await gameRef.UpdateAsync("PlayerInfo", playerInfo);
//                    Console.WriteLine($"PlayerInfo updated for player {playerId}.");
//                }



//                // ----- Uppdatera lokal cache -----
//                if (!App.CompletedChallengesCache.ContainsKey(gameId))
//                {
//                    App.CompletedChallengesCache[gameId] = new Dictionary<string, List<string>>();
//                }

//                if (!App.CompletedChallengesCache[gameId].ContainsKey(title))
//                {
//                    App.CompletedChallengesCache[gameId][title] = new List<string>();
//                }

//                var nickname = game.PlayerInfo[playerId].Nickname;
//                if (!App.CompletedChallengesCache[gameId][title].Contains(nickname))
//                {
//                    App.CompletedChallengesCache[gameId][title].Add(nickname);
//                    Console.WriteLine($"Player {nickname} added to cache for challenge '{title}'.");
//                }

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error updating challenge progress: {ex.Message}");
//            }
//        }
//        public async Task UnmarkChallengeAsCompletedAsync(string gameId, string title, string playerId)
//        {
//            var game = await GetGameByIdAsync(gameId);
//            if (game == null || string.IsNullOrEmpty(game.DocumentId)) return;

//            var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
//            var gameSnapshot = await gameRef.GetSnapshotAsync();
//            var gameData = gameSnapshot.ToDictionary();

//            if (!gameData.TryGetValue("Cards", out var cardsObj) || cardsObj is not List<object> cardsList)
//                return;

//            foreach (var cardObj in cardsList.OfType<Dictionary<string, object>>())
//            {
//                if (cardObj.TryGetValue("Title", out var t) && t.ToString() == title &&
//                    cardObj.TryGetValue("CompletedBy", out var cb) && cb is List<object> cbList)
//                {
//                    var updatedList = cbList
//                        .OfType<Dictionary<string, object>>()
//                        .Where(entry => entry.TryGetValue("PlayerId", out var pid) && pid.ToString() != playerId)
//                        .ToList();

//                    cardObj["CompletedBy"] = updatedList;
//                    break;
//                }
//            }

//            // Minska poäng i PlayerInfo
//            if (gameData.TryGetValue("PlayerInfo", out var playerInfoObj) &&
//                playerInfoObj is Dictionary<string, object> playerInfoDict &&
//                playerInfoDict.TryGetValue(playerId, out var playerStatsObj) &&
//                playerStatsObj is Dictionary<string, object> statsDict &&
//                statsDict.TryGetValue("Points", out var pointsObj))
//            {
//                int points = Convert.ToInt32(pointsObj);
//                statsDict["Points"] = Math.Max(points - 1, 0); // undvik negativa poäng
//                playerInfoDict[playerId] = statsDict;
//            }

//            await gameRef.UpdateAsync(new Dictionary<string, object>
//            {
//                { "Cards", cardsList },
//                { "PlayerInfo", gameData["PlayerInfo"] }
//            });

//            Console.WriteLine($"Removed {playerId} from CompletedBy for '{title}' and updated PlayerInfo.");
//        }
//        public async Task UpdatePlayerColorInGameAsync(string gameId, string playerId, string newColor)
//        {
//            var game = await GetGameByIdAsync(gameId);
//            if (game == null || string.IsNullOrEmpty(game.DocumentId))
//                return;

//            var docRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);

//            // 1. Uppdatera PlayerInfo (huvudfärg för spelaren i spelet)
//            if (game.PlayerInfo != null && game.PlayerInfo.ContainsKey(playerId))
//            {
//                game.PlayerInfo[playerId].Color = newColor;
//            }
//            if (game.PlayerInfo == null)
//            {
//                Console.WriteLine("PlayerInfo not found in game DB");
//                return;
//            }
//            if (game.Cards == null)
//            {
//                Console.WriteLine("Cards not found in game DB");
//                return;
//            }
//            // 2. Uppdatera CompletedBy-fältet i varje kort där spelaren finns
//            bool cardsModified = false;

//            if (game.Cards != null)
//            {
//                foreach (var card in game.Cards)
//                {
//                    if (card.CompletedBy != null)
//                    {
//                        var entry = card.CompletedBy.FirstOrDefault(c => c.PlayerId == playerId);
//                        if (entry != null)
//                        {
//                            entry.UserColor = newColor;
//                            cardsModified = true;
                            
//                        }
//                    }
//                }
//            }

//            // 3. Spara ändringarna till Firestore
//            var updates = new Dictionary<string, object>
//            {
//                { "PlayerInfo", game.PlayerInfo }
//            };

//            if (cardsModified)
//            {
//                updates["Cards"] = game.Cards;
//                App.ShouldRefreshChallenges = true;
//            }

//            await docRef.UpdateAsync(updates);
//            Console.WriteLine($"Spelarens färg uppdaterad till '{newColor}' i PlayerInfo och CompletedBy för spelet {gameId}.");
//        }
//        public async Task<List<Comment>> GetCommentsAsync(string gameId)
//        {
//            var comments = new List<Comment>();
//            // Hämta spelet med hjälp av GetGameByIdAsync
//            var game = await GetGameByIdAsync(gameId);
//            var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
//            var querySnapshot = await gameRef.Collection("Comments")
//                                             .OrderBy("Timestamp")
//                                             .GetSnapshotAsync();

//            foreach (var doc in querySnapshot.Documents)
//            {
//                var comment = doc.ConvertTo<Comment>();

//                // Slå upp spelarens färg från PlayerInfo
//                if (game.PlayerInfo.TryGetValue(comment.UserId, out var playerStats))
//                {
//                    comment.PlayerColor = playerStats.Color;
//                }
//                else
//                {
//                    comment.PlayerColor = "#FFFFFF"; // fallback-färg om något saknas
//                }

//                comments.Add(comment);
//            }

//            return comments;
//        }
//        public async Task PostCommentAsync(string gameId, string userId, string message)
//        {
//            try
//            {
//                // Förbered kommentarsdatan
//                var comment = new Dictionary<string, object>
//                {
//                    { "UserId", userId },
//                    { "Nickname", App.CurrentUserProfile.Nickname },
//                    { "Message", message },
//                    { "Timestamp", Timestamp.GetCurrentTimestamp() }
//                };

//                // Hämta spelet med hjälp av GetGameByIdAsync
//                var game = await GetGameByIdAsync(gameId);
//                if (game == null || string.IsNullOrEmpty(game.DocumentId))
//                {
//                    Console.WriteLine($"Game with ID {gameId} not found or missing DocumentId.");
//                    return;
//                }

//                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
//                await gameRef.Collection("Comments").AddAsync(comment);

//                Console.WriteLine("Comment posted successfully!");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error posting comment: {ex.Message}");
//            }
//        }
//        public async Task SetUserAsync(string userId, string email, string nickname)
//        {
//            var userRef = _firestoreDb.Collection("users").Document(userId);

//            var snapshot = await userRef.GetSnapshotAsync();
//            if (!snapshot.Exists)
//            {
//                var newUserProfile = new UserProfile
//                {
//                    UserId = userId,
//                    Email = email,
//                    Nickname = nickname,
//                    PlayerColor = "#FF5733" // Default färg
//                }; 

//                await userRef.SetAsync(newUserProfile);
//            }
//        }

//        public string FormatTimeAgo(DateTime timestamp)
//        {
//            var timeSpan = DateTime.Now - timestamp;

//            if (timeSpan.TotalSeconds < 60)
//                return $"{timeSpan.Seconds} sekund{(timeSpan.Seconds > 1 ? "er" : "")} sedan";
//            if (timeSpan.TotalMinutes < 60)
//                return $"{timeSpan.Minutes} minut{(timeSpan.Minutes > 1 ? "er" : "")} sedan";
//            if (timeSpan.TotalHours < 24)
//                return $"{timeSpan.Hours} timm{(timeSpan.Hours == 1 ? "e" : "ar")} sedan";
//            if (timeSpan.TotalDays < 30)
//                return $"{timeSpan.Days} dag{(timeSpan.Days > 1 ? "ar" : "")} sedan";

//            return timestamp.ToString("yyyy-MM-dd HH:mm");
//        }
//        public async Task UpdatePlayerColorAsync(string userId, string newColor)
//        {
//            var docRef = _firestoreDb.Collection("users").Document(userId);
//            await docRef.UpdateAsync("PlayerColor", newColor);
//        }
//        public async Task UpdateUserNicknameAsync(string userId, string newNickname)
//        {
//            try
//            {
//                // Uppdatera användarnamn i "users" samlingen
//                var userRef = _firestoreDb.Collection("users").Document(userId);
//                await userRef.UpdateAsync(new Dictionary<string, object> { { "Nickname", newNickname } });

//                Console.WriteLine($"Nickname updated for user {userId}");

//                // Hämta alla BingoGames
//                var gamesRef = _firestoreDb.Collection("BingoGames");
//                var gamesSnapshot = await gamesRef.GetSnapshotAsync();

//                foreach (var gameDoc in gamesSnapshot.Documents)
//                {
//                    var gameData = gameDoc.ToDictionary();

//                    // --- Uppdatera kommentarer ---
//                    var commentsRef = gameDoc.Reference.Collection("Comments");
//                    var commentsSnapshot = await commentsRef.GetSnapshotAsync();

//                    var batch = _firestoreDb.StartBatch();
//                    bool hasChanges = false;

//                    foreach (var commentDoc in commentsSnapshot.Documents)
//                    {
//                        var commentData = commentDoc.ToDictionary();

//                        if (commentData.ContainsKey("UserId") && commentData["UserId"].ToString() == userId)
//                        {
//                            batch.Update(commentDoc.Reference, new Dictionary<string, object> { { "Nickname", newNickname } });
//                            hasChanges = true;
//                        }
//                    }

//                    if (hasChanges)
//                    {
//                        await batch.CommitAsync();
//                        Console.WriteLine($"Updated nickname for user {userId} in comments of BingoGame {gameDoc.Id}");
//                    }

//                    // --- Uppdatera PlayerInfo ---
//                    if (gameData.TryGetValue("PlayerInfo", out var playerInfoObj) && playerInfoObj is Dictionary<string, object> playerInfo)
//                    {
//                        if (playerInfo.TryGetValue(userId, out var playerDataObj) && playerDataObj is Dictionary<string, object> playerData)
//                        {
//                            playerData["Nickname"] = newNickname;
//                            playerInfo[userId] = playerData;

//                            await gameDoc.Reference.UpdateAsync("PlayerInfo", playerInfo);
//                            Console.WriteLine($"Updated PlayerInfo nickname in game {gameDoc.Id}");
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error updating nickname across system: {ex.Message}");
//            }
//        }
//        public async Task<Dictionary<string, int>> GetLeaderboardAsync(string gameId)
//        {
//            // Hämta spelet
//            var game = await GetGameByIdAsync(gameId);
//            if (game == null)
//            {
//                Console.WriteLine($"Game with ID {gameId} not found.");
//                return new Dictionary<string, int>();
//            }

//            // Skapa en dictionary för leaderboarden
//            Dictionary<string, int> leaderboard = new Dictionary<string, int>();

//            // Om PlayerInfo finns, iterera över den och hämta poängen
//            if (game.PlayerInfo != null)
//            {
//                foreach (var kvp in game.PlayerInfo)
//                {
//                    // kvp.Key = playerId, kvp.Value = spelarens data (t.ex. en Dictionary<string, object>)
//                    // Om vi använder PlayerStats-klassen
//                    if (kvp.Value is PlayerStats stats)
//                    {
//                        leaderboard[kvp.Key] = stats.Points;
//                    }
//                    else
//                    {
//                        leaderboard[kvp.Key] = 0;
//                    }
//                }
//            }
//            else if (game.PlayerInfo != null)
//            {
//                // Fallback: om ingen PlayerInfo finns, sätt 0 poäng för varje spelare
//                foreach (var kvp in game.PlayerInfo)
//                {
//                    var playerId = kvp.Key;
//                    var playerStats = kvp.Value;

//                    if (!leaderboard.ContainsKey(playerId))
//                    {
//                        leaderboard[playerId] = playerStats?.Points ?? 0;
//                    }
//                }
//            }

//            return leaderboard;
//        }
//        public async Task UpdateUserProfileAsync(UserProfile profile)
//        {
//            try
//            {
//                var userDoc = _firestoreDb.Collection("users").Document(profile.UserId);
//                await userDoc.SetAsync(profile, SetOptions.MergeAll);
//                Console.WriteLine($"User profile for {profile.UserId} updated.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error updating user profile: {ex.Message}");
//                throw;
//            }
//        }
//        //FUNKTION FÖR BETALTJÄNST FIREBASE
//        public async Task<string> UploadProfileImageAsync(Stream imageStream, string fileName)
//        {
//            using var httpClient = new HttpClient();

//            // Förbered innehållet
//            var content = new MultipartFormDataContent();
//            var streamContent = new StreamContent(imageStream);
//            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

//            content.Add(streamContent, "file", fileName);

//            try
//            {
//                var response = await httpClient.PostAsync("https://backendbingoapi.onrender.com/api/upload", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadFromJsonAsync<UploadResponse>();
//                    return responseContent?.Url;
//                }
//                else
//                {
//                    var errorMsg = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine($"Upload failed: {response.StatusCode}, {errorMsg}");
//                    throw new Exception("Kunde inte ladda upp bilden.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Upload error: {ex.Message}");
//                throw;
//            }
//        }

//        public class UploadResponse
//        {
//            public string Url { get; set; }
//        }
//        public async Task ToggleReactionAsync(string documentId, string commentId, string userId, string emoji)
//        {
//            var commentRef = _firestoreDb
//                .Collection("BingoGames")
//                .Document(documentId)
//                .Collection("Comments")
//                .Document(commentId);

//            var commentSnap = await commentRef.GetSnapshotAsync();
//            if (!commentSnap.Exists) return;

//            var data = commentSnap.ToDictionary();
//            var reactions = data.ContainsKey("Reactions")
//                ? JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data["Reactions"].ToString())
//                : new Dictionary<string, List<string>>();

//            if (!reactions.ContainsKey(emoji))
//                reactions[emoji] = new List<string>();

//            if (reactions[emoji].Contains(userId))
//                reactions[emoji].Remove(userId);
//            else
//                reactions[emoji].Add(userId);

//            await commentRef.UpdateAsync("Reactions", reactions);
//        }
//    }
//}