# Assignment 03 — Locking Research

Student name: Mostafa Abdellah  
Group / ID: 4

## Q1 — What is `System.Threading.Monitor` used for?

`System.Threading.Monitor` is used to synchronize access to shared resources between threads. It allows only one thread at a time to enter a critical section protected by the same lock object.

It provides methods such as `Enter` and `Exit` to acquire and release a lock, and `TryEnter` to attempt to acquire a lock with an optional timeout. It also provides `Wait`, `Pulse`, and `PulseAll` for coordinating threads based on changes in shared state.

## Q2 — What does the C# `lock` statement compile to?

For a traditional reference-type lock object, the C# `lock` statement is conceptually implemented using `Monitor.Enter` and `Monitor.Exit`, with a `try/finally` block to ensure the lock is released even if an exception occurs.

Starting with C# 13 and .NET 9, when the expression is of type `System.Threading.Lock`, the compiler uses the lock type's `EnterScope()` mechanism instead.

Therefore, the exact implementation depends on the C# version and the type of the lock expression.

## Q3 — List at least two differences between using `lock` and calling `Monitor` APIs directly.

1. **Automatic release:** The `lock` statement automatically ensures that the lock is released when execution leaves the block, including when an exception occurs. When using `Monitor` directly, the developer must correctly release the lock in a `finally` block.

2. **More control:** `Monitor` provides methods such as `TryEnter`, which allows a thread to attempt to acquire a lock without waiting indefinitely. A regular `lock` statement does not provide a timeout option.

3. **Thread coordination:** `Monitor.Wait`, `Monitor.Pulse`, and `Monitor.PulseAll` allow threads to wait for a condition and notify other waiting threads. A normal `lock` statement only provides mutual exclusion; it does not directly provide these notification operations.

## Q4 — When might a developer use `Monitor.TryEnter`, `Monitor.Pulse`, or `Monitor.Wait` instead of a plain `lock`?

- **`Monitor.TryEnter`:** When a thread should attempt to acquire a lock but must not wait indefinitely. It can use a timeout and perform another action if the lock cannot be acquired in time.

- **`Monitor.Wait`:** When a thread needs to wait until a particular condition becomes true. It releases the lock while waiting and reacquires it before returning.

- **`Monitor.Pulse`:** When a thread changes shared state and wants to notify one thread waiting on the same monitor that it may be able to proceed.

For example, in a producer-consumer scenario, a consumer can wait while a shared queue is empty. A producer can add an item and call `Monitor.Pulse` to notify a waiting consumer.

`Wait` and `Pulse` must be called while holding the relevant monitor lock. The waiting thread should check its condition in a loop after waking up.

## Q5 — Write the same critical section twice: once with `lock`, once with `Monitor.Enter` / `Monitor.Exit`.

Both examples protect the same shared counter and ensure that only one thread at a time can increment it.

**Using `lock`:**

```csharp
private readonly object _gate = new();
private int _counter;

public void Increment()
{
    lock (_gate)
    {
        _counter++;
    }
}
```

**Using `Monitor.Enter` and `Monitor.Exit`:**

```csharp
using System.Threading;

private readonly object _gate = new();
private int _counter;

public void Increment()
{
    bool lockTaken = false;

    try
    {
        Monitor.Enter(_gate, ref lockTaken);
        _counter++;
    }
    finally
    {
        if (lockTaken)
        {
            Monitor.Exit(_gate);
        }
    }
}
```

The second example uses the `Monitor.Enter` overload with `ref bool` to track whether the lock was acquired. The `finally` block releases it only if acquisition succeeded.

For this simple critical section, `lock` is generally easier to read and less error-prone. Direct `Monitor` calls are useful when more advanced synchronization behavior is required.

## References

1. Microsoft Learn — Monitor Class: https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor
2. Microsoft Learn — The `lock` statement: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock
