# Phobos Log contributor instructions

Standing rules for this repository, stated once in their current form. What each version delivered lives in `CHANGELOG.md`. Created
on 9 November 2026 at the owner's request, as a sibling of phobos-modkit and Phobos Scope; its design record is phobos-rimworld's
`docs/development/logging-framework-design.md`.

## What belongs here

- **The game-free logging core only**: categories, levels, gates (once, throttle), budgets, the recent-lines ring, message
  signatures, and later the structured-file writer and its schema. **The core never names a game** and has no dependencies.
- **Hosts stay in their game's repository**: a sink that writes to one game's log (RimWorld's engine categories, BepInEx's logger)
  lives with that game's mods, beside the code that calls `PhobosLog.NewSession()`.
- **No native code and no Rust** (owner, 9 November 2026). Out-of-game readers of our logs belong in phobos-modkit.

## Rules for the code

- **.NET Standard 2.1**, `LangVersion latest`, nullable on, implicit usings off (consumers compile these sources into their own
  projects, so every file carries its own `using` lines and nothing relies on a consumer's settings).
- **Gate first, build second**: a suppressed line costs a compare and allocates nothing; never add an API that builds a message
  before deciding whether to write it.
- **Thread-safe**: hosts may log from any thread.
- Changing a public signature breaks every consumer's build: raise the major version and say what consumers must change.

## Tests and checks

- Every rule has an xunit test; tests use made-up messages, never copied logs.
- Before every commit run `dotnet test tests/Phobos.Log.Tests`, and commit only on its exit code. Build output goes outside the
  repository (`Directory.Build.props`); never commit `bin/` or `obj/`.

## Git, versions and documents

- **Public, MIT** (consumers are public repositories that pin this as a submodule). Never change the repository's visibility.
- Commit and push to `main`; no pull requests unless the owner asks. Never force-push or rewrite history; never move a pushed tag.
- Versions: a fix raises the last number, new functionality the middle, an incompatible change the first. Every change updates
  `CHANGELOG.md` in the same commit. Tag each release `vX.Y.Z`; consumers move their pin to a tag.
- Write plainly: answer first, short sentences, **bold** for must-dos, tables for comparisons. Never use Figma.
