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
