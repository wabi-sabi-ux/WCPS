# WCPS - Web Based Claims Processing System

Small README for local development (capstone/demo).

## Prerequisites
- .NET 8 SDK (or the SDK version your project targets).  
- SQL Server LocalDB or SQL Server instance (for local dev).  
- Optional: Visual Studio / VS Code.

## How configuration works
The app reads the connection string named `DefaultConnection` from configuration:
- `appsettings.json` (should **not** contain production secrets)
- Environment variables (recommended for CI/servers)
- `dotnet user-secrets` (recommended for local dev)

The environment variable name for `DefaultConnection` is:
