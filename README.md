Bingo – Klient & Backend

En MAUI-klient med .NET 8 backend som använder Firebase Auth (IdToken/RefreshToken) och Firestore för data. Klienten pratar endast med backend; inga servicekontonycklar i klienten.
Innehåll

    Arkitekturöversikt

    Konfiguration (Backend & Klient)

    Livscykel för auth (IdToken/RefreshToken)

    Dataflöden (Create/Join/Get Game, Challenges, Comments, Profile, Leaderboard)

    Endpoints (kort referens)

    Caching & Logout

    Kodexempel (snuttar)

    Testchecklista

    TODO / framtidsjobb

Arkitekturöversikt

    Klient (MAUI)

        Delad HttpClient via BackendServices med Authorization: Bearer <IdToken>.

        Säker lagring i SecureStorage (IdToken, RefreshToken, UserId, IsLoggedIn).

        Lättvikts-cache av spel (Preferences) per GameId för snabb rendering.

        Allt backend-IO går via services (Game/Challenge/Profile/Comments/Misc).

    Backend (.NET)

        FirebaseAuthorizeAttribute validerar IdToken och extraherar FirebaseUid.

        Firestore som datalager (users, BingoGames, Comments subcollection).

        Controllers: Auth, Profiles, Games, Challenges, Comments.

Konfiguration
Backend

    Miljövariabler/sekretess

        GOOGLE_APPLICATION_CREDENTIALS → absolut sökväg till servicekonto .json (lagras på servern).

        Firebase Web API key (för REST mot Identity Toolkit) som appsettings/secret (inte i repo).

    Firestoresamlingar

        users/{uid}: Email, Nickname, PlayerColor, CreatedAt, ProfileImageUrl, m.m.

        BingoGames/{docId}: GameId, GameName, PlayerIds[], PlayerInfo (dict), InviteCode, Cards[], …

        BingoGames/{docId}/Comments: CommentId, UserId, Nickname, PlayerColor, Message, Timestamp, Reactions…

Klient

    SecureStorage nycklar

        "IdToken", "RefreshToken", "UserId", "IsLoggedIn".

    BackendServices

        En (1) delad HttpClient.

        UpdateToken(token) sätter Authorization-header för alla services.

Auth-livscykel (IdToken + RefreshToken)

    Login
    Klient POST api/auth/login → backend kallar Firebase → returnerar idToken, refreshToken, localId.

        Klient sparar IdToken, RefreshToken, UserId i SecureStorage.

        BackendServices.UpdateToken(IdToken) sätter header.

        Klient hämtar profil via ProfilesController.

    App-start

        Läser IsLoggedIn, UserId, IdToken.

        Om IdToken saknas/utgånget → använd RefreshToken via Firebase Secure Token API.

        Misslyckas refresh → LogoutAsync().

    Logout

        Ta bort tokens från SecureStorage.

        BackendServices.ResetToken().

        Rensa lokala cache-objekt, navigera till login.

Dataflöden
Skapa spel

    Klient kombinerar egna + slumpade utmaningar → CreateGameRequest.

    POST api/games/create → får BingoGame.

    Cacha spelet och navigera till BingoBricka.

Gå med i spel

    POST api/games/join (InviteCode, Nickname, PlayerColor).

    Svar: redan med (GameId) eller nytt deltagande.

    Klient hämtar/cachar spelet och navigerar.

Hämta mina spel

    GET api/games/user → lista BingoGame.

Visa bingobricka

    Läser cache cached_game_{gameId} → renderar direkt.

    Hämtar fräscht från backend i bakgrunden → uppdaterar om ändring.

Utmaningar

    Markera klar: POST api/challenges/complete (gameId, challengeTitle).

    Avmarkera: POST api/challenges/uncomplete.

    Backend uppdaterar Cards[] och PlayerInfo[uid].Points.

Pricklogik i UI:

    ≤4 completion → färgade prickar.

        4 → visa klickbar “+X”-badge (öppna lista med alla som klarat).

Kommentarer

    Hämta: GET api/comments/{gameId}.

    Posta: POST api/comments/{gameId} (message).

    Reagera: POST api/comments/{gameId}/{commentId}/toggle-reaction (emoji).

Profil (global)

    Nickname:

        POST api/profiles/nickname uppdaterar users/{uid}.

        Option: batchsynk i alla spel via api/profiles/sync-nickname.

    Defaultfärg:

        POST api/profiles/color uppdaterar users/{uid}.

        Option: batchsynk färg i alla spel via api/profiles/sync-color.

Färg per spel

    POST api/games/{gameId}/update-color uppdaterar bara det spelet (PlayerInfo[uid].Color + CompletedBy-färg i spelet).

