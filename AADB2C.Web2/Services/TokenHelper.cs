//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C Authentication Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using AADB2C.Web2.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AADB2C.Web2.Services
{
    /// <summary>
    /// Static class used to assist in retrieving access tokens by authorization code
    /// or refresh token
    /// </summary>
    public static class TokenHelper
    {
        private static HttpClient _httpClient = new HttpClient();

        public static async Task<TokenResult> GetAccessTokenByAuthorizationCode(string authority, string clientId, string clientSecret, string authorizationCode, string redirectUri, string scope)
        {
            TokenResult tokenResult = null;
            scope = "offline_access " + scope; // Add the offline_access scope so we get a refresh token
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{authority}/oauth2/v2.0/token");
            List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            keyValues.Add(new KeyValuePair<string, string>("client_id", clientId));
            keyValues.Add(new KeyValuePair<string, string>("scope", scope));
            keyValues.Add(new KeyValuePair<string, string>("code", authorizationCode));
            keyValues.Add(new KeyValuePair<string, string>("redirect_uri", redirectUri));
            keyValues.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            keyValues.Add(new KeyValuePair<string, string>("responseType", "token id_token"));

            httpRequestMessage.Content = new FormUrlEncodedContent(keyValues);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                tokenResult = JsonConvert.DeserializeObject<TokenResult>(responseContent);
            }

            return tokenResult;
        }

        public static async Task<TokenResult> GetAccessTokenByRefreshToken(string authority, string clientId, string clientSecret, string refreshToken, string scope)
        {
            TokenResult tokenResult = null;
            scope = "offline_access " + scope; // Add the offline_access scope so we get a refresh token
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{authority}/oauth2/v2.0/token");
            List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
            keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
            keyValues.Add(new KeyValuePair<string, string>("client_id", clientId));
            keyValues.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));
            keyValues.Add(new KeyValuePair<string, string>("scope", scope));
            keyValues.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            httpRequestMessage.Content = new FormUrlEncodedContent(keyValues);
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                tokenResult = JsonConvert.DeserializeObject<TokenResult>(responseContent);
            }

            return tokenResult;
        }
    }
}
