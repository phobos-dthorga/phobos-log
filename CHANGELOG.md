# Phobos Log changelog

Versions are tags of this repository; consumers pin a tag.

## [Unreleased]

### Changed

- **The LICENSE adds two paragraphs** (phobos-modkit's shared licence rule, the owner's decisions of 20 November 2026): our own art and sound are CC BY 4.0, credited to House D'thorga, and the D'thorga names and preview images are not part of either grant, so a fork or reupload uses its own name. The code stays MIT; nothing granted before is withdrawn.

## [1.0.1] - 2026-11-09

### Fixed

- A trace line reading exactly "at in x" made a message's signature throw. It no longer does.

## [1.0.0] - 2026-11-08

### Changed

- **Incompatible:** the namespace is now Phobos.Logging (it was Phobos.Log). A namespace called Phobos.Log captured the word Log inside every Phobos namespace, so a mod's calls to its game's own Log class stopped compiling. Consumers change their using lines, and name a static field Logger, not Log. Nothing else changed.

## [0.1.0] - 2026-11-09

The first version, at the owner's request (9 November 2026), from phobos-rimworld's logging record.

### Added

- PhobosLog: one log per mod or part, under the category "mod" or "mod: part", written through a host's sink; Debug lines only while the host says debug is on.
- OnceGate and Throttle (structs kept beside a call: once a session, or once per so many game ticks), WarnOnce and ErrorOnce keyed by their caller's file and line, and PhobosLog.NewSession() to open them again in a new game.
- LogBudget: lines per window of real time by level, shared by a mod's parts, with one line saying how many were held back.
- RecentLog: the last 200 lines of every log in the process.
- LogSignature and StormTally: a message's signature (first line and the exception's first frame, numbers blanked) and a bounded count of them, moved from D'thorga Framework.
- 11 tests.
