# Kzn Store

Kzn Store is a standalone Windows desktop app and a separate GitHub repository from Kzn Notch. It can be installed, opened, and updated without Kzn Notch. When both are installed, Kzn Notch opens the Store from its tray menu and settings.

## Live app catalog

The Store reads [`catalog/store.json`](catalog/store.json) from this repository. It refreshes on launch, on **Refresh catalog**, and every 15 minutes. The last valid catalog is cached locally for offline use. Committing a catalog change to the default branch makes new apps available on the next refresh; a Store rebuild is not needed.

To add an app, add a product object to `Products` with a unique `Id`, display `Name`, exact four-part MSIX `Version`, package `PackageName`, certificate `Publisher`, `GitHubRepository`, localized `Description`, and its installer `DownloadUrl` plus the matching SHA-256 checksum. Keep downloads on HTTPS. The Store verifies the checksum and the MSIX identity and version before handing the installer to Windows.

```powershell
Get-FileHash -Algorithm SHA256 .\MyApp.msix
```

An empty `DownloadUrl` keeps a product visible but disables installation. The Store never executes code from the catalog. Windows App Installer still asks the user to confirm the installation.

## Build and package

Requires the .NET 10 SDK and Windows 11 SDK Build Tools (`makeappx.exe` and `signtool.exe`).

```powershell
./scripts/build.ps1 -Publish
./scripts/package-msix.ps1 -DevelopmentCertificate
```

Install from the generated `artifacts/KznStore-installer` folder. The development certificate is local-only. Public releases should be signed with the same trusted publisher identity configured in the catalog and in the Notch package.

## Release

Push a tag matching the project version, for example `v1.0.1`, to publish a separately installable Store MSIX. Configure the repository's existing signing secrets `KZN_SIGNING_PFX_BASE64` and `KZN_SIGNING_PASSWORD` with the same publisher certificate used by the Notch.
