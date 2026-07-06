# Eggnine.Common

A small utility library for common .NET operations, including async enumeration, one-way encryption, and range generation.

## 📦 Features

- **AsyncEnumerableExtensions**  
  LINQ-like extensions for working with `IAsyncEnumerable<T>`, including conditional filtering and transformation.

- **Collections.MassDeq**  
  A thread-safe doubly-linked deque with O(1) head-enqueue and O(1) contiguous-segment mass-dequeue (`TryMassDequeue`, splits the list at the first item where a predicate goes false). `foreach`/`ToList()` enumeration takes a detached, immutable snapshot under a single lock acquisition, safe to run concurrently with mutation on the live deque from another thread.

- **Encryption Utilities**  
  One-way salted hashing with verification, using modern .NET cryptographic APIs.

- **Range Generator**  
  Easy generation of sequences of consecutive numbers (e.g., for pagination or index loops).

## ⚖️ License

Licensed under the [MIT License](./LICENSE).

---

Created by [RF](mailto:RF@Eggnine.com) at Eggnine.