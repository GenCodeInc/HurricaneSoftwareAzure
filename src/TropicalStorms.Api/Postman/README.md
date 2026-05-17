# TropicalStorms Postman Package

This folder now contains four import-ready Postman collections with fixed URLs or simple collection variables and prefilled sample data.

Import these collections:

- `TropicalStorms Local - API and SOAP.postman_collection.json`
- `TropicalStormsAzure.postman_collection.json`
- `TropicalStorms Azure Linux - API and SOAP.postman_collection.json`
- `TropicalStorms Website API.postman_collection.json`

Why it is structured this way:

- no environment selection is required
- each collection is already pointed at the correct host
- local and Azure each include both JSON API and SOAP requests
- `TropicalStormsAzure` targets the live production custom domain
- the Linux Azure collection targets the direct Linux App Service hostname for platform-only checks

Included coverage:

- common JSON API GET requests
- WSDL check
- SOAP 1.1 and SOAP 1.2 `HelloWorld`
- `GetGISData`
- `StormNames`
- `GetStorm`
- `GetCoordinates`
- `Storms`
- `GetStormNames`
- `GetStormsDataset`
- `ImageLinks` with `GoesVisFull`
- website registration recovery
- website contact form
- website order quote

Prefilled sample data:

- username: `demo`
- password: `demo`
- region: `All`
- stormID: `8136`
- activeOnly: `true`

Suggested use:

1. Import all three collections.
2. Start with `WSDL Contract Check` in the SOAP folder.
3. Run the JSON API folder for local, Azure production, or the direct Linux Azure host.
4. Run the SOAP folder for local, Azure production, and the direct Linux Azure host when you want to compare behavior.

Notes:

- The local collection expects the app at `http://127.0.0.1:5085`.
- `TropicalStormsAzure` targets `https://webservice.hurricanesoftware.com`.
- The Linux Azure collection targets `https://api-tropicalstorms-linux-cu66c7.azurewebsites.net`.
- The website API collection defaults `baseUrl` to `https://webservice.hurricanesoftware.com` and lets you override the email and other sample values at the collection level.
