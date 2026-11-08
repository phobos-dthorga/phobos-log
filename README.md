# Phobos Log

The logging core shared by the Phobos game mods: one log per mod, under its own category, with gates so a fault of ours writes
once instead of every tick, and a budget so it can never become the storm that fills a game's log. Game-free C# (.NET Standard
2.1, no dependencies); each game's mods supply a small sink that writes to that game's log.

## Read this first

- **The namespace is `Phobos.Logging`**, though the repository and folder are phobos-log: a namespace `Phobos.Log` would capture the
  word `Log` inside every `Phobos.*` namespace and break each mod's calls to its game's own `Log` class. For the same reason, **name
  a mod's static field `Logger`, not `Log`.**
- **This is a library of sources, not a mod.** A consumer compiles `src/Phobos.Log/*.cs` into one assembly of its own, so all its
  mods share one copy: phobos-rimworld compiles it into D'thorga Framework, pinned as a git submodule at `external/phobos-log`.
- **No game is named in the core.** A host implements `ILogSink` (where a line goes) and `ILogClock` (game ticks for throttles,
  real seconds for budgets), and calls `PhobosLog.NewSession()` on a new game or a load.
- **Why it exists:** RimWorld stops every mod's logging, its Player.log included, once a session has written 10,000 messages; one
  mod's error repeating every tick reached that in minutes in the owner's colony. The record is phobos-rimworld's
  [logging-framework-design.md](https://github.com/phobos-dthorga/phobos-rimworld/blob/main/docs/development/logging-framework-design.md).
- **Not here:** the structured (JSON-lines) log file and its schema, and the Ostranauts (BepInEx) sink, are later slices; a reader
  for the file would live in [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit). There is no Rust here, and none
  inside any game (the owner's decision of 9 November 2026: a native library would bring per-platform binaries and crash risk for
  no gain over a C# writer).

## Use

```csharp
static readonly PhobosLog Logger = new PhobosLog("D'thorga Patches", "Harrier", sink, clock, budget, () => devMode);

Logger.Info("RimPacts' landing rule now runs through a plain postfix.");

static OnceGate bridgeGone;                      // a static field beside the call
if (Logger.Once(ref bridgeGone)) Logger.Warn("RimPacts' bridge is gone, so its patch is left as it is.");

static Throttle kept;                            // at most once an in-game hour
if (Logger.Every(ref kept, 2500)) Logger.Info("Kept the map of " + map + " open.");

Logger.ErrorOnce("could not size RimPacts' assault: " + error.Message);   // keyed by this call's file and line
```

| Piece | What it does |
| --- | --- |
| `PhobosLog` | One mod's or part's log; category `mod` or `mod: part`; Debug lines only while the host says debug is on |
| `OnceGate`, `Throttle` | Structs kept beside a call: once a session, or once per so many game ticks; the closed path is one compare and builds no string |
| `WarnOnce`, `ErrorOnce` | Once a session per call site, keyed by the caller's file and line |
| `LogBudget` | Lines per window of real time by level, shared by a mod's parts (defaults: ten minutes; 400 Debug and Info, 200 Warning, 50 Error); past it, lines are counted and one line says how many were held back |
| `RecentLog` | The last 200 lines of every log in the process, for a "copy the recent log" action |
| `LogSignature`, `StormTally` | A message's signature (first line plus the exception's first frame, numbers blanked) and a bounded count of them, to name the message behind a storm |

## Build and test

```text
dotnet test tests/Phobos.Log.Tests
```

Build output goes outside the repository (`%LOCALAPPDATA%\phobos-log\target`, or `PHOBOS_LOG_BUILD`). The tests need the .NET 10
SDK; the library itself targets .NET Standard 2.1 so Mono-based games (Unity, BepInEx) can use it too.

## Project map

| Location | Holds |
| --- | --- |
| `src/Phobos.Log/` | The library: `PhobosLog.cs`, `Gates.cs`, `LogBudget.cs`, `RecentLog.cs`, `Storms.cs`, `Hosts.cs` (the sink and clock interfaces) |
| `tests/Phobos.Log.Tests/` | xunit tests of every rule |
| `AGENTS.md` | Contributor rules |

## Licence

MIT, for Phobos-authored work only ([LICENSE](LICENSE)); see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
