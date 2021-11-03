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
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;

namespace AADB2C.Web2.Services
{
    /// <summary>
    /// Token cache implementation that leverages ASP.Net Core Session State
    /// </summary>
    public class TokenSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private string _userId = string.Empty;
        private string _cacheId = string.Empty;
        HttpContext _httpContext = null;

        List<TokenResult> _cache = new List<TokenResult>();

        public TokenSessionCache(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            _userId = userId;
            _cacheId = _userId + "_TokenCache";
            _httpContext = httpcontext;
            Load();
        }

        public List<TokenResult> GetTokenCacheInstance()
        {
            Load();
            return _cache;
        }

        public void Load()
        {
            // Retrieve any existing tokens from session state.
            // Locks are used to ensure thread safety.
            SessionLock.EnterReadLock();
            string cache = _httpContext.Session.GetString(_cacheId);
            if (!string.IsNullOrEmpty(cache))
            {
                _cache = JsonConvert.DeserializeObject<List<TokenResult>>(cache);
            }
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            // Write the tokens to session state.
            // Locks are used to ensure thread safety.
            SessionLock.EnterWriteLock();

            // Reflect changes in the persistent store
            _httpContext.Session.SetString(_cacheId, JsonConvert.SerializeObject(_cache));
            SessionLock.ExitWriteLock();
        }
    }
}
