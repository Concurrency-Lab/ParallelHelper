# General Questions

* Include patterns related to async/await?
* Reactive patterns? Although, requires more practical experience.

# Bugs

## Data races by pattern

Similar to *Avoid accessing shared memory after asynchrounous operations*.

```cs
var current = 10;
Task.Run(() => current = 20);
Console.WriteLine(current);
```

> Note: the same could be that the main thread writes the object and the task reads it.

## Deadlock (MonitorDeadlockAnalyzer)

Class based detection of deadlocks:

```cs
class A {
    private readonly object syncObject = new object();

    public void DoWork(A other) {
        lock(syncObject) {
            lock(other.syncObject) {
                // ...
            }
        }
    }
}
```


## Non atomic accesses to volatile fields

Multiple accesses to a `volatile` field where at least one is writing.
A common error is falsely believing that unary operations like `++` are atomic.

```cs
private volatile int count;
// ...
if(count < 0) {
    count = 0;
}
```

> There are cases where fields are marked `volatile` and updated with non-atomic operations. However, the read and write combinations are guarded by synchronization primitives like monitor locks.

## Non atomic access to collections

A concurrent collection is queried for a state (e.g. contains) and certain actions are executed on it depending on the outcome.

```cs
private readonly ConcurrentDictionary<string, object> cache = /* ... */;
// ...
object entry;
if(!cache.ContainsKey(key)) {
    entry = Compute(key);
    cache[key] = entry;
} else {
    entry = cache[key];
}
return entry;
```

## Single monitor pulse for multiple semantic wait conditions

Multiple semantic wait conditions are present in the code but only a single monitor signal is sent. Since the receiving thread cannot be specified, the signal may be sent to the wrong thread.

```cs
// Variant 1: multiple waits with different conditions
// The value of max is the same for all threads.
while(count >= max) {
    Monitor.Wait(syncObject);
}
// ...
while(count <= 0) {
    Monitor.Wait(syncObject);
}
//...
count--;
Monitor.Pulse(syncObject);
```

```cs
// Variant 2: single wait but with different semantics
// The value of desired is differs between the threads.
while(count < desired) {
    Monitor.Wait(syncObject);
}
//...
count++;
Monitor.Pulse(syncObject);
```

When in doubt, always prefer `Monitor.PulseAll`.

## Acquiring a write-lock inside a read-lock

Only upgradeable read-locks may switch to a write-lock.

## Write-only synchronized collection (UnsynchronizedCollectionAccessAnalyzer)

The basic ollections are not thread-safe. Only synchronizing the write-accesses to a collection but not the pottentially concurrently running read-accesses leads to unexpected behavior.

# Smells

## Asynchrounous operations inside LINQ expressions (TBD)

The use of asynchronous operations, e.g. through tasks or the async keyword, indicates design issues.

```cs
// Variant 1
var computations = entries.Select(e => Task.Run(() => Compute(e))).ToArray();
```

```cs
// Variant 2 (TBD)
var computations = entries.Select(e => async () => await ComputeAsync(e))).ToArray();
```

> TBD: Maybe too generic.
> TBD: Same direction as side-effects

## Starting threads inside a constructor (ThreadStartInsideConstructorAnalyzer)

Starting new threads or tasks inside a constructor usually unexpected. Prefer the use of a factory method to clearly state the intent.

## Side-effects inside PLINQ expressions (PlinqSideEffectAnalyzer)

Side-effects inside a conventional LINQ expression are generally bad-practice. Inside a PLINQ expression it burdens the risk of unexpected behavior due to data races or even race conditions.

## Side-effects inside Parallel.For (ParallelForSideEffectAnalyzer)

See: Side-effects inside PLINQ expressions.

## Synchronization inside PLINQ expressions

The need for synchronization, e.g. with a monitor, inside a PLINQ expression depicts a design error.

## Synchronization inside Parallel.For

See *Synchronization inside PLINQ expressions*.

## Monitor wait inside tasks

Tasks should always run-to-completion, thus the use of `Monitor.Wait` inside them is discouraged.

## Strings as synchronization objects (MonitorDiscouragedTypeSyncObjectAnalyzer)

The use of strings as synchronization objects is hightly discouraged. The monitor's behavior will depend on the source of the string. On the one hand, the same string literals will synchronize against each other due to the string pooling. On the other hand, equal non-literals (e.g. through IO) will not synchronize.

## Boxed types as synchronization objects (MonitorDiscouragedTypeSyncObjectAnalyzer)

See *Strings as synchronization objects*.

## Change of the synchronization object inside a monitor lock (MonitorSyncObjectChangeAnalyzer)

Changing the synchronization object inside a monitor lock is generally bad practice.

```cs
lock(syncObject) {
    // ...
    syncObject = new object();
}
```

> TBD: This appears to be some common pattern. Maybe people believe that the synchronization occurs around the variable rather than the value?

## CompareExchange not enclosed by a loop

