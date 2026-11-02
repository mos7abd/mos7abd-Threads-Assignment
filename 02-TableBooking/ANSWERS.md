# Assignment 02 — Answers

Student name: Mostafa Abdellah  
Group / ID: 4

## What is wrong

The `Book()` method checks `TablesLeft`, waits for 20 milliseconds, and then updates `TablesLeft` and `Booked` without synchronization.

Because 40 customer threads call this method concurrently, multiple threads can pass the availability check before any of them updates the shared values. This causes a race condition, allowing more bookings than the available tables and making `TablesLeft` negative.

Original results from three runs:

| Run | Booked | TablesLeft |
|---  |   --- :|   ---:     |
| 1   |   36   |   -31      |
| 2   |   30   |   -34      |
| 3   |   31   |   -24      |

## How you fixed it

I added a private lock object and wrapped the complete booking operation in a `lock` statement. The availability check, table count update, booking count update, and success message now execute under the same lock.

This prevents multiple threads from booking the same remaining capacity concurrently. When no tables are left, subsequent booking attempts return without changing the counters.

Results after the fix from three runs:

| Run | Booked | TablesLeft |
|---  |  ---:  |   ---:     |
| 1   | 5      |   0        |
| 2   |  5     |   0        |
| 3   |   5    |   0        |

All three runs produced the correct totals: exactly five bookings and zero tables remaining.
