# Repository Guidelines

## Project Structure & Module Organization
`TN_Doc/` hosts the ASP.NET Core app (Controllers, Services, Views, wwwroot, `Client/statusbar`). Shared logic lives in `TN.DocGeneral/`, and document engines remain under `tn.docgeneral/*` so `Tests/Tests.csproj` can reference them. Data adapters sit in `Ivk.DataBase/`. Keep documentation in `docs/`, release artefacts in `distrib/`, and FastReport templates plus configuration under `TN_Doc/Doc` and `TN_Doc/Cfg`.

## Build, Test, and Development Commands
Use `dotnet restore && dotnet build TN_Doc.sln` after dependency updates. Start the backend with `dotnet run --project TN_Doc/TN_Doc.csproj`; create distributables with `dotnet publish -c Release -o distrib/out`. Run test suites through `dotnet test` (add `--logger:"console;verbosity=detailed"` for flaky cases) and produce coverage via `dotnet test /p:CollectCoverage=true`. Build the status bar separately: `cd TN_Doc/Client/statusbar && npm install && npm run build`.

## Coding Style & Naming Conventions
Follow the ReSharper profile in `TN_Doc.sln.DotSettings`: 4‑space indentation, braces on separate lines, and expression-bodied members only when clearer. Use PascalCase for public types/methods, camelCase for locals, `_field` for private members, and suffix async methods with `Async`. Name controllers after features and mirror template folders for document modules (`KMH_PR_PU`). Vue components stay in PascalCase, directories in kebab-case. Run `dotnet format` (or the IDE ReSharper pass) before committing shared code.

## Testing Guidelines
NUnit 4 with Moq and coverlet powers the `Tests/` suite. Name fixtures/files with the `*Tests` suffix (`UsersTests.cs`), keep Arrange-Act-Assert sections explicit, and reuse helpers from `Tests/Fixtures/`. When adding FastReport flows, place regression specs in the matching namespace (`Tests/Services/Kmh`). Run `dotnet test /p:CollectCoverage=true` before submitting risky refactors and attach reports if failures appear.

## Commit & Pull Request Guidelines
History shows single-sentence, capitalized messages scoped to one concern (e.g., `Рефакторинг компонента истории изменений`). Prefer Russian phrasing for product logic, optionally prefixing with the area (`Poverka: ...`). PRs must summarize intent, link the issue/task, mention configuration changes (`CfgApp.json`, SQL migrations, templates), and include UI screenshots when relevant. Always state which of `dotnet test`, `npm run build`, or deployment scripts ran locally.

## Configuration & Security Notes
Environment-specific secrets belong in `TN_Doc/Cfg`; only placeholders (similar to `pass.txt`) should reach Git. FastReport templates inside `TN_Doc/Doc` are client-delivered, so coordinate renames with release engineering and keep supporting fonts under `fonts/`. Document new OPC or ELIS endpoints inside `docs/integration/` and hide diagnostic logging behind configuration switches to avoid leaking plant data.
