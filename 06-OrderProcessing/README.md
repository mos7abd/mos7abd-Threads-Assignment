# Mini Project — Order Processing System

## 1. Project Overview

### Target Framework
- .NET 10.0

### Project Structure
Explain the folder structure and the responsibility of each project, folder, and important class.

Include:
- Shared Models
- Interfaces
- Services
- Repositories
- Storage
- Utilities
- Logging
- Exceptions
- Phase 1
- Phase 2
- Phase 3
- Phase 4

### How to Run
Explain how to build and run the application and how to select each phase from the main menu.

### Data Folder
Explain where the runtime `Data` folder is created, how its location is determined, and which JSON files it contains.

---

## 2. Storage Design

### File List
Document every JSON file, its purpose, and the phase that uses it.

### JSON Format
Describe the structure of the stored records and provide representative JSON examples.

### Concurrent File Access
Explain how `JsonFileStore` protects concurrent updates using:
- `ConcurrentDictionary` to maintain file-specific locks.
- `SemaphoreSlim` to coordinate access to each file.
- `WaitAsync(ct)` to support asynchronous waiting and cancellation.
- Temporary files and atomic replacement to prevent corrupted or partially written JSON files.

Explain how these mechanisms prevent lost updates and file corruption.

---

## 3. Performance Results

Include actual measurements from all required experiments.

### Phase 1 — Sync vs Async

| Metric | Synchronous | Asynchronous |
|---|---:|---:|
| Execution Time | 83.1 seconds | 97.3 seconds |
| Total Orders | 20 | 20 |
| Validated | 15 | 15 |
| Rejected | 5 | 5 |

### Phase 2 — Pricing and Concurrent Processing

Document:
- Sequential vs concurrent pricing measurements.
- Concurrent processing time: 7.057 seconds.
- Completed: 13.
- Rejected: 6.
- Failed: 1.

### Phase 3 — Sync vs Async

Include the actual measurements for:
- Synchronous processing using 10 threads.
- Asynchronous processing using 10 tasks.
- Execution time.
- Distinct thread IDs.
- `ThreadPool.ThreadCount`.

### Phase 4 — Dashboard, Shipping, Fraud Scoring and Email

Include:
- Customer Dashboard execution time and concurrency measurements.
- Fastest Shipping Quote execution time and distinct threads.
- Fraud Scoring sequential vs parallel execution time.
- Email Re-send execution time and maximum concurrent sends.

For every experiment, include the actual values for elapsed time, distinct threads, and `ThreadPool.ThreadCount` where applicable.

---

## 4. Console Screenshots

Add screenshots organized by phase.

### Phase 1
- Phase 1 execution.

### Phase 2
- Race condition demonstration.
- Faulted task.
- Payment timeout.
- Refund.

### Phase 3
- Email retry.
- Fire-and-forget before fix.
- Fire-and-forget after fix.

### Phase 4
- Task 10.1 — Live Checkout Progress.
- Task 10.2 — Customer Dashboard.
- Task 10.3 — Fastest Shipping Quote.
- Task 10.4 A — Fraud Scoring.
- Task 10.4 B — Email Re-send.
- One cancelled run using `--cancel-after`.

---

## 5. Problems Encountered and Solutions

Document the actual problems discovered during development and how each one was resolved.

For each problem, describe:
1. The problem.
2. Why it happened.
3. The solution.
4. How the solution was verified.

---

## 6. Questions and Answers

Include all questions and answers from:
- Phase 1.
- Phase 2.
- Phase 3.
- Phase 4.

Keep the original question wording and organize the answers under their corresponding phases and tasks.

---

## 7. Sample Output

Include the final JSON files for the required project states.

Organize them under:

`sample-output/`

Document which phase or task each sample represents.

---

## Final Verification Checklist

- [ ] Project targets `net10.0`.
- [ ] All four phases can be selected and executed from the menu.
- [ ] Runtime Data folder location is documented.
- [ ] Project structure and class responsibilities are documented.
- [ ] Storage design and JSON formats are documented.
- [ ] Concurrent file access protection is explained.
- [ ] All required performance measurements are included.
- [ ] All required console screenshots are included.
- [ ] Development problems and solutions are documented.
- [ ] All questions from Phases 1–4 are answered.
- [ ] `sample-output/` contains the final JSON files for the required states.


## Phase 1 Questions

### 1. Why are the sync and async times almost the same when orders are awaited one after another?