Leaderboard

    Klient hämtar BingoGame och sorterar PlayerInfo efter Points.

Endpoints (kort)

Auth

    POST /api/auth/register (anonym) → skapar Firebase-användare + users/{uid} → tokens.

    POST /api/auth/login (anonym) → returnerar tokens.

Profiles

    GET /api/profiles/profile

    POST /api/profiles/nickname

    POST /api/profiles/color

    POST /api/profiles/sync-nickname

    POST /api/profiles/sync-color

Games

    POST /api/games/create

    POST /api/games/join

    GET /api/games/{gameId}

    GET /api/games/user

    POST /api/games/{gameId}/update-color

Challenges

    POST /api/challenges/complete

    POST /api/challenges/uncomplete

    GET /api/challenges/random?count=N

Comments

    GET /api/comments/{gameId}

    POST /api/comments/{gameId}

    POST /api/comments/{gameId}/{commentId}/toggle-reaction

Caching & Logout

    Game cache (klient): Preferences.Set("cached_game_{id}", json).

    Logout:

        Rensa IdToken, RefreshToken, UserId, IsLoggedIn från SecureStorage.

        BackendServices.ResetToken() nollställer header.

        AccountServices.ClearGameCacheOnLogout() rensar spelcache.

Kodexempel
App-start med token-refresh

// App.InitializeAsync()
var userId = await SecureStorage.GetAsync("UserId");
var isLoggedIn = bool.TryParse(await SecureStorage.GetAsync("IsLoggedIn"), out var b) && b;

if (isLoggedIn && !string.IsNullOrEmpty(userId))
{
    var token = await SecureStorage.GetAsync("IdToken");

    if (string.IsNullOrEmpty(token) || JwtService.IsTokenExpired(token))
    {
        var refresh = await SecureStorage.GetAsync("RefreshToken");
        if (!string.IsNullOrEmpty(refresh))
        {
            var newToken = await FirebaseAuthService.RefreshIdTokenAsync(refresh);
            if (!string.IsNullOrEmpty(newToken))
                token = newToken;
            else
            {
                await AccountServices.LogoutAsync();
                return;
            }
        }
        else
        {
            await AccountServices.LogoutAsync();
            return;
        }
    }

    BackendServices.UpdateToken(token);
    App.CurrentUserProfile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
}

Login (klient → backend)

var req = new { Email = email, Password = password };
var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
var resp = await BackendServices.HttpClient.PostAsync($"{BaseUrl}/api/auth/login", content);
var data = JsonSerializer.Deserialize<LoginResponse>(await resp.Content.ReadAsStringAsync());

await SecureStorage.SetAsync("UserId", data.LocalId);
await SecureStorage.SetAsync("IdToken", data.IdToken);
await SecureStorage.SetAsync("RefreshToken", data.RefreshToken);
await SecureStorage.SetAsync("IsLoggedIn", "true");

BackendServices.UpdateToken(data.IdToken);

Markera challenge som klar

var ok = await BackendServices.ChallengeService.MarkChallengeAsCompletedAsync(gameId, title);
if (ok)
{
    var fresh = await BackendServices.GameService.GetGameByIdAsync(gameId);
    // uppdatera UI med fresh.Cards
}

Per-spel färg

await BackendServices.GameService.UpdatePlayerColorInGameAsync(gameId, "#FF9900");

Testchecklista

    Register → Checka att users/{uid} skapas och tokens returneras.

    Login → Tokens i SecureStorage, Authorization-header sätts, profil hämtas.

    App-kallstart efter 1h+ → IdToken utgånget → refresh med RefreshToken → profil laddas.

    Create Game → rendera Bricka direkt från cache, uppdatera när backend svarar.

    Join Game med Invite Code → cache + öppning funkar.

    Challenge complete/uncomplete → UI uppdaterar prickar/“+X” korrekt.

    Comments → skapa, hämta, reactions.

    Profile nickname/color → uppdateras i users/{uid} och (vid batch) speglar i alla spel.

    Per-spel färg → påverkar bara det spelet.

    Logout → all känslig info rensas, cache städas, header nollställs.

TODO / Framtid

    Bilduppladdning via backend (IImageService + Storage).

    Offline-läge (mer cache + diff/merge).

    Rate limiting/CAPTCHA på register/login.

    Begränsa mängden lokal game-cache (LRU).

    UI-polering (10×10 brickor, animationer, A11y).

    Utökad felhantering (globala toasts, retry-policy).

Snabba tips

    Håll Firebase Web API key i klienten, men servicekontonycklar endast i backend.

    Alltid SecureStorage för tokens, aldrig Preferences.

    Använd en delad HttpClient + uppdatera Authorization-header centralt.

    Efter varje ändring som rör auth: testa login, app-start-refresh och logout.
