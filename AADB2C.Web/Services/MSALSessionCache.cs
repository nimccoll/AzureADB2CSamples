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
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System.Threading;

namespace AADB2C.Web.Services
{
    /// <summary>
    /// Sample implementation of an MSAL token cache leveraging ASP.Net session state as the backing store
    /// </summary>
    public class MSALSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        string CacheId = string.Empty;
        HttpContext httpContext = null;

        TokenCache cache = new TokenCache();

        public MSALSessionCache(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
            Load();
        }

        public TokenCache GetMsalCacheInstance()
        {
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            Load();
            return cache;
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session.SetString(CacheId + "_state", state);
            SessionLock.ExitWriteLock();
        }
        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            state = (string)httpContext.Session.GetString(CacheId + "_state");
            SessionLock.ExitReadLock();
            return state;
        }
        public void Load()
        {
            // Retrieve any existing tokens from session state.
            // Locks are used to ensure thread safety.
            SessionLock.EnterReadLock();
            byte[] blob = httpContext.Session.Get(CacheId);
            if(blob != null)
            {
                cache.Deserialize(blob);
            }
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            // Write the tokens to session state.
            // Locks are used to ensure thread safety.
            SessionLock.EnterWriteLock();

            // Reflect changes in the persistent store
            httpContext.Session.Set(CacheId, cache.Serialize());
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                Persist();
            }
        }
    }
}