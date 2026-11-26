# ShopLedger - Phase 1
Open the folder in Visual Studio / VS Code (MAUI workload, .NET 8), then:
dotnet build -t:Run -f net8.0-android
Add AppIcon, Splash and Styles resources from the default MAUI template (not included here).
Folders: Models, Data, Services, ViewModels, Views.

---
## Phase 7 – polish & tests

* **Demo data**: Settings → *Load demo data* (only when the app is empty). *Delete ALL data* wipes everything (make a backup first).
* **Product photo**: Product edit → Gallery / Camera / Remove. Photos are copied into app storage and shown as thumbnails in the list.
* **Appearance**: Settings → Same as phone / Light / Dark.
* **Icon & splash**: simple placeholder SVGs in `Resources/AppIcon` and `Resources/Splash` — replace with your own artwork.
* **Permissions**: microphone and camera are requested when first used; refusing shows a message instead of crashing.
* **iOS** (needs a Mac): `Platforms/iOS/Info.plist` has the usage descriptions. You still need to add `AppDelegate`'s Entitlements/provisioning in Xcode/VS for Mac.

### Running the tests
```
dotnet test ShopLedger.Tests
```
The test project is plain `net8.0` and compiles the app's logic files directly (see its .csproj), so it needs no MAUI workload.
Covers: balance/stock math, LedgerService (credit/cash sale, rollback, credit limit, payments, overpayment, adjustments, purchases),
demo-data invariants, fuzzy matching, voice parsing and date ranges.
**Note:** these tests were written without access to a .NET SDK and have not been run yet — if one fails, the assertion probably needs tuning rather than the app being wrong (voice-parser tests are the most likely).
