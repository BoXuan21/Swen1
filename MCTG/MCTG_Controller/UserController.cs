using System;
using System.Collections.Generic;

namespace MCTG
{
    public class UserController
    {
        private readonly Dictionary<string, string> _userCredentials = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _userTokens = new Dictionary<string, string>();

        public UserController()
        {
            // Predefined users for demonstration
            _userCredentials.Add("kienboec", "daniel");
        }

        // Validate user credentials and generate a token
        public string ValidateUserCredentials(string username, string password)
        {
            if (_userCredentials.ContainsKey(username) && _userCredentials[username] == password)
            {
                // Generate a token if credentials are valid
                if (!_userTokens.ContainsKey(username))
                {
                    _userTokens[username] = $"{username}-mtcgToken";
                }

                return _userTokens[username];
            }

            return null; // Invalid credentials
        }

        // Get token by username
        public string GetToken(string username)
        {
            return _userTokens.ContainsKey(username) ? _userTokens[username] : null;
        }
    }
}