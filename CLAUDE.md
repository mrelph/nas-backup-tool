# NAS Backup Tool

Windows Forms desktop app (.NET 6, `net6.0-windows`) that backs up local folders to a NAS over UNC paths, with optional AI duplicate analysis via Amazon Bedrock (Claude models).

## Commands

All commands require Windows (WinForms target). This repo is often edited from macOS — building/running here will fail unless `EnableWindowsTargeting` is set, and the app itself only runs on Windows.

- `dotnet build --configuration Release` — build
- `dotnet run` — run the GUI app
- `dotnet publish -c Release -r win-x64 --self-contained` — self-contained build

There are no tests, no linter config, and no CI.

## Architecture

Flat single-project layout (`NASBackup.csproj`, namespace `NASBackup`):

- `Program.cs` — entry point, launches `MainForm`
- `MainForm.cs` — all UI (tabs: Backup, Duplicates, Schedule, Settings); also owns duplicate *deletion* logic (`RemoveFilesAsync`, `RemoveDuplicatesAsync`)
- `BackupEngine.cs` — incremental copy (compares LastWriteTime + size), 1MB buffered streams, preserves timestamps
- `DuplicateAnalyzer.cs` — MD5 exact-dup detection, name/size similarity heuristic, optional Bedrock `InvokeModel` call for recommendations
- `BackupConfig.cs` — JSON config at `%APPDATA%\NASBackup\config.json`; passwords/AWS secret encrypted with Windows DPAPI to `%APPDATA%\NASBackup\credentials.dat`

## Conventions

- No nullable reference types (`<Nullable>disable</Nullable>`); implicit usings on
- Progress/status reported via C# events (`ProgressChanged`, `StatusChanged`, `LogMessage`)
- Bedrock model IDs live in `BackupConfig.BedrockModel` and the hardcoded combo-box list in `MainForm.cs` (~line 363). Only use model IDs confirmed to exist on Bedrock; do not invent new ones. Bedrock request format is `anthropic_version: bedrock-2023-05-31` messages API.

## DESTRUCTIVE OPERATIONS — read before touching

This tool deletes and overwrites real user files. When testing or modifying:

- `File.Delete` in `MainForm.RemoveFilesAsync` permanently deletes files flagged as duplicates — no recycle bin, no undo.
- `config.AutoRemoveDuplicates` (Settings checkbox) auto-deletes "duplicate" files during backup, keeping only the newest per group. The similarity heuristic (name+size) can flag non-identical files.
- `BackupEngine.CopyFileAsync` uses `FileMode.Create` — it overwrites destination files on the NAS unconditionally once a file is deemed changed.
- Never run backups or duplicate removal against real NAS/user paths while testing; use throwaway temp directories.

## Gotchas

- `BackupConfig.cs` lines 48 and 74 contain `$\"aws_secret_{...}\"` (literal backslash-escaped quotes) — invalid C#; the project does not compile until fixed.
- `ProtectedData` (DPAPI) is Windows-only; keep any refactor Windows-guarded.
- csproj references `AWS.Tools.BedrockRuntime` (the PowerShell module package) rather than `AWSSDK.BedrockRuntime`, with a mismatched `AWSSDK.Core 3.7.0` — verify package choices before adding AWS features.
- No `.gitignore` — do not commit `bin/` or `obj/` after building.
- NAS credential auth (`ConnectWithCredentialsAsync`) is a stub: it creates a `NetworkCredential` but never calls `WNetAddConnection2`, so authenticated shares rely on Windows having cached credentials.
- Secrets (NAS password, AWS secret key) must never be written to `config.json`; `Password` is `[JsonIgnore]` and secrets go through DPAPI storage only.
