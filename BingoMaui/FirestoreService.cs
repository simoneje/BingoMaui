using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Google.Cloud.Firestore;
using System.IO;
// using AndroidX.Annotations;

namespace BingoMaui.Services
{

    // Här skapas alla firestoreDB aktioner
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly FirebaseAuthProvider _authProvider;

        public FirestoreService()
        {
            // Anslut till din Firestore-databas
            _firestoreDb = FirestoreDb.Create("bingomaui-28990");
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig("YOUR_API_KEY"));
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
        public async Task AddPlayerToGameAsync(string documentId, string playerId, string gameName)
        {
            var documentRef = _firestoreDb.Collection("BingoGames").Document(documentId);
            //var gameName = documentRef.

            await documentRef.UpdateAsync("Players", FieldValue.ArrayUnion(playerId)); // Lägg till spelaren i "Players"-listan
            Console.WriteLine($"Player {playerId} added to game {gameName}.");
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
                    CompletedBy = new List<string>() // Tom lista eftersom BingoCard inte hanterar CompletedBy
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
            var game = doc.ConvertTo<BingoGame>();

            // Lägg till dokumentets ID om det behövs
            game.DocumentId = doc.Id;

            return game;
        }
        public async Task MarkChallengeAsCompletedAsync(string gameId, string title, string playerId)
        {
            try
            {
                // Hämta spelet med hjälp av GetGameByIdAsync
                var game = await GetGameByIdAsync(gameId);

                if (game == null || string.IsNullOrEmpty(game.DocumentId))
                {
                    Console.WriteLine($"Game with ID {gameId} not found or DocumentId is missing.");
                    return;
                }

                // Hämta referens till rätt dokument i "BingoGames" med hjälp av DocumentId
                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
                var gameSnapshot = await gameRef.GetSnapshotAsync();

                if (!gameSnapshot.Exists)
                {
                    Console.WriteLine($"Game with DocumentId {game.DocumentId} not found.");
                    return;
                }
                var gameData = gameSnapshot.ToDictionary();
                var newGameData = new Object();
                var numbers = gameData.Keys;

                // Kontrollera om korten finns som en del av en lista
                Console.WriteLine(gameData.Keys);
                foreach (KeyValuePair<string, object> entry in gameData)
                {
                    Console.WriteLine($"Card key:'{entry.Key}' and Value: '{entry.Value}'");
                    if(entry.Key == "Cards")
                    {
                        newGameData = entry.Value;
                        if (newGameData is List<object> cardsList)
                        {
                            foreach (var cardObj in cardsList)
                            {
                                if (cardObj is Dictionary<string, object> cardDict)
                                {
                                    // Kontrollera om kortet innehåller rätt "Title"
                                    var foundTitle = false;
                                    foreach (var innerEntry in cardDict)
                                    {
                                        if (innerEntry.Key == "Title" && innerEntry.Value.ToString() == title) // Byt "desiredTitle" mot rätt titel
                                        {
                                            Console.WriteLine($"Found card with Title: {innerEntry.Value}");
                                            foundTitle = true;
                                            break; // Fortsätt med nästa steg
                                        }
                                    }

                                    if (foundTitle)
                                    {
                                        // Leta efter eller uppdatera "PlayerProgress"
                                        var playerProgressUpdated = false;

                                        foreach (var innerEntry in cardDict)
                                        {
                                            if (innerEntry.Key == "PlayerProgress" && innerEntry.Value is Dictionary<string, object> playerProgressDict)
                                            {
                                                // Kontrollera om spelarens ID redan finns i "PlayerProgress"
                                                if (!playerProgressDict.ContainsKey(playerId))
                                                {
                                                    playerProgressDict[playerId] = true; // Markera spelaren som klarat utmaningen
                                                    Console.WriteLine($"Added PlayerId {playerId} to PlayerProgress.");
                                                }

                                                playerProgressUpdated = true;
                                                break; // Avsluta när vi har hittat och uppdaterat PlayerProgress
                                            }
                                        }

                                        // Om PlayerProgress inte existerar, skapa det
                                        if (!playerProgressUpdated)
                                        {
                                            cardDict["PlayerProgress"] = new Dictionary<string, object>
                                            {
                                                { playerId, true }
                                            };
                                            Console.WriteLine($"Initialized PlayerProgress with PlayerId {playerId}.");
                                        }

                                        // Uppdatera databasen
                                        try
                                        {
                                            await gameRef.UpdateAsync("Cards", cardsList);
                                            Console.WriteLine("PlayerProgress successfully updated in Firebase.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error updating PlayerProgress in Firebase: {ex.Message}");
                                        }

                                        break; // Avsluta när vi har uppdaterat rätt kort
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("newGameData is not a List<object>");
                        }

                    }
                }

                //foreach (entry2 in newGameData)
                //{
                //    Console.WriteLine($"Card key:'{entry2.Key}' and Value: '{entry2.Value}'");
                //}

                var cards = newGameData as List<Dictionary<string, object>>;
                if (cards == null)
                {
                    Console.WriteLine("Cards data is invalid.");
                    return;
                }

                // Hitta det specifika kortet baserat på "Title"
                var targetCard = cards.FirstOrDefault(card =>
                    card.ContainsKey("Title") && card["Title"].ToString() == title);

                if (targetCard == null)
                {
                    Console.WriteLine($"Card with Title '{title}' not found in game {gameId}.");
                    return;
                }

                // Lägg till spelarens progress om det inte redan finns
                if (!targetCard.ContainsKey("PlayerProgress"))
                {
                    targetCard["PlayerProgress"] = new List<string>();
                }

                var progress = targetCard["PlayerProgress"] as List<string>;
                if (progress != null && !progress.Contains(playerId))
                {
                    progress.Add(playerId);
                    Console.WriteLine($"Player {playerId} marked card with Title '{title}' as completed.");
                }

                // Uppdatera dokumentet i Firestore
                await gameRef.UpdateAsync("Cards", cards);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating challenge progress: {ex.Message}");
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
                // Hämta spelet med hjälp av GetGameByIdAsync
                var game = await GetGameByIdAsync(gameId);
                // Hämta spelet från Firestore
                var gameRef = _firestoreDb.Collection("BingoGames").Document(game.DocumentId);
                var gameSnapshot = await gameRef.GetSnapshotAsync();

                if (!gameSnapshot.Exists)
                {
                    Console.WriteLine($"Game with ID {gameId} not found.");
                    return null;
                }

                var gameData = gameSnapshot.ToDictionary();

                if (!gameData.ContainsKey("Cards"))
                {
                    Console.WriteLine("Cards field not found in the game document.");
                    return null;
                }

                // Hämta Cards och leta efter rätt utmaning baserat på titel
                var cards = gameData["Cards"] as List<object>;
                if (cards == null)
                {
                    Console.WriteLine("Cards data is invalid.");
                    return null;
                }

                foreach (var cardObj in cards)
                {
                    if (cardObj is Dictionary<string, object> cardDict &&
                        cardDict.ContainsKey("Title") &&
                        cardDict["Title"].ToString() == title)
                    {
                        if (cardDict.ContainsKey("PlayerProgress") && cardDict["PlayerProgress"] is Dictionary<string, object> playerProgressDict)
                        {
                            var completedBy = playerProgressDict.Keys.ToList();

                            return new Challenge
                            {
                                Title = cardDict["Title"].ToString(),
                                Description = cardDict.ContainsKey("Description") ? cardDict["Description"].ToString() : string.Empty,
                                Category = cardDict.ContainsKey("Category") ? cardDict["Category"].ToString() : string.Empty,
                                CompletedBy = completedBy
                            };
                        }
                        else
                        {
                            Console.WriteLine("PlayerProgress is either missing or not in the expected format.");
                            return new Challenge
                            {
                                Title = cardDict["Title"].ToString(),
                                Description = cardDict.ContainsKey("Description") ? cardDict["Description"].ToString() : string.Empty,
                                Category = cardDict.ContainsKey("Category") ? cardDict["Category"].ToString() : string.Empty,
                                CompletedBy = new List<string>() // Tom lista om PlayerProgress saknas
                            };
                        }
                    }
                }

                Console.WriteLine($"Challenge with Title {title} not found in game {gameId}.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting challenge by title: {ex.Message}");
                return null;
            }
        }
        public async Task<List<Challenge>> GetChallengesForGameAsync(string gameId)
        {
            var challenges = new List<Challenge>();
            var querySnapshot = await _firestoreDb
                .Collection("Challenges")
                .WhereEqualTo("GameId", gameId)
                .GetSnapshotAsync();

            foreach (var document in querySnapshot.Documents)
            {
                var challenge = document.ConvertTo<Challenge>();
                challenges.Add(challenge);
            }

            return challenges;
        }
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

    }
}
