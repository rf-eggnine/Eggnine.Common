# Changelog

Format loosely follows [Keep a Changelog](https://keepachangelog.com/). Versions follow SemVer.

## 4.0.0 - 2026-07-07

### Fixed

- **`MassDeq<T>` data-loss bug**: `MassDeqEnumerator.Remove()` (used by `TryRemove` and
  `ICollection<T>.Remove`) called `Clear()` whenever the removed node happened to be the physical
  head of the list — silently wiping every other item in the deque, and resetting `IsReversed`,
  even when the caller only asked to remove one item. Present since `MassDeq<T>` was introduced;
  never caught earlier because no test asserted on the actual contents/count after removing a head
  item, and the one real caller (`GameEngineQueue.TryRemove`) discarded the removed value and never
  hit this exact shape of list. Fixed by rewriting the splice to update `_head`/`_tail` directly
  instead of branching on walk direction.
- **`MassDeq<T>.TryRemove` returned the wrong value**: it returned the deque's current head value
  instead of the value of the node actually removed (coincidentally correct only when the removed
  node *was* the head). Went unnoticed for the same reason as above — nothing asserted on the
  returned value.

### Changed

- **Breaking**: `TryRemove` now returns `bool` with an `out T? removed` parameter, matching the
  `TryXxx` shape used everywhere else in the BCL (`Dictionary.TryGetValue`, `ConcurrentDictionary.
  TryRemove`, `Queue.TryDequeue`) and by `MassDeq<T>.TryDequeue` itself. Previously returned a
  `(bool Success, T? Value)` tuple.
- **Breaking**: `TryMassDequeue`, `InsertBefore`, `Clone`, and `CloneReverse` now take
  `Predicate<T>` instead of `Func<T, bool>`, matching `List<T>`'s predicate-based members
  (`Find`, `RemoveAll`, `Exists`). Existing lambda-literal call sites are source-compatible;
  call sites passing an explicitly-typed `Func<T, bool>` variable will need to change the
  variable's declared type.

### Added

- `IMassDeq<T>` interface, covering everything `MassDeq<T>` offers beyond `ICollection<T>`.
  Also implements `IReadOnlyCollection<T>` directly (`MassDeq<T>` already satisfied it
  structurally), so callers that only need read access no longer have to go through
  `AsReadOnly()` to get an `IReadOnlyCollection<T>`-typed reference.
- `Dequeue()` and `Peek()` — throwing counterparts to `TryDequeue`/`TryPeek`, matching
  `System.Collections.Generic.Queue<T>`'s shape.
- `TryPeek(out T item)` — look at the next item `Dequeue`/`TryDequeue` would return, without
  removing it. `MassDeq<T>` previously had no way to do this at all.
- 15 new unit tests covering `Dequeue`/`Peek`/`TryPeek`, the new `TryRemove` out-param shape,
  `IMassDeq<T>`/`IReadOnlyCollection<T>` conformance, and the two `Remove()` bugs above.

## 3.1.0

- Added `IMassDeq<T>` (superseded by the reshaped version above before this ever shipped as a
  package — no NuGet package has been published from this repo yet, so nothing external depends
  on the 3.1.0 shape).

## 3.0.0

Version bump only, no code changes in the commit itself. Per the bump commit's own message: this
version accounts for accumulated breaking changes since `v2.0.0` that were never given their own
version bump — removal of the public `Encryption`/`IEncryption`/`StringExtensions` types, the
`Range` → `LongRange` rename, and `WhereableAsyncEnumerable.FirstAsync` changing from silently
returning `default` on an empty sequence to throwing.

## 2.0.0 and earlier

Tagged retroactively from existing commit history; not curated release notes. `v2.0.0` in
particular points at a `secret-scan.yml` workflow tweak, not a deliberate package release point.
Treat pre-3.0.0 tags as approximate history markers, not a reliable source of "what changed."

---

**On tag policy**: tags in this repo are never deleted or rewritten once pushed, including for
versions later found to have bugs (see `MassDeq<T>` above) — they're kept as an honest, immutable
record. If/when this package is published to NuGet, buggy versions get *unlisted* (hidden from
new installs, per NuGet's own mechanism for this) rather than deleted, and the fix is documented
here rather than silently folded into the old tag.
