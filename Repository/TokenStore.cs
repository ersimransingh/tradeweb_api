using System;
using System.Collections.Concurrent;

namespace TradeWeb.API.Repository
{
    public class TokenStore
    {
        private static ConcurrentDictionary<string, string> _activeTokens = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Store token for a user
        public void StoreToken(string username, string token)
        {
            _activeTokens[username] = token; // Store/Update the token
        }

        // Check if a token is valid for a user
        public bool IsTokenValid(string username, string token)
        {
            return _activeTokens.TryGetValue(username, out var storedToken) && storedToken == token;
        }

        // Remove a token when user logs out or session expires
        public void RemoveToken(string username)
        {
            _activeTokens.TryRemove(username, out _);
        }
    }
}
