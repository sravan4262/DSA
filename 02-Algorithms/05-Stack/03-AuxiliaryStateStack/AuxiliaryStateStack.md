# AuxiliaryStateStack

**Node:** 05 · Stack · **Needs:** array, stack

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run AuxiliaryStateStack.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. Say it as a guarantee about operations, with the case label attached.

## 2. Brute force first

**Approach:** keep one stack and scan it on every `Min()`.

**Time:** Push O(?) · Pop O(?) · **Min O(?)**  **Space:** O(?) extra

**Measured:** n = 10,000 pushes then 10,000 `Min()` calls → **100,010,000** element reads.
The O(1) version does **20,000**.

**Why you cannot just keep a single `_min` field:**

> This is the important box on the page. Push works. Say exactly what Pop cannot do, and
> then finish this sentence: *"a running SUM needs no auxiliary stack because ______"*.

## 3. The idea

> Two or three sentences, no code. The sentence that makes the code obvious is about
> *lifetimes*: the minimum recorded when element `i` arrived becomes the answer again
> precisely when ______.

## 4. Visual trace

> Use `push 5, push 3, push 7, push 3`, then pop all the way down asking `Min()` each time.
> Show both stacks. Mark the row where the answer has to go back up to 5.

```
 op         values         mins           Min()
 push 5     [5]            [5]
 push 3     [5,3]          [5,3]
 push 7     [5,3,7]        [5,3,3]
...
```

## 5. Why it works, and why it would not for a queue

> Three separate things:
>
> **(a)** Both stacks are always the same height. Why does that matter — what would break
> if `Pop` only sometimes popped the aux stack? (It is exactly the lazy version's bug.)
>
> **(b)** Nothing is ever recomputed. Say why not, in terms of what was never thrown away.
>
> **(c)** Try the same trick on a **queue**: record the minimum at arrival, dequeue from the
> far end. What goes wrong? Name the structure in node 07 that fixes it.

## 6. Complexity

**Time** — Push O(?) · Pop O(?) · Peek O(?) · Min O(?)

**Space** — O(?) extra for the parallel version.

**Which case am I quoting?** `Min()` is **O(1) worst**. But `Push` on the same object is
**O(1) amortised** — and the reason has nothing to do with this algorithm.

> Write out why those two operations get different labels. Then: the lazy variant is O(n)
> worst and O(log n) *expected*. What does "expected" claim, and what is it a claim
> **about**?

## 7. Side by side

**Time** — n pushes, then n `Min()` queries. `dotnet run AuxiliaryStateStack.cs -- compare`

| n | scan steps | parallel steps | ratio |
| --- | --- | --- | --- |
| 100 | 10,100 | 200 | 50.5× |
| 1,000 | 1,001,000 | 2,000 | 500.5× |
| 10,000 | 100,010,000 | 20,000 | 5,000.5× |
| 50,000 | 2,500,050,000 | 100,000 | **25,000.5×** |

**Space** — aux entries kept for n = 10,000, by input shape.

| input shape | parallel | lazy | encoded |
| --- | --- | --- | --- |
| increasing 1..n | 10,000 | **1** | 0 |
| decreasing n..1 | 10,000 | 10,000 | 0 |
| **all identical** | 10,000 | **10,000** | 0 |
| random, seed 1 | 10,000 | **10** | 0 |

> Three things to write about the space table:
>
> - `all identical` is also a worst case for the lazy version. Say why, and connect it to
>   the `<=` in `Push` — the fix for the correctness bug *is* the cause of this.
> - random input gives **10** at n = 10,000. Where does 10 come from? (ln 10,000 = 9.2.
>   What is the chance element `i` is smaller than all `i−1` before it?)
> - the encoded column is 0 on every row. `-- trace` shows what believing that costs.
>   Write the four-operation script that breaks it.

## 8. Implementation

- [x] Written from scratch, no library calls → `AuxiliaryStateStack.cs`
- [ ] Written again from memory a week later

> Six classes in the file, two of them wrong on purpose. Fill this in:
>
> | Class | extra space | correct? | if not, the input that breaks it |
> | --- | --- | --- | --- |
> | `MinStackBrute` | | | |
> | `MinStackOneVariable` | | | |
> | `MinStackParallel` | | | |
> | `MinStackLazy` | | | |
> | `MinStackLazyStrict` | | | |
> | `MinStackEncoded` | | | |
>
> Then: `SumStack` is the same shape as `MinStackOneVariable` and is **correct**. One line
> in `Pop` is the whole difference. Write that line and say what property it relies on.

## 9. Unit tests

`dotnet run AuxiliaryStateStack.cs -- test` — **16/16 passing**

- [x] One push one min · push pop push min · min never changes · min changes often
- [x] Pop the minimum · **duplicate minima** · **triple duplicates** · **dup min, dup above**
- [x] **All identical** · strictly decreasing · strictly increasing
- [x] Negatives · mixed signs · drain to empty · **zero**
- [x] Random 2,000-operation script, seed 20261005

**The second table is the one that matters:** the same 16 scripts run against the broken
`<` version, and only **6 of 16** can tell it apart from the correct one.

> Three to write about:
>
> - What do all 6 catching scripts have in common? One feature, stated in four words.
> - `strictly decreasing` pushes a new minimum on every single push and still **passes** a
>   broken implementation. Why? (The answer is about where `<` and `<=` differ.)
> - 5 of the 6 catch it by **throwing** and 1 by returning a wrong number. Which is the
>   dangerous outcome, and why?

## 10. Benchmark

n pushes then n `Min()` queries, decreasing input.

| n | scan steps | scan ms | parallel ms | lazy ms | encoded ms |
| --- | --- | --- | --- | --- | --- |
| 1,000 | 1,001,000 | 4.04 | 0.02 | 0.03 | 0.02 |
| 10,000 | 100,010,000 | 381.41 | 0.29 | 0.21 | 0.12 |
| 20,000 | 400,020,000 | **1,499.37** | 0.46 | 0.42 | 0.27 |

`dotnet run AuxiliaryStateStack.cs -- bench`

> The scan's ms column quadruples each time n doubles — write down why that is the
> signature of O(n²) and not O(n log n).
>
> The three O(1) columns are within noise of each other, and on some runs one of them is
> *faster at the larger n*. So: on what grounds do you actually choose between them? Give
> the answer as an ordering, best first, and the reason for each position.

## 11. Where is it used?

> `MinStack` (the problem is literally this), `MaxStack`, `StockSpanner`,
> `BasicCalculator` (what is the auxiliary state there — it is not a minimum),
> `SlidingWindowMaximum` (why does this one need a **deque** instead?). Worth naming: an
> undo stack in an editor — what is the "auxiliary state" and does it have an inverse?

## 12. Recall check

- The one-sentence rule that decides whether you need a second stack at all. State it as a
  question about the aggregate, then classify sum, count, min, max and gcd by it.
- `Push` is amortised O(1) and `Min` is worst-case O(1) on the *same object*. Explain both
  labels, and say which part of the object each one is about.
- The lazy version uses `<=`. Give the four-operation script that breaks `<`, and say which
  of the two stacks ends up out of step with the other.
