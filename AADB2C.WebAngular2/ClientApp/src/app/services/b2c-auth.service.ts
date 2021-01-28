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
import { Injectable } from '@angular/core';
import * as Msal from '@azure/msal-browser';

@Injectable({
  providedIn: 'root'
})
export class B2cAuthService {
  applicationId: string = "{the client ID of your B2C application}"; // B2C application Id
  tenant: string = "{your Azure AD B2C tenant FQDN}"; // Azure tenant ID
  signUpSignInPolicy: string = "{your Azure AD B2C Sign Up Sign In policy name}"; // Name of user flow

  // name of scope, taken from the portal
  b2cScopes = ["{the scope value of your API}"];

  // the creation of this was taken from the ref above.
  authority = "https://{your B2C tenant name}.b2clogin.com/tfp/" + this.tenant + "/" +
    this.signUpSignInPolicy;

  msalConfig: Msal.Configuration = {
    auth: {
      clientId: this.applicationId,
      authority: this.authority,
      knownAuthorities: [this.authority]
    }
  };

  loginRequest = {
    authority: this.authority,
    scopes: this.b2cScopes
  };

  clientApplication: Msal.IPublicClientApplication = new Msal.PublicClientApplication(this.msalConfig);

  currentUser: Msal.AccountInfo;

  constructor() { }

  // Trigger a login with Azure AD B2C
  async login() {
    var _this = this;

    try {
      const loginResponse = await _this.clientApplication.loginPopup(_this.loginRequest);
      _this.currentUser = loginResponse.account;
    }
    catch (err) {
      console.log(err);
    }

    return _this.currentUser;
  }

  // Trigger a logout from Azure AD B2C
  logout() {
    var _this = this;

    _this.clientApplication.logout();
  }

  // Retrieve the currently signed in user
  getCurrentUser() {
    var _this = this;
    if (!_this.currentUser) {
      var accounts: Msal.AccountInfo[];

      accounts = _this.clientApplication.getAllAccounts();
      if (accounts.length > 0) {
        _this.currentUser = accounts[0];
      }
    }

    return _this.currentUser;
  }

  // Retrieve an access token for the specified scope(s)
  async getAccessToken() {
    var _this = this;
    var accessToken: string;
    var currentUser: Msal.AccountInfo = _this.getCurrentUser();

    if (currentUser) {
      var tokenRequest: Msal.SilentRequest = {
        account: currentUser,
        scopes: _this.b2cScopes
      };

      try {
        const tokenResponse = await _this.clientApplication.acquireTokenSilent(tokenRequest);
        accessToken = tokenResponse.accessToken;
      }
      catch (err) {
        console.log(err);
        accessToken = "Retrieval of access token failed - login first";
      }
    }

    return accessToken;
  }
}
