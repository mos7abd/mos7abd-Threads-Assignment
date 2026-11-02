# Assignment 04 — Answers

Student name: Mostafa Abdellah  
Group / ID:4

## Why Expected and Actual disagree

- The original implementation used `Views++`, which is not an atomic operation. It consists of reading the current value, incrementing it, and writing the result back.
- When multiple threads execute this operation concurrently, some updates can be lost because threads may read the same value and overwrite each other's changes. This is a race condition known as a lost update.
- As a result, the actual count could be lower than the expected count of 400,000.

## What you changed

- Replaced `Views++` with `Interlocked.Increment(ref Views)` in `RegisterView`.
- `Interlocked.Increment` performs the increment atomically, preventing concurrent updates from being lost.
- After the change, the actual count matched the expected count of 400,000 in the runs tested.

## When would you protect the update differently

- I would use `lock` if the update involved multiple related operations that must execute together, such as checking a condition and updating several shared fields consistently.
- For a single integer increment, `Interlocked.Increment` is simpler and more appropriate. For more complex shared-state operations or thread coordination, I would choose a suitable synchronization mechanism based on the requirements.