# Assignment 05 — Answers

Student name: Mostafa Abdellah  
Group / ID: 4

## Failures you observed

- **View counting:** The expected view count was 50,000, but the actual count was inconsistent and much lower. Before the fix, the observed counts included 16,038, 18,710, and 47,201. This happened because multiple threads updated the regular Dictionary concurrently, causing race conditions and lost updates.
- **Email jobs:** Although 20 email jobs were placed in the queue, some runs reported only 19 unique sent emails and 19 total sends. The regular Queue was accessed by multiple workers without proper synchronization, causing concurrency issues when checking and removing jobs.

## What you changed

- Replaced `Dictionary<string, int>` with `ConcurrentDictionary<string, int>` and used `AddOrUpdate` to increment product view counts safely under concurrent access.
- Replaced `Queue<string>` with `ConcurrentQueue<string>` and used `TryDequeue` to retrieve email jobs safely without separately checking the queue count.
- Kept a `lock` around `SentEmails.Add` because `List<string>` is not thread-safe and multiple workers may write to it concurrently.
- Tested the application multiple times after the changes. Each tested run reported 50,000 actual views, 20 unique sent emails, and 20 total sends out of 20 placed jobs, with no exceptions observed.

## Collection safety vs business-rule safety

A thread-safe collection protects its individual operations, but it does not automatically guarantee that a complete business operation is correct. A business rule may involve multiple operations that must work together, and another thread can change the shared state between those operations.

For example, checking a queue's `Count` and then calling `Dequeue` are two separate operations. Another worker could remove a job after the count check but before the dequeue operation. Using `ConcurrentQueue.TryDequeue` makes retrieving an available job a single thread-safe operation.

Similarly, `ConcurrentDictionary.AddOrUpdate` safely updates a product's view count, but more complex business rules involving multiple values or steps may still require additional synchronization.

Therefore, thread safety must be considered at the business-operation level, not only at the collection level. The chosen synchronization mechanism must protect the complete operation required by the business rule.