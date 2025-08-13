//using Firebase.Auth;
//using Google.Cloud.Firestore;

//namespace BingoMaui.Services
//{
//    class FirestoreAdminService
//    {
//        private readonly FirestoreService _firestoreService;
//        private readonly FirestoreDb _firestoreDb;
//        public FirestoreAdminService() 
//        {
//            _firestoreService = new FirestoreService();
//            _firestoreDb = FirestoreDb.Create("bingomaui-28990");
//        }
//        public async Task MigratePlayerIdsAsync()
//        {

//            var gamesRef = _firestoreDb.Collection("BingoGames");
//            var snapshot = await gamesRef.GetSnapshotAsync();

//            foreach (var doc in snapshot.Documents)
//            {
//                var gameData = doc.ToDictionary();

//                if (gameData.TryGetValue("PlayerInfo", out var playerInfoObj) &&
//                    playerInfoObj is Dictionary<string, object> playerInfoDict)
//                {
//                    var playerIds = playerInfoDict.Keys.ToList();

//                    // Uppdatera endast om PlayerIds inte redan finns
//                    if (!gameData.ContainsKey("PlayerIds"))
//                    {
//                        await doc.Reference.UpdateAsync("PlayerIds", playerIds);
//                        Console.WriteLine($"✅ Updated PlayerIds for game: {doc.Id}");
//                    }
//                }
//            }

//            Console.WriteLine("🎉 Migration of PlayerIds complete.");
//        }
//    }

//}
