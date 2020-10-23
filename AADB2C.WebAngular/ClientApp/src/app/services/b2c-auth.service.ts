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
import * as Msal from 'msal';

@Injectable({
  providedIn: 'root'
})
export class B2cAuthService {
  applicationId = "{the client ID of your B2C application}"; // B2C application Id
  tenant = "{your Azure AD B2C tenant FQDN}"; // Azure tenant ID
  signUpSignInPolicy = "{your Azure AD B2C Sign Up Sign In policy name}"; // Name of user flow

  // name of scope, taken from the portal
  scopes = ["openid", "profile"];
  b2cScopes = ["{the scope value of your API}"];

  // the creation of this was taken from the ref above.
  authority = "https://{your B2C tenant name}.b2clogin.com/" + this.tenant + "/" +
    this.signUpSignInPolicy;

  msalConfig = {
    auth: {
      clientId: this.applicationId,
      authority: this.authority,
      validateAuthority: false
    }
  };

  loginRequest = {
    scopes: this.scopes
  };

  clientApplication = new Msal.UserAgentApplication(this.msalConfig);

  constructor() { }

  // Trigger a login with Azure AD B2C
  async login() {
    var _this = this;
    var currentUser: Msal.Account;

    currentUser = await _this.clientApplication.loginPopup(_this.loginRequest).then(function (loginResponse: any) {
        return loginResponse.account;
      }, function (error: any) {
        console.log(error);
      });

    return currentUser;
  }

  logout() {
    var _this = this;

    _this.clientApplication.logout();
  }

  // Retrieve the currently signed in user
  getCurrentUser() {
    var _this = this;
    return _this.clientApplication.getAccount();
  }

  // Retrieve an access token for the specified scope(s)
  async getAccessToken() {
    var _this = this;
    var tokenRequest = {
      scopes: _this.b2cScopes
    };
    var accessToken;

    accessToken = await _this.clientApplication.acquireTokenSilent(tokenRequest).then(
      function (tokenResponse: any) {
        console.log(tokenResponse.accessToken);
        return tokenResponse.accessToken;
      }, function (error: any) {
        console.log(error);
        return "Retrieval of access token failed - login first";
    });

    return accessToken;
  }
}