Both versions process orders sequentially. The synchronous version blocks the current thread during `Thread.Sleep()`, while the asynchronous version uses `await Task.Delay()` and allows the thread to do other work while waiting. However, because each order must finish validation before the next begins, the asynchronous version does not overlap the waiting periods between orders. Therefore, their execution times can be similar, especially when simulated delays dominate the total time. The async version also includes saving each order, which adds extra time.

### 2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.

No. In an asynchronous method, the continuation after an `await` may run on a different thread, depending on the synchronization context and task completion. When an awaited operation completes, the runtime schedules the remaining code according to the applicable context and scheduler. My logs should be used to compare the thread IDs before and after an `await` to determine whether the thread changed during my run.

### 3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?

While `Task.Delay()` is waiting, the method is suspended without blocking a thread just to wait for the delay. The thread can return to the thread pool and handle other work. In contrast, `Thread.Sleep()` blocks the current thread for the specified duration, preventing it from doing other work during that time.

### 4. Why should a repository method return Task<T> instead of T?

A repository method that performs asynchronous I/O should return `Task<T>` so callers can use `await` without blocking a thread while the operation is in progress. This improves scalability when handling many concurrent requests. Returning `T` is appropriate for operations that complete synchronously and do not need asynchronous execution.

### 5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?

`File.ReadAllText()` performs synchronous file reading and blocks the calling thread until the operation completes. `File.ReadAllTextAsync()` provides an asynchronous API that allows the caller to await completion without blocking the calling thread during the I/O wait. The asynchronous version does not normally need to keep a thread occupied solely while waiting for the disk I/O to complete, although the exact implementation depends on the operating system and runtime.

--------------------------------------------------------------------------
### phase2 Task 8.4 (6)
### Phase 1 vs. Phase 2 Performance

#### Phase 1 — Validation

| Metric | Measured Time |
|  ---   |     ---:      |
| Synchronous validation | 83.1 seconds |
| Asynchronous validation | 97.3 seconds |
| Total orders | 20 |
| Validated | 15 |
| Rejected | 5 |
| Orders saved in `orders.json` | 20 |

#### Phase 2 — Concurrent Processing

| Metric | Measured Result |
|---|---:|
| Concurrent processing time | 7.057 seconds |
| Completed | 13 |
| Rejected | 6 |
| Failed | 1 |
| Orders saved in `orders.json` | 20 |

#### Analysis

Phase 1 measured synchronous and asynchronous validation separately. In this run, asynchronous validation took 14.2 seconds longer than synchronous validation. Asynchronous execution does not automatically guarantee a shorter elapsed time; the result depends on the workload, implementation, and timing boundaries.

Phase 2 processed validated orders concurrently and recorded a processing duration of 7.057 seconds. This duration measures the processing stage, so it should not be directly compared with Phase 1's validation timings as if they measured identical work.

Both phases confirmed that `orders.json` contains exactly 20 records. Phase 2's final distribution was 13 completed, 6 rejected, and 1 failed order.


### Task 8.5 Why `Task.Run()` Fits Here but Not for Database Calls

`Task.Run()` is appropriate for the invoice-rendering simulation because the loop of 20,000,000 iterations is **CPU-bound**: it performs calculations that consume processor time. `Task.Run()` schedules this work on a ThreadPool thread.

Database calls are generally **I/O-bound**: the application spends much of the time waiting for the database or network. Use the database provider's asynchronous methods, such as `ExecuteReaderAsync()` or Entity Framework's `SaveChangesAsync()`, instead of wrapping database calls in `Task.Run()`. This avoids occupying a ThreadPool thread just to wait for I/O.

# Phase 2 — Questions and Answers

### 1. What is the difference between starting a task and awaiting it?

Starting a task begins the operation and returns a `Task` representing its execution. Awaiting a task waits asynchronously for it to complete and retrieves its result, if it has one. Starting multiple tasks before awaiting them allows their operations to overlap instead of running sequentially.

### 2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?

No, creating a `Task` does not necessarily create a new thread. A task represents an asynchronous operation or a unit of work. `Task.Run()` typically schedules CPU-bound work on a ThreadPool thread, while I/O operations can complete asynchronously without occupying a thread while waiting.

`Task.Delay()` does not need a dedicated thread while waiting. It completes after the specified time, unless canceled.

### 3. What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it dangerous?

`.Result` blocks the current thread until the task completes and returns its result. `.Wait()` also blocks the current thread but does not return a result.

