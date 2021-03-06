## v6.0.1
* `Flurl.Http` dependency updated to 3.0.1.

## v5.3.1
* API methods now check for null or whitespace only values for required URL parameters.

## v5.1.2
* PR #58: Makes `PlaceBuy.Amount` nullable. Documentation states that `PlaceBuy.Amount` or `PlaceBuy.Total` can be specified but not both. Thanks Nemeth!

## v5.1.1
* Issue #55: Add `client.HoistResponse(out var msg)` that can be used to get the underlying `HttpResponseMessage`. Useful for advanced scenarios where you want to manually check low-level HTTP headers, HTTP status code or inspect the response body manually.

## v5.1.0
* Issue #54: Better support for Pagination with `client.GetNextPageAsync` helper.

## v5.0.8
* PR #53: Ensure all `.CreatedAt` and `.UpdatedAt` fields are nullable to prevent deserialization exception errors.

## v5.0.7
* Issue #52: `Coinbase.Models.Account.CreatedAt`/`.UpdatedAt` now nullable to avoid JSON deserialization error when JSON response contains null for these fields. 

## v5.0.6
* `EnableFiddlerDebugProxy` now uses the `proxyUrl` parameter instead of hard-coded `localhost` when debugging HTTP requests. 

## v5.0.5
* Issue #51 - Fixed model mismatch in `Sells.PlaceSellOrderAsync` and `PaymentMethods.ListPaymentMethodsAsync`.
* Added `EnableFiddlerDebugProxy` method on `CoinbaseClient` to help debug HTTP calls.
* Updated Newtonsoft dependency to v12. 

## v5.0.4
* Ensures all requests have a `User-Agent` string, including `Data` requests.
* Added missing `CancellationToken` to `GetCurrenciesAsync()` 

## v5.0.3
* PR #47: Extra DI interfaces for DI support. Notification event and transaction status constants. Changed `Transaction.Description` return from object to string. Thanks granthoff1107! 

## v5.0.2
* Fixed possible "Operation is not supported on this platform" exception from being thrown in `WebhookHelper.IsValid()` on .NET Core platform. `Rsa.ImportParameters(CoinbasePublicKey)` used instead of `Rsa.FromXmlString()`.

## v5.0.1
* This library now has full model support for all Coinbase v2 APIs including Wallet and Data Endpoints. See: https://github.com/bchavez/Coinbase for more info.
* Version 5.x is carries with it breaking changes now that OAuth API authentication is supported.
* Dropped RestSharp dependency for Flurl.Http.

## v5.0.0-beta-6
* PR 44: Add callback method OnRefresh when client renews oauth token. 

## v5.0.0-beta-5
* PR 41: OAuth Auto Refresh tokens supported.
* Renamed `CoinbaseApi` to `CoinbaseClient`.
* `OAuthHelper` static class added. 

## v5.0.0-beta-4
* Issue 35: Create a new URL for each request.

## v5.0.0-beta-3
* Issue #33: AllowAnyHttpStatusCode() to allow parsing of error body instead of throwing.
* Corrected `currency` model in `accounts` to parse object instead of string.
* Updated `Transaction` model.

## v5.0.0-beta-2
* Promotes `CoinbaseApi` as a full client. Allows mutation of request before request is sent.

## v5.0.0-beta-1
**BREAKING CHANGES**
* Complete re-write of Conbase's API according to current documentation.
* Supported Platforms: .NET Standard 2.0 or .NET Framework 4.5 or later.
* Replaced RestSharp dependency with Flurl.Http.
* OAuth and API Key + Secret authentication mechanisms both supported.
* Please see GitHub readme for more information: https://github.com/bchavez/Coinbase
* Newtonsoft reference updated.

## v3.0.1
* .NET Core compatibility.
* Migrated from .NET Framework 4.0 to 4.5.2.
* Newtonsoft reference update.

## v2.0.6:
* Update Newtonsoft Reference.

## v2.0.5:
* Update Newtonsoft Reference.

## v2.0.4:
* Issue #21: CoinbaseResponse now uses JToken instead of JObject for better compatibility across other APIs that might return a JArray or JObject.
* Issue #21: Fixed "invalid signature" when calling some API endpoints.

## v2.0.3:
* API Aesthetics: Easier usage of CheckoutRequest.Metadata

## v2.0.2:
** BREAKING CHANGES **
* API Aesthetics: Swaped arguments to SendRequest(endpoint, opts)

## v2.0.1:
** BREAKING CHANGES **
* Coinbase API v2. Please see below:

## v2.0.1-beta-2
** BREAKING CHANGES **
* For MVC projects, please use Request.InputStream to extract the callback JSON and pass the JSON as an argument to api.GetNotification() and inspect the returnValue.UnverifiedOrder.
* For Web API projects, you can use the Notification class directly as model.

## v2.0.1-beta-1
** BREAKING CHANGES **
* Compatibility with Coinbase API v2.
* Deprecated Coinbase.Mvc project. Please replace uses of CoinbaseCallback with the Notification class (in Coinbase.ObjectModel).
* [JsonNetBinder] (or any other binder) is no longer needed when processing callbacks.
* Checkout redirect URLs are generated by api.GetCheckoutUrl(response), where response is the return value from api.CreateCheckout() and api is CoinbaseApi.
* You can now send raw requests to any endpoint: /order, /time, /wallet by using api.SendRequest(body, endpoint, httpMethod)
* Dependency on FluentValidation removed.

## v0.3.14:
* Added debug symbols and source to symbolserver.org for easier debugging experience.
* Updated RestSharp reference.

## v0.3.12:
* Removed obsolete constructor.
* Fixed #18 - CoinbaseApi constructor having null baseURL.

## v0.3.11:
* Improved Refund API support
* Added Order.Status.Expired for merchant callbacks.

## v0.3.10:
* Added Refund API functionality.
* Added SendMoney API functionality.
* Added GetOrder API functionality.
* Updated Nuget references.
- Pull Requests from: ryanmwilliams

## v0.3.7:
** BREAKING CHANGES **
* Moved MVC specific code into new NuGet library. Please use Coinbase.Mvc for your MVC projects.
* Issue #3 fixed - ASCII HMAC encoding now uses UTF8.
* Issue #4 fixed - Input string not a valid integer.
* Using new http://api.coinbase.com API endpoint.
* Third-party references updated: Newtonsoft, RestSharp
* Coinbase's ObjectModel updated.
* Namespace refactorings: Order, ButtonReqeust, etc are now in Coinbase.ObjectModel.
* Support for handling subscription payments.
* Using FluentValidation (not signed) to ease with versioning/upgrades.
- Pull Requests from: ElanHasson

## v0.2.5:
* Updated all Nuget Package Dependencies
* Removed missing App.config file from Coinbase.Tests project

## v0.2.4:
* Added support for new API Key + Secret.
* Deprecated single API Key

## v0.2.3:
* Added "mispaid" status to Status enum to avoid parsing error.

## v0.2.2:
* Added JsonNetBinderAttribute.cs for binding CoinbaseCallbacks.
* Made CreatedAt nullable to prevent deserialization errors.
* Updated documentation.

## v0.2.1:
* Fixing nuget deployment issue.

## v0.2.0:
* Removed Microsoft WebAPI HttpClient (version issues)
* Replaced with RestSharp

## v0.1.0:
* Bug fix in GetCheckoutUrl()

## v0.1.0:
* Initial implementation for payment checkout URL and button creation.