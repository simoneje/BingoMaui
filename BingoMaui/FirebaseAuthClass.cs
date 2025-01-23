using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using System;
using System.Threading.Tasks;

namespace BingoMaui.Services
{
    public class FirebaseAuthService
    {
        private readonly FirebaseAuthProvider _authProvider;

        public FirebaseAuthService()
        {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyCuGa8fDtOtjPUc8wQV0kJ1YFi21AY3nr8"));
        }

        // Metod för att skapa en ny användare (registrera sig)
        public async Task<string> RegisterUserAsync(string email, string password)
        {
            try
            {
                var auth = await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
                // auth.User ger dig information om den skapade användaren
                return auth.User.LocalId; // Returnerar ett unikt ID för användaren
            }
            catch (Exception ex)
            {
                // Fånga fel (t.ex. om e-post redan är registrerad)
                return $"Error creating user: {ex.Message}";
            }
        }

        // Metod för att logga in en användare (e-post & lösenord)
        public async Task<string> LoginUserAsync(string email, string password)
        {
            try
            {
                var auth = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                // auth.User innehåller information om inloggad användare
                Preferences.Set("UserId", auth.User.LocalId);
                return auth.User.LocalId;
            }
            catch (Exception ex)
            {
                return $"Error logging in: {ex.Message}";
            }
        }
    }
}

