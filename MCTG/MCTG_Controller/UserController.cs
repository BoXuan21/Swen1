using System;
using System.Collections.Generic;

namespace MCTG
{
    public class UserController
    {
        private readonly Dictionary<string, string> _userEndpoints = new Dictionary<string, string>();

        // Add or update user endpoint
        public void AddOrUpdateUserEndpoint(string userId, string endpoint)
        {
            if (_userEndpoints.ContainsKey(userId))
            {
                _userEndpoints[userId] = endpoint;
                Console.WriteLine($"Endpoint für Benutzer {userId} aktualisiert: {endpoint}");
            }
            else
            {
                _userEndpoints.Add(userId, endpoint);
                Console.WriteLine($"Neuer Benutzer hinzugefügt: {userId}, Endpoint: {endpoint}");
            }
        }

        // Get user endpoint
        public string GetUserEndpoint(string userId)
        {
            return _userEndpoints.ContainsKey(userId) ? _userEndpoints[userId] : null;
        }

        // Get all user endpoints (for debugging or admin purposes)
        public Dictionary<string, string> GetAllUserEndpoints()
        {
            return new Dictionary<string, string>(_userEndpoints);
        }
    }
}