Blocking can cause ThreadPool starvation when many threads are waiting for tasks that need ThreadPool threads to continue. In some synchronization contexts, it can also cause deadlocks. Using `await` is generally preferred because it waits asynchronously without blocking the current thread.

A small separate demo method is provided below. This is the only place where `.Result` and `.Wait()` should be used for this demonstration.

```csharp
static async Task<int> GetNumberAsync()
{
    await Task.Delay(500);
    return 42;
}

static void BlockingDemo()
{
    Task<int> task = GetNumberAsync();

    int result = task.Result; // Blocks until completion.
    Console.WriteLine(result);

    Task anotherTask = Task.Delay(500);
    anotherTask.Wait(); // Blocks until completion.
}
```

### 4. What happens to an exception thrown inside a task that is never awaited?

An exception from a task is stored in the task and is observed when the task is awaited or its exception is accessed. If a faulted task is never awaited or otherwise observed, the application may fail to handle the error properly, and the failure can go unnoticed. Exceptions should be observed and handled appropriately.

### 5. When should you use Task.Run(), and when should you avoid it?

Use `Task.Run()` mainly for CPU-bound work that needs to run on a ThreadPool thread, such as intensive calculations.

Avoid wrapping naturally asynchronous I/O operations, such as database calls or `File.WriteAllTextAsync()`, in `Task.Run()` just to make them asynchronous. Prefer their native asynchronous APIs. In server applications, unnecessary `Task.Run()` calls can add scheduling overhead without improving scalability.

### 6. What is the difference between Task and Task<T>?

`Task` represents an asynchronous operation that does not return a result value.

`Task<T>` represents an asynchronous operation that returns a result of type `T` when it completes.

For example, `Task` can represent saving a file, while `Task<int>` can represent calculating and returning an integer.

### 7. Why can a lock block not contain an await?

C# does not allow `await` inside a `lock` block because `lock` relies on holding a thread-owned monitor until the block exits. An `await` can suspend the method and resume it later, potentially on a different thread, making that locking model unsuitable.

For asynchronous coordination, use `SemaphoreSlim` with `WaitAsync()` and release it in a `finally` block.

### 8. Why did the unprotected stock reservation oversell, and how did you prevent it?

The unprotected reservation oversold because multiple tasks could read the same available stock before any of them updated it. Each task could conclude that enough stock was available, allowing the combined reservations to exceed the actual quantity. This is a race condition.

The problem was prevented by protecting the stock check and update as one coordinated operation using an appropriate synchronization mechanism, such as `SemaphoreSlim`. This ensures that competing reservations cannot simultaneously pass the availability check and reserve the same stock. The semaphore must be released in a `finally` block so it remains available even if an exception occurs.


## Task 9.6 — Sync vs async comparison

### Execution Results

Both synchronous and asynchronous versions processed the first 10 incoming orders with failures disabled.

| Metric | Sync | Async |
|---|---:|---:|
| Total Orders | 10 | 10 |
| Completed | 8 | 8 |
| Rejected | 2 | 2 |
| Elapsed Time | 11,450 ms | 9,541 ms |
| Distinct Thread IDs | 10 | 10 |
| ThreadPool Before | 4 | 4 |
| ThreadPool After | 4 | 11 |

Both versions produced the expected results: 8 completed orders and 2 rejected orders.

### Questions & Answers

**Q1: Does async make processing a single order faster? Explain why or why not.**

No, async does not necessarily make a single order process faster. Its main benefit is that it does not block a thread while waiting for I/O operations, such as database or file access. This allows the application to use available threads more efficiently.

**Q2: Why does async scale better than synchronous processing when handling multiple orders?**

Async scales better because threads are not blocked while waiting for I/O operations to complete. Instead, they can be released to handle other work. This reduces unnecessary thread usage and helps the application handle more concurrent operations efficiently.

**Q3: What would happen if we processed 1,000 orders using the synchronous approach?**

Processing 1,000 orders synchronously could require a large number of dedicated threads if each order runs on its own thread. This increases memory consumption and context switching overhead. It may also lead to resource exhaustion and reduced application performance.

**Q4: Why is async essential when working with databases, files, and external APIs?**

Database, file, and external API operations often involve waiting for external resources. Using async allows the application to release the current thread during these waiting periods instead of blocking it. This improves scalability, resource utilization, and application responsiveness.

### Conclusion

In this experiment, the asynchronous implementation completed the 10 orders in 9,541 ms, compared to 11,450 ms for the synchronous implementation.

