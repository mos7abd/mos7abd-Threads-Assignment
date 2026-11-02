LinkedIn Profile:
https://www.linkedin.com/in/mo-abdella

Note:
The required LinkedIn technical posts for this assignment have been completed and included below as drafts.

As mentioned in the previous assignment, I prefer to publish LinkedIn posts when they provide meaningful value to my professional development. Therefore, these posts have not been published on LinkedIn. However, the required technical content, explanations, C# examples, and practical applications have been prepared for all the assigned topics.


Post 1 — 
# Topic 1: Task.WhenAll vs Task.WhenAny vs Task.WhenEach
While learning asynchronous programming in C#, I started to understand that `Task.WhenAll`, `Task.WhenAny`, and `Task.WhenEach` are not alternatives that do exactly the same thing. Each one is useful in a different situation.

Imagine I am building an application that needs employee information, GIS data, and user permissions.

I can use `Task.WhenAll` when I need all these operations to finish before continuing. Instead of waiting for each operation before starting the next one, I can start them first and then await all of them.

```csharp
var employeeTask = GetEmployeeAsync();
var gisTask = GetGisDataAsync();


await Task.WhenAll(employeeTask, gisTask);
```
`Task.WhenAny` is useful when I want to know which task completes first. For example, I might be waiting for the fastest response from multiple data sources. The other tasks do not automatically stop when one task completes.

`Task.WhenEach` is useful when I want to process tasks as they complete, without waiting for the slowest task before handling the results.

My main takeaway is that choosing the right method depends on the requirement. Sometimes I need every result, sometimes the first result, and sometimes I want to handle results as they become available.

-------------------------------------------------------------------------------
Post 2 — 
# Topic 5: Interlocked vs lock
One thing I learned while working with multithreading in C# is that sharing a variable between threads can cause problems even when the code looks simple.

For example, imagine multiple threads updating a product view counter. Using `count++` may lose updates because the operation involves reading, incrementing, and writing the value. Two threads can read the same value and overwrite each other's results.

For a simple counter, `Interlocked` is a good option:

```csharp
Interlocked.Increment(ref count);
```

This performs the increment atomically, so concurrent increments are not lost.

But what if I need to update several related values together? This is where `lock` can be more suitable.

```csharp
lock (_sync)
{
    balance -= amount;
    transactionCount++;
}
```csharp

The lock ensures that only one thread at a time can execute code protected by the same lock. This helps preserve consistency when multiple operations must happen together.

The important lesson is that I should choose the synchronization tool based on the problem. For a simple atomic operation, `Interlocked` is often enough. When a group of operations must be protected as one critical section, `lock` is usually more appropriate.

----------------------------------------------------------------------------------------------------------------------
Post 3 —
# Topic 4: CancellationToken
While learning asynchronous programming in .NET, I found that cancellation is not the same as forcibly stopping a thread. Instead, `CancellationToken` provides a way to communicate that an operation should stop when it is safe to do so.

Imagine an API that retrieves employee information, loads GIS data, and then queries another service. If the user cancels the request, continuing unnecessary work wastes resources.

A cancellation token can be passed through the call chain:

```csharp
public async Task<string> GetDataAsync(
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    return await httpClient.GetStringAsync(
        url,
        cancellationToken);
}
```

Here, the method checks whether cancellation has been requested and passes the token to the HTTP operation, which supports cancellation.

One important point is that passing a token does not automatically cancel every operation. Each method must pass it to supported operations or check it during its own work.

Cancellation is cooperative. The code and the operations it calls must cooperate with the cancellation request. Depending on the operation, cancellation may result in an `OperationCanceledException` or a task with the `Canceled` status.

My takeaway is that good cancellation handling makes applications more responsive and avoids unnecessary work, especially when several asynchronous methods depend on each other.

-----------------------------------------------------------------------------------------------

Post 4 —
# Topic 2: async/await is a state machine

When I first started learning `async` and `await` in C#, I thought that using `async` meant the method would always run on a new thread. I learned that this is not how asynchronous programming works.

The C# compiler transforms an asynchronous method into a state machine that keeps track of where execution should continue after an awaited operation completes.

For example:

```csharp
public async Task<string> GetEmployeeNameAsync()
{
    var employee = await GetEmployeeAsync();
    var name = await GetNameAsync(employee.Id);

    return name;
}
```

When the method reaches the first `await`, if the task is not complete, the method can suspend its continuation and return control to its caller. When the awaited task completes, execution resumes from the appropriate point.

The second call happens after the first one finishes because the employee ID is needed to get the name.

This is especially useful for I/O operations, such as HTTP requests or asynchronous database calls. While waiting for I/O, a thread does not have to remain blocked for the entire waiting period.

Another important point is that `await` does not automatically make operations run in parallel. If operations are independent, I may start their tasks first and then use `Task.WhenAll`.

Understanding the state machine helped me see that `async/await` makes asynchronous code easier to write and read, while the compiler handles the continuation details.

------------------------------------------------------------------------------

Post 5 —
# Topic 7: Task lifecycle

While learning about `Task` in C#, I realized that a task has a lifecycle, and understanding its status helps me understand what is happening during asynchronous execution.

A task can have different statuses depending on how it was created and how it executes. For example, `Created` means a task has been created but has not started. `WaitingToRun` means it is waiting for execution, and `Running` means its delegate is executing.

A task can finish successfully with `RanToCompletion`, finish because of cancellation with `Canceled`, or fail because of an unhandled exception with `Faulted`.

Here is a simple example:

```csharp
Task task = Task.Run(() =>
{
    Console.WriteLine("Processing...");
});

await task;

Console.WriteLine(task.Status);
```

`Task.Run` schedules the work for execution. The `await` waits for the task to complete without synchronously blocking the current thread while it waits.

After successful completion, the task's status is `RanToCompletion`.

Not every task passes through every status. Some tasks finish so quickly that I may never observe them in the `Running` state, and tasks created in different ways can start in different states.

Understanding the task lifecycle helps me reason about completion, cancellation, and exceptions instead of assuming that starting a task means the work has already finished.


