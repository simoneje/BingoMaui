using System.IdentityModel.Tokens.Jwt;

namespace BingoMaui.Services.Auth
{
    public static class JwtService
    {
        public static string ExtractUidFromToken(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                return string.Empty;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(idToken);
                var uidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "sub");

                return uidClaim?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Kunde inte extrahera UID från token: {ex.Message}");
                return string.Empty;
            }
        }
        public static bool IsTokenExpired(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return true;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                var expClaim = token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                if (expClaim == null) return true;

                var expUnix = long.Parse(expClaim);
                var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix);

                return expTime <= DateTimeOffset.UtcNow;
            }
            catch
            {
                return true; // Om något går fel, anta att token är ogiltig
            }
        }

    }
}
