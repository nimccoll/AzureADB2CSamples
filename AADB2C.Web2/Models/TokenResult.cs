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
namespace AADB2C.Web2.Models
{
    public class TokenResult
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int not_before { get; set; }
        public int expires_in { get; set; }
        public int expires_on { get; set; }
        public string resource { get; set; }
        public string profile_info { get; set; }
        public string scope { get; set; }
        public string refresh_token { get; set; }
        public int refresh_token_expires_in { get; set; }
    }
}
