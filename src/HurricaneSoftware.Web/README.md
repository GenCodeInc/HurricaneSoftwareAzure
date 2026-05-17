# HurricaneSoftware.Web

Standalone Blazor WebAssembly front-end for the public HurricaneSoftware.com website.

## Target hosting

This project is designed for Azure Static Web Apps Free tier:

- static-only front-end publish output
- HTTPS and custom domain handled by Static Web Apps
- public routes preserved in Blazor rather than ASP.NET Web Forms
- dynamic behavior moved into the existing `TropicalStorms.Api` host under `api/website`

## Preserved public routes

The app keeps both clean routes and the main legacy `.aspx` entry points where they still matter:

- `/` and `/Default.aspx`
- `/screenshots` and `/MoreImages.aspx`
- `/download` and `/Download.aspx`
- `/awards` and `/Awards.aspx`
- `/faq` and `/FAQ.aspx`
- `/tv-promotions` and `/TVPromotions.aspx`
- `/register`, `/RegisterNow.aspx`, and `/Registration.aspx`
- `/lost-registration` and `/LostYourRegCode.aspx`
- `/confirm-email` and `/ConfirmEmail.aspx`
- `/registration/complete` and `/RegistrationComplete.aspx`
- `/contact` and `/ContactUs.aspx`

## Website API dependency

The front-end expects the existing ASP.NET Core API to expose the website-only endpoints added in this repo:

- `POST /api/website/registration/recover/acs`
- `POST /api/website/contact`
- `POST /api/website/alerts/confirm`
- `POST /api/website/orders/quote`
- `POST /api/website/orders/paypal/create`
- `POST /api/website/orders/paypal/capture`

The public website now uses the ACS-backed registration recovery route so website-originated mail is isolated from the legacy SMTP path.

By default the WebAssembly app uses:

- local: `http://127.0.0.1:5085/`
- hosted fallback: `https://webservice.hurricanesoftware.com/`

Override with `wwwroot/appsettings.json` if needed.

## Local run

From the repo root:

```powershell
dotnet run --project .\src\TropicalStorms.Api\TropicalStorms.Api.csproj --urls http://127.0.0.1:5085
dotnet run --project .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj
```

Then open the local website URL printed by the Blazor app.

## Static Web Apps notes

- `wwwroot/staticwebapp.config.json` contains SPA fallback and header rules.
- Publish output is the standard standalone Blazor `wwwroot` payload.
- The site is intentionally static; only the existing API host performs dynamic work.

## PayPal note

The old site used legacy direct credit-card PayPal APIs inside Web Forms code-behind. The new website flow is intentionally safer:

- the browser never collects raw card data for the site itself
- the API creates and captures PayPal checkout orders
- registration issuance happens only after capture succeeds

Set `TropicalStorms:Website:PayPal:*` in the API configuration before using live checkout.
