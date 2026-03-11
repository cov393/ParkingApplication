# ParkingApplication — Technical Documentation

A cross-platform mobile application for discovering and viewing parking locations, originally developed as a dissertation project. The app provides user authentication and an interactive map of parking spots, primarily focused on the Coventry/Birmingham area.

---

## Table of Contents

- [Technical Overview](#technical-overview)
- [Solution Architecture](#solution-architecture)
- [Technology Stack](#technology-stack)
- [Authentication](#authentication)
- [Data Layer](#data-layer)
- [Navigation & UI Structure](#navigation--ui-structure)
- [Maps & Location](#maps--location)
- [Platform-Specific Configuration](#platform-specific-configuration)
- [Build & Run](#build--run)
- [Design Artifacts](#design-artifacts)

---

## Technical Overview

| Aspect | Details |
|--------|---------|
| **Framework** | Xamarin.Forms 5.x |
| **Shared code** | .NET Standard 2.0 |
| **Platforms** | Android (API 30 / v11), iOS |
| **Backend / auth** | Firebase (Authentication, Realtime Database, Firestore) |
| **Maps** | Xamarin.Forms.GoogleMaps v2 |
| **Min. tooling** | Visual Studio 2019+ (Xamarin workload), .NET Standard 2.0 SDK |

The application follows a single shared-library approach: UI, business logic, and service abstractions live in the `App1` (.NET Standard) project; platform heads (`App1.Android`, `App1.iOS`) handle lifecycle, native SDK initialization, and optional platform-specific code.

---

## Solution Architecture

```
ParkingApplication/
├── App1.sln
├── App1/                    # Shared .NET Standard 2.0 library
│   ├── App.xaml(.cs)        # Application entry, DI registration
│   ├── AppShell.xaml(.cs)   # Shell navigation (Flyout + TabBar)
│   ├── MainPage.xaml(.cs)   # Legacy landing (Login/Registration entry)
│   ├── View/                # Pages (Login, Registration, Home)
│   ├── Services/            # IDataStore + MockDataStore
│   ├── Models/              # Domain models (e.g. Item)
│   └── Database/            # Embedded JSON (parking locations)
├── App1.Android/            # Android head (MainActivity, assets, manifest)
└── App1.iOS/                # iOS head (AppDelegate, Info.plist, assets)
```

- **App1**: All XAML views, view-models (where used), services, and models. No direct platform APIs; platform access is via Xamarin.Essentials or dependency injection where applicable.
- **App1.Android / App1.iOS**: Thin entry points that initialize Xamarin.Forms, Xamarin.Essentials, Google Maps (and on Android, FFImageLoading), then run the shared `App` and `AppShell`.

---

## Technology Stack

### Core

- **Xamarin.Forms** (5.0.0.2401) — UI framework, Shell navigation, data binding, XAML compilation.
- **.NET Standard 2.0** — Shared contract for the core library; ensures compatibility with current Xamarin Android/iOS targets.

### Backend & Auth

- **FirebaseAuthentication.net** (3.7.2) — Email/password sign-in and registration; token handling and refresh.
- **FirebaseDatabase.net** (4.0.7) — Firebase Realtime Database client (included for future or alternate use).
- **Google.Cloud.Firestore** (2.5.0) — Firestore client (available for structured parking/location data).

### Maps & Location

- **Xamarin.Forms.GoogleMaps.v2** (3.4.5) — Native Google Maps on Android and iOS, pins, camera, map types. Requires platform-specific initialization and, on iOS, an API key.

### Cross-Platform Utilities

- **Xamarin.Essentials** (1.7.3) — Preferences (e.g. token persistence), device info, and other shared APIs.
- **Newtonsoft.Json** (13.0.1) — JSON serialization (e.g. Firebase auth payload to `Preferences`).

### Imaging (Android)

- **Xamarin.FFImageLoading.Forms** (2.4.11.982) — Cached image loading; initialized in `MainActivity` for Android.

---

## Authentication

Authentication is implemented with **Firebase Authentication** (email/password).

### Flow

1. **Registration** (`Registration.xaml.cs`): `FirebaseAuthProvider.CreateUserWithEmailAndPasswordAsync` creates the account; on success the user is directed to the Login page.
2. **Login** (`Login.xaml.cs`): `FirebaseAuthProvider.SignInWithEmailAndPasswordAsync` signs in; the refreshed auth content is serialized and stored via `Preferences.Set("MyFirebaseRefreshToken", ...)`.
3. **Post-login**: User is navigated to `Home` (map). Authenticated state is implied by the presence of the stored token (commented code in `App.xaml.cs` suggests future use for conditional root page).

### Security considerations

- **API key usage**: The Firebase Web API key is currently in code (e.g. `Login.xaml.cs`, `Registration.xaml.cs`). For production, move it to a secure configuration (e.g. environment, build-time config, or a backend that issues tokens) and restrict key usage in Firebase Console (HTTP referrer / app restrictions).
- **Token storage**: `Xamarin.Essentials.Preferences` is used for the serialized auth payload; for sensitive tokens, consider platform-specific secure storage (e.g. Keychain/Keystore) via Essentials or custom implementations.

---

## Data Layer

### Abstraction

The app uses a generic repository-style interface for items:

- **`IDataStore<T>`** (`Services/IDataStore.cs`): `AddItemAsync`, `UpdateItemAsync`, `DeleteItemAsync`, `GetItemAsync`, `GetItemsAsync(bool forceRefresh)`.
- **`MockDataStore`**: In-memory implementation for `Item` (id, text, description), registered in `App.xaml.cs` with `DependencyService.Register<MockDataStore>()`. No Firebase/Firestore integration in the current code; this is the default data store for list/item use cases.

### Parking locations

- **Source**: `App1/Database/ParkingSpaceLocation.json` — embedded JSON with an array of parking locations (address, label, latitude/longitude).
- **Usage**: The JSON defines multiple locations (Coventry/Birmingham area). The **Home** map currently builds pins in code (e.g. a single hardcoded pin) rather than loading this JSON; the JSON is the intended data source for “free parking space near user” and similar features and can be wired to `Home` via a small loader (e.g. `Assembly.GetManifestResourceStream` + Newtonsoft.Json).

---

## Navigation & UI Structure

- **Shell** (`AppShell.xaml`): Single `Shell` with:
  - **Flyout**: `FlyoutItem` entries for Home, Login, Registration (each with `ShellContent` and route).
  - **TabBar**: Two `TabBar` sections, each with one `ShellContent` (Login and Registration again), so the same pages are reachable from tabs and flyout.
- **Routes**: `Home`, `Login`, `Registartion` (note typo in route name). `Routing.RegisterRoute(nameof(Home), typeof(Home))` is called in `AppShell` constructor so `Home` can be used for programmatic navigation.
- **Entry points**: Main app starts with `MainPage = new AppShell()`. `MainPage` (legacy) offers buttons to push `Login` and `Registration` modally; the primary entry is Shell.

### Key views

| View | Role |
|------|------|
| **Home** | Map page; displays parking pins and camera region. |
| **Login** | Email/password fields; Firebase sign-in; on success navigates to `Home`. |
| **Registration** | Email/password sign-up; on success redirects to `Login`. |

---

## Maps & Location

- **Control**: `Xamarin.Forms.GoogleMaps` `<map:Map>` in `Home.xaml`; code-behind in `Home.xaml.cs` adds a `Pin` (type `Place`, label “Parking lot”, position, optional rotation and tag) and sets the visible region with `MoveToRegion(MapSpan.FromCenterAndRadius(..., Distance.FromMeters(1000)))`.
- **Data**: Pin data is currently hardcoded. The app includes `Database/ParkingSpaceLocation.json` with many locations; these can be deserialized and turned into pins and optionally filtered by user location (e.g. “free parking space near user”).
- **Platform init**:
  - **Android**: `Xamarin.FormsGoogleMaps.Init(this, savedInstanceState)` in `MainActivity.OnCreate`. No API key is passed here (key is often set in Android manifest or Maps SDK config).
  - **iOS**: `Xamarin.FormsGoogleMaps.Init("AIzaSy...")` in `AppDelegate.FinishedLaunching` — the Google Maps API key is required for iOS.

---

## Platform-Specific Configuration

### Android (`App1.Android`)

- **Target**: Android 11 (v11.0).
- **MainActivity**: `FormsAppCompatActivity`; initializes `CachedImageRenderer` (FFImageLoading), `Xamarin.FormsGoogleMaps`, Xamarin.Essentials, then Xamarin.Forms and `App`.
- **Permissions**: `OnRequestPermissionsResult` forwarded to Xamarin.Essentials (e.g. for location when you add “near user” features).
- **Manifest**: Configure location and network permissions as required for maps and Firebase.

### iOS (`App1.iOS`)

- **AppDelegate**: `Forms.Init()` then `Xamarin.FormsGoogleMaps.Init("<Google Maps API Key>")`, then `LoadApplication(new App())`.
- **Capabilities**: Enable Maps and any required background/location capabilities in the project and entitlements if you use user location.

---

## Build & Run

- **IDE**: Visual Studio 2019 or later with Xamarin workload (Android + iOS).
- **Solution**: Open `App1.sln`. Set startup project to `App1.Android` or `App1.iOS` and choose a device/emulator.
- **Android**: Build and run on emulator or device (API 11+); ensure Google Play Services / Maps SDK and Firebase are configured for the app’s package and signing.
- **iOS**: Build and run on simulator or device; ensure the Google Maps API key is valid and the key is restricted appropriately in Google Cloud Console.
- **Shared project**: No extra steps; it is referenced by both platform projects.

---

## Design Artifacts

The following diagrams and mockups illustrate application flow and UI design (from the original dissertation):

### Application flowchart

![Application flowchart](https://user-images.githubusercontent.com/74015697/211216827-e6f53691-8691-4b5e-85b5-e19f1bc25102.png)  
![Application flowchart (continued)](https://user-images.githubusercontent.com/74015697/211216853-18bb1bf5-005e-4878-a1e1-1070c0c2731d.png)

### Initial design mockup

![Initial design mockup](https://user-images.githubusercontent.com/74015697/211216873-7db9b4fc-dc05-483e-9956-ea3ae573a80f.png)

### Final design interaction workflow

![Final design interaction workflow](https://user-images.githubusercontent.com/74015697/211216888-3c8c2c01-7f0b-41e1-b1c3-2938fadde740.png)

### Positions from Locations database

![Positions from Locations database](https://user-images.githubusercontent.com/74015697/211216918-d2a9b030-1c3d-455f-83fc-6698148ce858.png)

### Registration.xaml.cs (logic)

![Registration.xaml.cs](https://user-images.githubusercontent.com/74015697/211216937-90714550-e8d1-464d-9a8a-c976abf7d3b5.png)  
![Registration.xaml.cs (continued)](https://user-images.githubusercontent.com/74015697/211216946-bd5261d2-e73d-42fb-ac9b-1101cbb3b76b.png)

### Login.xaml.cs (logic)

![Login.xaml.cs](https://user-images.githubusercontent.com/74015697/211216967-03cee318-3de3-410d-8895-5bc5d69ce5de.png)  
![Login.xaml.cs (continued)](https://user-images.githubusercontent.com/74015697/211216974-fa60305d-e0a2-43f2-8c03-f98728f12962.png)

### Restriction for unauthenticated users

![Restriction for unsigned users](https://user-images.githubusercontent.com/74015697/211217001-5487b6f6-0b8d-49b5-a8e0-23d60f9c3f1b.png)

### Free parking space near user loader

![Free parking space near user loader](https://user-images.githubusercontent.com/74015697/211217048-13dac2f0-07cb-4007-848d-41bfafad8e5d.png)

---

*This document describes the architecture, stack, and technical decisions of the ParkingApplication project as implemented. For product or UX details, refer to the design artifacts above and the dissertation materials.*