Since `Interlocked.CompareExchange` is an optimistic operation, it has to be expected to fail. Therefore, it is usually required to enclose the operation inside a loop and retry the exchange upon failure.

## Async methods with the return type void

Async methods that have the return type void are fire-and-forget methods. It is usally bad practice to not return a `Task` object, except for event handlers.

## Task waits inside tasks => nope

One task waits for the completion of another using a blocking operation like `Task.Wait()`. This violates the run-to-completion rule and blocks the worker-thread unnecessarily.

```cs
var other = Task.Run(() => /* ... */);
Task.Run(() => {
    // ...
    var computed = other.Result;
    // ...
});
```

## Task waits inside asynchronous methods

Waiting for the completion of a task inside an `async` method can lead to infinitely blocking the current thread, depending on the execution model. Besides this undesired behavior, it denotes an incorrect use of the `async` pattern.

```cs
public async Task DoComputationAsync() {
    // ...
    DoComputationA().Wait();
    // ...
    await DoComputationB();
}
```

## Busy loop to acquire a lock

A semaphore (or similar) is being acquired repeatedly inside a loop.

```cs
while(retrying) {
    if(!semaphore.Wait(1)) {
        continue;
    }
    // ...
}
```

> TBD: Semaphore waits with timeouts inside loops.

## Writing to shared memory inside a read lock

Acquiring locks more granularly may lead to cases where shared memory is written while only a read lock is held.

## Accessing multiple volatile fields without further synchronization

The use of multiple `volatile` fields often hints some mutual dependency, therefore a race-condition is likely to be present.

## Monitor wait inside task

The use of `Monitor.Wait` inside tasks violates the run-to-completion principle. Moreover, it is quite likely that another task is responsible for the signal. Therefore, it is required that the thread pool offers at least two threads (although this is less problematic due to throughput based scaling).

> TBD: Inside `async` method

## Usage of blocking collections inside tasks (TBD)

The use of blocking collection (their blocking methods to be more precise) inside tasks may violate the run-to-completion principle.

## Controlling the level of parallelism with a semaphore

```cs
var semaphore = new SemaphoreSlim(10);

foreach(var entry in entries) {
    semaphore.Wait();

    Task.Run(() => {
        // do someting with 'entry'
        semaphore.Release();
    });
}
```

> This pattern appears to be a quite common.

> TBD: Most-likely always better to use PLINQ, `Parallel.For`, or `Parallel.ForEach` with the `WithDegreeOfParallelism` or `MaxDegreeOfParallelism` configured.
> Alternatively, control the parallelism by only creating a limited number of tasks.


## Invalid use of a non-blocking collection (InvalidUseOfNonBlockingCollectionAnalyzer)

A non-blocking collection is used where a blocking collection would be appropriate.

```cs
class Sample {
  private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
  public void DoWork() {
    while(true) {
      if(!_queue.TryDequeue(out var item)) {
        // A Thread.Sleep(...) is often used to reduce the CPU pressure.
        // This allows to identify these cases with a high certainty.
        Thread.Sleep(100);
        continue;
      }
      // do work...
    }
  }
}
```

In this sample, the user waits with a spinning loop for an item to appear in the queue. Often, a thread sleep or other thread delaying mechanisms are used to reduce the CPU load when the queue is empty. However, a blocking collection would be appropriate here so the thread can be put into the wait state.

## Thread sleep in an asynchronous method (ThreadSleepInAsynchronousMethodAnalyzer)

The use of `Thread.Sleep(...)` inside asynchronous methods is discouraged. One should always use `Task.Delay(...)` instead.

## Task only returning value (TaskOnlyReturningValueAnalyzer)

Do not use `Task.Run(() => ...)` to return already computed values. Use `Task.FromResult` instead.

## Task.Factory.StartNew with an async delegate (TaskFactoryStartNewWithAsyncDelegateAnalyzer)

The method `Task.Factory.StartNew` is not "compatible" to `async` delegates and requires manual unwrapping of the inner tasks. Therefore, the use of `Task.Run` is perferable. The use of `Task.Run` should be preferred in general.

# Best Practice

## Monitor wait not inside a conditional loop (MonitorWaitWithoutConditionalLoopAnalyzer)

It is rarely the case that a condition does not have to be rechecked when waiting for a monitor signal, especially when multiple threads will be signaled as one thread may invalidate the condition again.

```cs
if(count < desired) {
    Monitor.Wait(syncObject);
}
//...
count++;
Monitor.PulseAll(syncObject);
```

> TBD: Additional pattern for the complete absence of a wait condition?

## Prefer the slim synchronization primitives

Many synchronization primitives have slim counterpart which should be preferred when no OS level synchronization is necessary.

| Original         | Slim Counterpart     |
| ---------------- | -------------------- |
| Semaphore        | SemaphoreSlim        |
| Mutex            | SemaphoreSlim        |
| ReaderWriterLock | ReaderWriterLockSlim |
| ManualResetEvent | ManualResetEventSlim |

