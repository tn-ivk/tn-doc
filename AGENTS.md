# Repository Guidelines

> **Note**: This file is maintained for compatibility. Primary documentation is in [CLAUDE.md](CLAUDE.md).

Use [CLAUDE.md](CLAUDE.md) for the authoritative rules:
- Build, test, and development commands
- Architecture overview and module patterns
- Coding conventions and style guides
- Commit and PR guidelines
- Configuration and security notes

## Critical Rules (mirrored)
- Never mention AI or automation in commit messages
- Never add "Co-Authored-By" or similar attribution in commits
- Commit messages must be in Russian

## Essential Commands

```bash
# Build and run
dotnet build
cd TN_Doc && dotnet run

# Git submodules (required after clone)
git submodule update --init --recursive

# Vue components (from TN_Doc/Client/)
npm install && npm run build:all
npm run dev:editor

# Testing
dotnet test
dotnet test --filter "ClassName=TestClass"
dotnet test --filter "Namespace~KMH"
```

## Architecture Quick Map

```
TN_Doc/            ASP.NET Core web app (controllers, services, configs, templates)
TN_Doc/Client/     Vue 3 apps (statusbar/configurator/document-editor/shared)
tn.docgeneral/     Git submodule with document libraries
tn_toolsfastreport Git submodule with FastReport utilities
winprutil/         Git submodule with Windows printing
Tests/             NUnit tests
```

## Key Paths
- App config: `TN_Doc/Cfg/CfgApp.json`
- Doc configs: `TN_Doc/Cfg/Cfg*.json`, `TN_Doc/Cfg/CfgEdit*.json`
- Templates: `TN_Doc/Doc/*.frx`
- Vue editor: `TN_Doc/Client/document-editor/`
- DI config: `TN_Doc/Startup.cs`
- Logging: `TN_Doc/nlog.config`

## Project Docs
- Entry point: `docs/README.md`
- Setup/build: `docs/development/setup.md`, `docs/development/building.md`
- API: `docs/api/endpoints.md`
- Passport config: `docs/configs/passport.md`
- Field history: `docs/features/field-history.md`
