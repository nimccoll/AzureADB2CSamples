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
import { Component } from '@angular/core';
import * as Msal from 'msal';
import { B2cAuthService } from '../services/b2c-auth.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {

  CurrentUser: Msal.Account;
  AccessToken: string;
  APIUrl: string = "https://localhost:44315/api/values";
  ResponseValues;

  constructor(private http: HttpClient, private b2cAuthService: B2cAuthService) {

  }

  // Login with Azure AD B2C
  login() {

    var _this = this;

    _this.b2cAuthService.login().then(function (currentUser) {
      if (currentUser) {
        console.log(currentUser.name);
        _this.CurrentUser = currentUser;
      }
    });
  }

  // Logout of Azure AD B2C
  logout() {
    var _this = this;

    _this.b2cAuthService.logout();
    _this.CurrentUser = null;
  }

  // Call REST API protected by Azure AD B2C
  callAPI() {
    var _this = this;

    _this.b2cAuthService.getAccessToken().then(function (accessToken) {
      console.log(accessToken);
      _this.AccessToken = accessToken;
      const headers = { 'Authorization': 'Bearer ' + accessToken }
      _this.http.get<any>(_this.APIUrl, { headers }).subscribe(data => {
        _this.ResponseValues = data;
      })
    });
  }
}
