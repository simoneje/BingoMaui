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
    }
}