Async was approximately 1,909 ms faster in this run. However, the main advantage of async is not necessarily reducing the execution time of a single operation; it is improving scalability and thread utilization when handling I/O-bound operations.\

## Task 9.7 — Fire-and-forget investigation

### Console Output Analysis

**Before Fix:**

The email sending operation started, but the method returned before the email was completed.

The output showed:

- Email sending started.
- "Order processing finished" was printed.
- The email completed afterward.

This demonstrates that the caller does not wait for a fire-and-forget operation.

**After Fix:**

The method awaited the email operation.

The output showed:

- Email sending started.
- Email sent successfully.
- "Order processing finished" was printed afterward.

This confirms that awaiting the email task ensures the operation completes before the method returns.

### Phase 4 – Task 10.1: Live Checkout Progress

**Implementation:**
- Started processing all 20 orders concurrently.
- Used an unbounded `Channel<OrderProcessingResult>` to publish each order's result immediately after processing completes.
- A dedicated consumer reads results from the channel and prints them in completion order.
- Used `Task.WhenAll` once to wait for all processing tasks to finish before completing the channel and printing the final status summary.
- Each result includes the processed order and its elapsed processing time.

**Verification:**
- All 20 orders were processed.
- Results were printed incrementally in completion order, not input order.
- Final status counts matched the expected Task 9.4 end state:

| Status | Count |
|---|---:|
| Pending | 0 |
| Validated | 0 |
| Rejected | 6 |
| PaymentFailed | 2 |
| Completed | 11 |
| Failed | 1 |
| **Total** | **20** |

**Key Design Decision:**
`Channel` handles real-time result delivery, while `Task.WhenAll` coordinates the completion of all processing tasks. No repeated `Task.WhenAny` loop or unfinished-task list rebuilding is used.

## Phase 4 – Task 10.2: Customer Dashboard

### Implementation
Implemented `CustomerDashboardService.LoadAsync` to load customer profile, orders, payments, and emails concurrently.

The four operations are started before waiting for their completion. `Task.WhenAll` is used to wait for all operations, and the elapsed time is measured using `Stopwatch`.

The service also collects and reports all exceptions when multiple operations fail.

### Test 1: Customer #3 — Successful Dashboard

**Actual console output:**
```text
Dashboard loaded in 0.80 seconds.
Customer #3 Omar Hassan
Orders: 2
Payments: 2
Emails: 1
```

**Result:** Passed.

The returned dashboard contains:
- 2 orders: #3 Completed and #13 Failed.
- 2 payments: #3 Captured and #13 Refunded.
- 1 email: #3.

### Test 2: Customer #99 — Multiple Failures

**Failure scenarios:**
- Customer profile lookup fails because Customer #99 does not exist.
- Payments loading is configured to throw `TimeoutException("Payments store timeout")`.

**Actual console output:**
```text
Dashboard loading failed:
Customer #99 not found
Payments store timeout
```

**Result:** Passed.

Both exceptions were reported, demonstrating that the dashboard does not stop reporting failures after encountering the first exception.

### Concurrency Verification

The logs show that all four operations started almost simultaneously:

| Operation | Start time |
|---|---|
| Customer | 09:32:12.319 |
| Orders | 09:32:12.320 |
| Payments | 09:32:12.321 |
| Emails | 09:32:12.322 |

The successful dashboard completed in approximately **0.80 seconds**, close to the duration of the slowest operation rather than the sum of all four operation durations.

This confirms the expected concurrent execution behavior.

### Task 10.3 – Fastest Shipping Quote: Concurrency Design Decision

**Selected Tool: `Task.WhenAny`**

The shipping scenario involves three independent carriers competing to provide a quote. The system must select the first successful response without waiting for slower carriers to finish.

`Task.WhenAny` is the appropriate choice because it allows the service to react whenever any carrier task completes. If a carrier fails, the service can continue waiting for the remaining tasks. Once a successful quote is received, the service cancels all other active carrier requests.

This approach satisfies the live system's requirement to return the fastest successful shipping quote while respecting the 1.2-second time budget and supporting caller cancellation.

**Why not await tasks one by one?**

Awaiting carrier tasks sequentially would make the result depend on the order in which carriers are awaited, rather than their actual completion times. A faster response from another carrier could be ignored while the service waits for a slower one.

**Design Decision:**

Use `Task.WhenAny` to identify completed carrier operations dynamically, combined with `CancellationTokenSource` to cancel unnecessary requests and enforce the time budget.


## Task 10.4 — Fraud Scoring and Email Re-send

