# SecureVault — Password Manager

A fully featured, end-to-end encrypted password manager with:
- **MAUI app** — runs natively on Android, iOS, Windows, macOS
- **Browser extension** — Chrome & Firefox auto-fill on any site

---

## Security Model

- **AES-256-GCM** encryption for every password field
- **PBKDF2** (600,000 iterations, SHA-256) key derivation from your master password
- **Your master password never leaves your device** — only the encrypted blob is synced
- Breach checking via [HaveIBeenPwned](https://haveibeenpwned.com/) k-anonymity API
- Auto-lock after configurable inactivity
- Clipboard auto-clear after 30 seconds
- Biometric unlock (Face ID / Touch ID / Windows Hello)

---

## Building the MAUI App

### Prerequisites
- Windows 10/11 or macOS 13+
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community edition is free)
- .NET 8 SDK with MAUI workload:
  ```
  dotnet workload install maui
  ```

### Steps
1. Open `PasswordManager/PasswordManager.sln` in Visual Studio 2022
2. Restore NuGet packages (automatic on first build)
3. Select your target platform (Android Emulator, iOS Simulator, Windows Machine)
4. Press **F5** to build and run

### Android
- Enable Developer Options on your phone → USB Debugging
- Select your device from the toolbar and press Run
- After first install, go to **Settings → Passwords → AutoFill Service** and enable SecureVault

### iOS
- Requires a Mac with Xcode 15+
- Pair your iPhone in Xcode first
- Enable the Credential Provider Extension in **Settings → Passwords → AutoFill Passwords**

---

## Installing the Browser Extension

### Chrome / Edge
1. Open `chrome://extensions/`
2. Enable **Developer mode** (top right)
3. Click **Load unpacked**
4. Select the `BrowserExtension/` folder
5. The 🔐 icon appears in your toolbar

### Firefox
1. Open `about:debugging#/runtime/this-firefox`
2. Click **Load Temporary Add-on**
3. Select `BrowserExtension/manifest.json`

> For permanent Firefox installation, sign the extension at [addons.mozilla.org](https://addons.mozilla.org/developers/)

---

## Sync Providers

Configure in **Settings tab** of the MAUI app.

### Firebase (recommended — real-time sync)
1. Go to [console.firebase.google.com](https://console.firebase.google.com)
2. Create a project → Enable Realtime Database
3. Enable Email/Password authentication
4. Copy **Project ID** and **Web API Key** into app Settings
5. Enter your Firebase email + password

### Google Drive
1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Enable the Drive API → Create OAuth 2.0 credentials
3. Use the OAuth flow to obtain an access token
4. Paste the token into Settings

### GitHub
1. Go to GitHub Settings → Developer Settings → Personal Access Tokens
2. Create a token with `repo` scope
3. Create a **private** repository for your vault
4. Enter token, username, and repo name in Settings

### Custom Provider
Add any REST endpoint that accepts `GET /vault` and `PUT /vault` with a bearer token.

---

## Auto-fill

### Android
The app registers as an Android AutofillService. After install:
- Go to **Settings → System → Languages & Input → Advanced → AutoFill Service**
- Select **SecureVault**

### iOS
- After install, go to **Settings → Passwords → AutoFill Passwords**
- Enable **SecureVault** as a credential provider

### Desktop (via Browser Extension)
- The 🔐 button appears in password fields automatically
- Tap it to pick credentials or auto-fill the current site
- Works in Chrome, Edge, Firefox, Brave

---

## Features

| Feature | Status |
|---------|--------|
| AES-256-GCM encryption | ✅ |
| PBKDF2 key derivation | ✅ |
| Firebase sync | ✅ |
| Google Drive sync | ✅ |
| GitHub sync | ✅ |
| Custom REST provider | ✅ |
| Password generator | ✅ |
| Passphrase generator | ✅ |
| TOTP / 2FA codes | ✅ |
| HaveIBeenPwned breach check | ✅ |
| Biometric unlock | ✅ |
| Auto-lock | ✅ |
| Clipboard auto-clear | ✅ |
| Android AutoFill service | ✅ |
| iOS Credential Provider | ✅ |
| Browser extension (Chrome/Firefox) | ✅ |
| Password strength scoring | ✅ |
| Vault export/import backup | ✅ |
| Favorites & search | ✅ |
| Categories & tags | ✅ |
| Custom fields | ✅ |
| Secure notes | ✅ |

---

## Project Structure

```
SecureVault/
├── PasswordManager/          # .NET MAUI app
│   └── PasswordManager/
│       ├── Models/           # VaultEntry, VaultSettings
│       ├── Services/         # Encryption, storage providers, TOTP
│       ├── ViewModels/       # MVVM ViewModels (CommunityToolkit.Mvvm)
│       ├── Views/            # XAML pages
│       ├── Converters/       # Value converters
│       ├── Platforms/
│       │   ├── Android/      # AutofillService
│       │   ├── iOS/          # CredentialProviderExtension
│       │   └── Windows/      # NativeMessagingHost for browser extension
│       └── Resources/        # Fonts, styles, colors
└── BrowserExtension/         # Chrome/Firefox extension (MV3)
    ├── manifest.json
    ├── background.js          # Service worker
    ├── content.js             # Page injection & auto-fill
    └── popup/                 # Extension popup UI
```
