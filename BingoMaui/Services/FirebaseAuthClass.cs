﻿using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace BingoMaui.Services
{
    public class FirebaseAuthService
    {
        private readonly FirebaseAuthProvider _authProvider;
        private readonly FirestoreService _firestoreService;

        public FirebaseAuthService()
        {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyCuGa8fDtOtjPUc8wQV0kJ1YFi21AY3nr8"));
            _firestoreService = new FirestoreService();

        }
        public string GetLoggedInNickname()
        {
            if(App.CurrentUserProfile.Nickname == null)
            {
                return "Anonym";
            }
            return App.CurrentUserProfile.Nickname; // Default till "Anonym Användare" om inget finns
        }


        // Metod för att skapa en ny användare (registrera sig)
        public async Task<string> RegisterUserAsync(string email, string password, string nickname = null)
        {
            try
            {
                // Skapa användaren i Firebase Authentication
                var auth = await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
                var userId = auth.User.LocalId;

                // Använd e-post som default nickname om inget anges
                if (string.IsNullOrEmpty(nickname))
                {
                    nickname = email.Split('@')[0]; // Ta bort domän från e-post
                }

                // Skapa användarens Firestore-dokument
                await _firestoreService.SetUserAsync(userId, email, nickname);

                return userId; // Returnera användarens ID
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
                var userId = auth.User.LocalId;

                // Hämta användarens nickname från Firestore
                //var nickname = await _firestoreService.GetUserNicknameAsync(userId);
                //App.CurrentUserProfile.Nickname = GetLoggedInNickname();
                //App.CurrentUserProfile.Nickname = nickname;

                // Spara UserId och Nickname lokalt
                Preferences.Set("UserId", userId);
                if (App.CurrentUserProfile == null)
                    App.CurrentUserProfile = new UserProfile();

                App.CurrentUserProfile.UserId = userId;
                //Preferences.Set("Nickname", nickname);
                //Console.WriteLine($"Nickname to save: {nickname}");

                var idToken = await auth.GetFreshAuthAsync(); // Hämtar färsk token
                return idToken.FirebaseToken;

            }
            catch (Exception ex)
            {
                return $"Error logging in: {ex.Message}";
            }
        }
    }
}