### A — Fraud Scoring (CPU-bound)

**Scenario:**

The system needs to evaluate 10,000 orders to identify potentially fraudulent transactions. Each order receives a fraud score, and orders with a score of 90 or higher are flagged for review.

**Implementation:**

- Created 10,000 orders in memory with IDs from 1 to 10,000 without using any files.
- Implemented the exact scoring algorithm provided in the assignment.
- First, processed all orders sequentially using a regular `foreach` loop and measured the execution time.
- Then, processed the same orders in parallel using `Parallel.ForEach`.
- Used `ParallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount` to distribute the CPU-bound work across available processor cores without exceeding the specified concurrency limit.
- Used `ConcurrentBag<int>` to collect flagged order IDs safely from multiple threads without losing or duplicating results.
- Used `ConcurrentDictionary<int, byte>` to track the distinct thread IDs involved in the parallel execution.
- Passed the `CancellationToken` through `ParallelOptions` to support cancellation of the parallel operation.

**Why these tools?**

Fraud scoring is CPU-bound because it involves intensive calculations rather than waiting for external operations. `Parallel.ForEach` is suitable for distributing independent calculations across multiple processor cores. `ConcurrentBag` provides thread-safe collection, while `ConcurrentDictionary` allows tracking distinct threads safely.

**Actual Results:**

| Metric | Result |
|---|---:|
| Total Orders | 10,000 |
| Flagged Orders | 1,005 |
| Sequential Execution Time | 5,985 ms |
| Parallel Execution Time | 542 ms |
| Speed-up | 11.04x |
| Distinct Threads Used | 20 |
| Environment.ProcessorCount | 20 |
| Results Match | True |

The first five flagged order IDs were:

`4, 8, 12, 29, 33`

Both sequential and parallel executions produced exactly the same 1,005 flagged orders.

The parallel implementation completed the workload approximately 11 times faster than the sequential implementation.

Cancellation was also tested successfully using a `CancellationTokenSource`.

---

### B — Email Re-send (I/O-bound)

**Scenario:**

The system needs to resend confirmation emails for 30 test orders, with order IDs ranging from 1001 to 1030. All emails must be sent successfully without injected failures, while limiting concurrent sends to a maximum of four.

**Implementation:**

- Created 30 test orders and their corresponding customer records in memory.
- Reused the existing `IEmailService` implementation through `EmailService`.
- Disabled injected failures using `failuresEnabled: false`.
- Used `Parallel.ForEachAsync` to process the email jobs asynchronously.
- Configured `ParallelOptions.MaxDegreeOfParallelism = 4` to ensure that no more than four email sends run concurrently.
- Passed the cancellation token to `Parallel.ForEachAsync` and forwarded the loop token to `SendConfirmationAsync`.
- Used `Interlocked.Increment` and `Interlocked.Decrement` to observe the current number of active sends.
- Used `Interlocked.CompareExchange` with `Volatile.Read` to track the highest observed concurrency without controlling or limiting it manually.
- Reused the existing `JsonFileStore` to append email records safely to `emails.json`.

**Why these tools?**

Email sending is I/O-bound because each operation spends time waiting for simulated rendering, SMTP connection, sending, and file operations.

`Parallel.ForEachAsync` allows asynchronous operations to run concurrently while enforcing the configured maximum degree of parallelism.

`Interlocked` is used only to monitor active operations and record the maximum concurrency; it does not control how many operations can run simultaneously.

The existing `JsonFileStore` uses a per-file `SemaphoreSlim` to protect concurrent file updates and prevent lost updates or file corruption. This lock protects data integrity and is separate from the concurrency limit applied to email sending.

**Actual Results:**

| Metric | Result |
|---|---:|
| Total Emails | 30 |
| Successful Sends | 30 |
| Failed Sends | 0 |
| Max Concurrent Sends | 4 |
| Total Execution Time | 8,499 ms |

All 30 email sends completed successfully on their first attempt.

**Execution Time Explanation:**

Each email has three simulated I/O delays:

- Rendering: 200 ms
- SMTP connection: 300 ms
- Sending: 500 ms

Total simulated delay per email is approximately 1 second.

With 30 emails and a maximum concurrency of four:

`30 ÷ 4 × 1 second ≈ 7.5 seconds`

The actual execution time was approximately 8.5 seconds due to additional processing and file-update overhead.

**Note:** The existing email records are preserved when new messages are appended to `emails.json`. The final JSON contents still need to be verified to confirm that all 30 new order IDs appear exactly once.