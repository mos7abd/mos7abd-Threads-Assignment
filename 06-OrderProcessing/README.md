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