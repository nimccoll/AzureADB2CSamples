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

// Define the AADB2C namespace
var AADB2C = (function ($) {
    var init = function () {

    };

    return {
        init: init
    };
})(jQuery);

// Define the AADB2C.WebJQuery namespace
AADB2C.WebJQuery = (function ($) {
    var userAgentApplication;

    // Handle tokens received from loginRedirect and acquireTokenRedirect (only necessary if not using popup login)
    var tokenReceivedCallback = function (errorDesc, token, error, tokenType) {
        if (token) {
            // Do something
        }
        else {
            console.log(error + ":" + errorDesc);
        }
    };

    // Log the user in to AAD B2C via a popup dialog box
    var login = function (apiScopes) {
        userAgentApplication.loginPopup(apiScopes).then(function (idToken) {
            // login success
            $('#lnkSignIn').hide();
            $('#txtGreeting').html('Hello ' + userAgentApplication.getUser().name + '!');
            $('#lnkSignOut').show();
        }, function (error) {
            // login failure
            console.log(error);
        });
    };

    // Initialize the MSAL client
    var initMSAL = function (clientId, authority) {
        userAgentApplication = new Msal.UserAgentApplication(clientId, authority, tokenReceivedCallback);
    };

    // Initialize the page header
    var initHeader = function (apiScopes) {
        $('#lnkSignIn').on('click', function (event) {
            event.preventDefault();
            login(apiScopes);
        });
        $('#lnkSignOut').on('click', function (event) {
            event.preventDefault();
            userAgentApplication.logout();
            $('#lnkSignIn').show();
            $('#txtGreeting').html('');
            $('#lnkSignOut').hide();
        });
        $('#lnkBrand').on('click', function (event) {
            event.preventDefault();
            $('#divIndex').show();
            $('#divCallAPI').hide();
            $('#divPrivacy').hide();
        });
        $('#lnkHome').on('click', function (event) {
            event.preventDefault();
            $('#divIndex').show();
            $('#divCallAPI').hide();
            $('#divPrivacy').hide();
        });
        $('#lnkPrivacy').on('click', function (event) {
            event.preventDefault();
            $('#divIndex').hide();
            $('#divCallAPI').hide();
            $('#divPrivacy').show();
        });
    };

    // Use JQuery Ajax to invoke the WebAPI
    var getValues = function (accessToken, apiUrl) {
        $.support.cors = true; // Enable CORS support
        $.ajax({
            type: "GET",
            url: apiUrl,
            crossDomain: true, // CORS
            headers: {
                'Authorization': 'Bearer ' + accessToken
            }
        }).done(function (data) {
            $('#ulValues').html('');
            $('#divIndex').hide();
            $('#divCallAPI').show();
            data.forEach(function (item, index) {
                $('#ulValues').append('<li>' + item + '</li>');
            });
            console.log("Web APi returned:\n" + JSON.stringify(data));
        }).fail(function (jqXHR, textStatus) {
            console.log("Error calling the Web api:\n" + textStatus);
        });
    };

    // Call the WebAPI
    var callAPI = function (apiScopes, apiUrl) {
        // If the user is not signed in, trigger a login
        if (userAgentApplication.getUser() === null) {
            login(apiScopes);
        }

        // If the user is signed in, call the WebAPI
        if (userAgentApplication.getUser() !== null) {
            userAgentApplication.acquireTokenSilent(apiScopes).then(function (accessToken) {
                // acquireTokenSilent Success
                getValues(accessToken, apiUrl);
            }, function (error) {
                // acquireTokenSilent Failure, send an interactive request.
                userAgentApplication.acquireTokenPopup(apiScopes).then(function (accessToken) {
                    getValues(accessToken, apiUrl);
                }, function (error) {
                    console.log(error);
                });
            });
        }
    };

    // Initialize the functionality of the Index page
    var initIndex = function (clientId, authority, apiScopes, apiUrl) {
        var scopes = apiScopes.split(' ');
        initMSAL(clientId, authority);
        initHeader(scopes);
        $('#btnCallAPI').on('click', function () {
            callAPI(scopes, apiUrl);
        });
    };

    // Expose the initIndex method
    return {
        initIndex: initIndex
    };
})(jQuery);