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

                var document = querySnapshot.Documents[0];
                var bingoGame = document.ConvertTo<BingoGame>();
                bingoGame.DocumentId = document.Id; // Tilldela dokumentets ID

                return bingoGame;
            }

            return null; // Om spelet inte hittades
        }
        public async Task AddPlayerToGameAsync(string documentId, string playerId, string gameName, string playerColor)
        {
            var documentRef = _firestoreDb.Collection("BingoGames").Document(documentId);

            // Lägg till spelaren i "Players"-listan
            await documentRef.UpdateAsync("Players", FieldValue.ArrayUnion(playerId));

            // Lägg till spelarens färg i "PlayerInfo" (om du använder detta fält)
            await documentRef.UpdateAsync("PlayerInfo", new Dictionary<string, object>
            {
                { playerId, playerColor }
            });

            // Lägg till spelaren i "Leaderboard" med 0 poäng
            await documentRef.UpdateAsync("Leaderboard", new Dictionary<string, object>
            {
                { playerId, 0 }
            });

            Console.WriteLine($"Player {playerId} added to game {gameName} with color {playerColor} and 0 points in Leaderboard.");

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
        public async Task<List<BingoGame>> GetGamesForUserAsync(string userId)
        {
            var gamesRef = _firestoreDb.Collection("BingoGames");
            var query = gamesRef.WhereArrayContains("Players", userId);
            var snapshot = await query.GetSnapshotAsync();

            var games = new List<BingoGame>();
            foreach (var doc in snapshot.Documents)
            {
                var game = doc.ConvertTo<BingoGame>();
                game.DocumentId = doc.Id; // Lägg till DocumentId från snapshot
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
        public async Task MarkChallengeAsCompletedAsync(string gameId, string title, string playerId)
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
                if (!targetCard.TryGetValue("CompletedBy", out var completedByObject) || !(completedByObject is List<object>))
                {
                    completedByList = new List<Dictionary<string, object>>();
                    targetCard["CompletedBy"] = completedByList;
                }
                else
                {
                    completedByList = ((List<object>)targetCard["CompletedBy"]).Cast<Dictionary<string, object>>().ToList();
                }

                // Kontrollera om spelaren redan finns i listan
                bool playerAlreadyCompleted = completedByList.Any(entry =>
                    entry.ContainsKey("PlayerId") && entry["PlayerId"].ToString() == playerId);

                if (!playerAlreadyCompleted)
                {
                    // Här används en placeholder för spelarens färg.
                    // Ersätt med din logik för att hämta spelarens färg, exempelvis från en Player-modell.
                    string currentUserColor = "#FF5733";

                    var completedInfo = new Dictionary<string, object>
                    {
                        { "PlayerId", playerId },
                        { "UserColor", currentUserColor }
                    };
                    completedByList.Add(completedInfo);

                    // Uppdatera kortet i Firestore med den nya CompletedBy-listan
                    await gameRef.UpdateAsync("Cards", cardsList);
                    Console.WriteLine($"Player {playerId} marked card with Title '{title}' as completed (using CompletedBy).");
                }

                // ----- Uppdatera leaderboard -----
                Dictionary<string, object> leaderboard = new Dictionary<string, object>();
                if (gameData.ContainsKey("Leaderboard") && gameData["Leaderboard"] is Dictionary<string, object> lb)
                {
                    leaderboard = new Dictionary<string, object>(lb);
                }

                // Öka spelarens poäng med 1
                if (leaderboard.ContainsKey(playerId))
                {
                    leaderboard[playerId] = Convert.ToInt32(leaderboard[playerId]) + 1;
                }
                else
                {
                    leaderboard[playerId] = 1;
                }

                // Uppdatera leaderboard-fältet i Firestore
                await gameRef.UpdateAsync("Leaderboard", leaderboard);
                Console.WriteLine($"Leaderboard updated for player {playerId}.");

                // ----- Uppdatera lokal cache -----
                if (!App.CompletedChallengesCache.ContainsKey(gameId))
                {
                    App.CompletedChallengesCache[gameId] = new Dictionary<string, List<string>>();
                }

                if (!App.CompletedChallengesCache[gameId].ContainsKey(title))
                {
                    App.CompletedChallengesCache[gameId][title] = new List<string>();
                }

                var nickname = await GetUserNicknameAsync(playerId);
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
                    { "Nickname", App.LoggedInNickname },
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

        public async Task<bool> IsChallengeCompletedAsync(string cardId, string playerId)
        {
            var cardRef = _firestoreDb.Collection("BingoCards").Document(cardId);
            var snapshot = await cardRef.GetSnapshotAsync();

            if (snapshot.Exists && snapshot.ContainsField($"PlayerProgress.{playerId}"))
            {
                return snapshot.GetValue<bool>($"PlayerProgress.{playerId}");
            }

            return false;
        }
        public async Task<Challenge> GetChallengeByTitleAsync(string gameId, string title)
        {
            try
            {
                // 1. Hämta spelet från Firestore
                var game = await GetGameByIdAsync(gameId);
                if (game == null || string.IsNullOrEmpty(game.DocumentId))
                {
                    Console.WriteLine($"Game with ID {gameId} not found or DocumentId is missing.");
                    return null;
                }

                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
                var gameSnapshot = await gameRef.GetSnapshotAsync();

                if (!gameSnapshot.Exists)
                {
                    Console.WriteLine($"Game with ID {gameId} not found in Firestore.");
                    return null;
                }

                var gameData = gameSnapshot.ToDictionary();
                if (!gameData.ContainsKey("Cards"))
                {
                    Console.WriteLine("Cards field not found in the game document.");
                    return null;
                }

                // 2. Hämta "Cards" och leta efter rätt utmaning baserat på titel
                var cards = gameData["Cards"] as List<object>;
                if (cards == null)
                {
                    Console.WriteLine("Cards data is invalid.");
                    return null;
                }

                // 3. Iterera över korten och leta efter matchande titel
                foreach (var cardObj in cards)
                {
                    if (cardObj is Dictionary<string, object> cardDict &&
                        cardDict.ContainsKey("Title") &&
                        cardDict["Title"].ToString() == title)
                    {
                        // 4. Försök läsa "CompletedBy" i stället för "PlayerProgress"
                        if (cardDict.ContainsKey("CompletedBy") && cardDict["CompletedBy"] is List<object> completedByRaw)
                        {
                            // Konvertera varje objekt i "CompletedBy" till CompletedInfo
                            var completedByList = completedByRaw
                                .OfType<Dictionary<string, object>>()
                                .Select(dict => new CompletedInfo
                                {
                                    PlayerId = dict.ContainsKey("PlayerId") ? dict["PlayerId"].ToString() : string.Empty,
                                    UserColor = dict.ContainsKey("UserColor") ? dict["UserColor"].ToString() : "#000000"
                                })
                                .ToList();

                            return new Challenge
                            {
                                Title = cardDict["Title"].ToString(),
                                Description = cardDict.ContainsKey("Description") ? cardDict["Description"].ToString() : string.Empty,
                                Category = cardDict.ContainsKey("Category") ? cardDict["Category"].ToString() : string.Empty,
                                CompletedBy = completedByList
                            };
                        }
                        else
                        {
                            // Om "CompletedBy" saknas eller inte är i rätt format
                            Console.WriteLine("CompletedBy is either missing or not in the expected format.");
                            return new Challenge
                            {
                                Title = cardDict["Title"].ToString(),
                                Description = cardDict.ContainsKey("Description") ? cardDict["Description"].ToString() : string.Empty,
                                Category = cardDict.ContainsKey("Category") ? cardDict["Category"].ToString() : string.Empty,
                                CompletedBy = new List<CompletedInfo>() // Tom lista
                            };
                        }
                    }
                }

                Console.WriteLine($"Challenge with Title '{title}' not found in game {gameId}.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting challenge by title: {ex.Message}");
                return null;
            }
        }

        //public async Task<List<Challenge>> GetChallengesForGameAsync(string gameId)
        //{
        //    var challenges = new List<Challenge>();
        //    var querySnapshot = await _firestoreDb
        //        .Collection("BingoGames")
        //        .WhereEqualTo("GameId", gameId)
        //        .GetSnapshotAsync();

        //    foreach (var document in querySnapshot.Documents)
        //    {
        //        var challenge = document.ConvertTo<Challenge>();
        //        challenges.Add(challenge);
        //    }

        //    return challenges;
        //}
        public async Task SetUserAsync(string userId, string email, string nickname)
        {
            var userRef = _firestoreDb.Collection("users").Document(userId);

            await userRef.SetAsync(new
            {
                Email = email,
                Nickname = nickname,
                UserId = userId
            });
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
                    var commentsRef = gameDoc.Reference.Collection("Comments");
                    var commentsSnapshot = await commentsRef.GetSnapshotAsync();

                    var batch = _firestoreDb.StartBatch();
                    bool hasChanges = false;

                    foreach (var commentDoc in commentsSnapshot.Documents)
                    {
                        var commentData = commentDoc.ToDictionary();

                        // Kolla om kommentaren är från den aktuella användaren
                        if (commentData.ContainsKey("UserId") && commentData["UserId"].ToString() == userId)
                        {
                            batch.Update(commentDoc.Reference, new Dictionary<string, object> { { "Nickname", newNickname } });
                            hasChanges = true;
                        }
                    }

                    // Verkställ ändringar om några kommentarer uppdaterades
                    if (hasChanges)
                    {
                        await batch.CommitAsync();
                        Console.WriteLine($"Updated nickname for user {userId} in BingoGame {gameDoc.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating nickname across comments: {ex.Message}");
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

            // Om leaderboard-fältet finns i spelet, kopiera in de existerande poängen
            if (game.Leaderboard != null)
            {
                foreach (var kvp in game.Leaderboard)
                {
                    leaderboard[kvp.Key] = Convert.ToInt32(kvp.Value);
                }
            }

            // Antag att game.Players är en lista över alla spelare (userIds) i spelet.
            if (game.Players != null)
            {
                foreach (var playerId in game.Players)
                {
                    if (!leaderboard.ContainsKey(playerId))
                    {
                        leaderboard[playerId] = 0; // Sätt 0 poäng om spelaren inte finns i leaderboarden
                    }
                }
            }

            return leaderboard;
        }


    }
}