> TBD: System.Timer.Timer => System.Threading.Timer

## Prefer the lock keyword for monitors

Explicitely using the Monitor using `Monitor.Enter` and `Monitor.Exit` is cumbersome to implement right. Therefore, prefer the use of `lock`.

## Do not declare methods async unnecessarily (UnnecessarilyAsyncMethodAnalyzer)

Methods should not be declared `async` unnecessarily. If the only awaited task is the last statement of the method's body, one may return the corresponding task object directly.

```cs
public async Task<int> DoComputationAsync() {
    // ...
    return await DoActualComputationAsync();
}
```

> TBD: This could also involve tasks that are inside branches. Although, it hinders beverity:

> ```cs
> public Task DoCompututationAsync() {
>     // ...
>     if(condition) {
>         return DoActualComputationAsync();
>     }
>     return Task.CompletedTask;
> }
> ```

> Important: `await` is necessary when returning the result of an asynchronous operation of a resource within a `using` statement. The same goes for `try-catch` clauses.

## Avoid fire-and-forget threads (FireAndForgetThreadAnalyzer)

Fire-and-forget threads (or tasks) may result in unobserved errors and are usually design issues.

```cs
Task.Run(() => /* ... */);
```

```cs
new Thread(() => /* ... */).Start();
```

> TBD: This may also include also methods that return tasks.

## Avoid starting timers upon instantiation (TBD) (TimerScheduledUponInstantiationAnalyzer)

Creating a `Timer` instance that automatically starts may lead to data races and / or race conditions if the timer object is used within the `TimerCallback`, e.g. to dispose it.

```cs
private Timer timer;
// ...
public void DoDelayed(int timeout) {
    timer = new Timer(Completed, null, timeout);
}

private void Completed(object state) {
    // ...
    timer.Dispose();
}
```

## Async task implementation (AsyncMethodWithTaskImplementationAnalyzer)

Do not offload CPU bound methods bodies (or parts of) to tasks to make a method *asynchronous*. Let the user decide where to use the thread pool.

```cs
public Task<int> ComputeAsync() {
    return Task.Run(() => /* ... */);
}
```

> In general: Use `Task.Run` for the invocation, not the implementation. I.e., do not create *fake async* methods.
> https://channel9.msdn.com/Series/Three-Essential-Tips-for-Async/Async-Library-Methods-Shouldn-t-Lie

> Note: Creating tasks is contraproductive in ASP.NET environments.

## Combination of asynchronous operations with CPU bound tasks

A method that uses asynchronous APIs (i.e. for IO) and does CPU intensive work. The CPU intensive work should not be offloaded into a task (see *Async task implementation*) at implementation side.

```cs
public async Task DoWorkAsync() {
    var content = await ReadAsync();
    // ...
    var result = await Task.Run(() => /* ... */);
    // ...
}
```

> TBD: How to differentiate between *good* (consumer side) and *bad* (implementation side) cases.

## Do not create asynchronous wrappers for synchronous methods

A method that only has a synchronous implementation (because it is solely CPU bound) should not get an asynchronous wrapper whose only purpose is to make the method awaitable.

```cs
public int Compute() {
    var result = /* ... */;
    return result;
}

public Task<int> ComputeAsync() {
    return Task.Run(Compute);
}
```

> Note: The *Async* suffix implies that the method is making use of non-blocking operations (e.g. non-blocking IO). In this pattern, this is not the case since the CPU bound work is only offloaded to a task.
> Aka async-over-sync
> See: https://blogs.msdn.microsoft.com/pfxteam/2012/03/24/should-i-expose-asynchronous-wrappers-for-synchronous-methods/
> https://channel9.msdn.com/Series/Three-Essential-Tips-for-Async/Async-Library-Methods-Shouldn-t-Lie
> Maybe create the same for synchrounous wrappers for asynchronous methods: sync-over-async
> See: https://blogs.msdn.microsoft.com/pfxteam/2012/04/13/should-i-expose-synchronous-wrappers-for-asynchronous-methods/

## Avoid accessing shared memory after asynchrounous operations

Accessing shared memory (e.g. through a member field) after awaiting an asynchronous operation may lead to a race condition.

```cs
public int Value { get; set; }

public async Task DoActionAsync() {
    await DoAsync();
    // ...
    var state = Value;
}
```

> TBD: A general pattern is for example showing the user that some work is being done (e.g. through disabling the button). So this pattern probably needs to be stricter. Maybe only consider cases where the shared memory is read after the asynchronous operation?

## Pattern for WithCancellation

`WithCancellation` allows passing a cancellation token to an asynchronous operation that is being awaited. But the token will not cancel the asynchronous operation but the `await`. This might be unexpected behavior.

> The analysis may check if the async operation accepts a cancellation token.

## ManualResetEvent and AutoResetEvent error pattern?

## Make monitor synchronization objects readonly (MonitorReadonlySyncObjectAnalyzer)

Since synchronization objects should not change through the object's lifetime, it is recommended to mark them as readonly.
