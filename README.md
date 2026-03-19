# QuickTrans

QuickTrans is a .NET 8 WPF desktop MVP for quick Chinese translation on Windows.

## Features

- Always-on-top floating translator window
- Draggable input shell with Enter-to-translate behavior
- Translation requests routed through `translate.googleapis.com`
- Expandable result panel under the input box
- Idle auto-collapse into a draggable circular launcher
- Windows CI build validation on every push to `main`

## Tech Stack

- .NET 8
- WPF
- `HttpClient` + `System.Text.Json`

## Project Structure

- `src/QuickTrans.App`: WPF application, view models, services, and infrastructure

## Run

Build and run on Windows with .NET 8 SDK installed:

```powershell
dotnet build QuickTrans.sln
dotnet run --project .\src\QuickTrans.App\QuickTrans.App.csproj
```

The current implementation targets `net8.0-windows` and should be validated on a Windows machine.

## Validation

- Local environment inspection confirmed the app targets Windows via WPF and the Google Translate endpoint contract matches the implementation assumptions.
- GitHub Actions workflow [`.github/workflows/ci.yml`](./.github/workflows/ci.yml) restores and builds the solution on `windows-latest` for push and pull request validation.
