# MonotonicStack

**Node:** 05 · Stack · **Needs:** array, stack

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run MonotonicStack.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence.

## 2. Brute force first

**Approach:** scan right from every index.

**Time:** Best O(?) · Worst O(?)  **Space:** O(?)

**Measured:** n = 1,000 decreasing → **499,500** comparisons. n = 1,000 **increasing** → **999**.

**What work is repeated:**

> Be specific about *which* stretch gets re-read and under what input.

## 3. The idea

> Two or three sentences, no code. The sentence that makes the code obvious starts
> *"the stack holds the indices of elements that are still ______"*.

## 4. Visual trace

> Use `[2, 1, 2, 4, 3]`. Show the stack contents after every step.

```
 i   a[i]   pops (answered by a[i])   stack after
 0      2   -                         [2]
 1      1   -                         [2,1]
...
```

## 5. Why it is faster

> Two separate things:
>
> **(a)** Why is the stack always decreasing bottom→top, when nothing in the code sorts
> or checks it? (Argue from what *could not* have happened.)
>
> **(b)** Why is a `for` loop wrapping a `while` loop still O(n)? Count the right thing.

## 6. Complexity

**Time** — O(?)  **Space** — O(?), and worst case the stack holds how many elements?

**Which case am I quoting?** The stack version's bound is **amortised**. State the
argument in the form *"every index is ______ exactly once and ______ at most once"*.

> Note `-- compare` prints a **`2n?`** column that checks this on every input rather than
> asserting it. Say why that column can never read "NO".

## 7. Side by side

**Brute force's worst case** — strictly decreasing input.

| n | brute force | stack ops (push+pop) | ≤ 2n? | ratio |
| --- | --- | --- | --- | --- |
| 100 | 4,950 | 199 | yes | 25× |
| 1,000 | 499,500 | 1,999 | yes | 250× |
| 10,000 | 49,995,000 | 19,999 | yes | 2,500× |
| 50,000 | 1,249,975,000 | 99,999 | yes | **12,500×** |

**Brute force's best case** — strictly increasing input. Paste this table from
`-- compare`; the brute force wins it.

| n | brute force | stack ops | ratio |
| --- | --- | --- | --- |
| 1,000 | 999 | 1,999 | |

`dotnet run MonotonicStack.cs -- compare`

> Same lesson as insertion sort vs mergesort in node 02. Write it out: what does the
> O(n²) method have that the O(n) method does not, and why is that not a reason to
> prefer it?

## 8. Implementation

- [x] Written from scratch, no library calls → `MonotonicStack.cs`
- [ ] Written again from memory a week later

> The file also carries the other three variants. Fill this in from memory:
>
> | Question | walk direction | pop while |
> | --- | --- | --- |
> | next greater to the right | | |
> | next smaller to the right | | |
> | prev greater to the left | | |
> | prev smaller to the left | | |

## 9. Unit tests

`dotnet run MonotonicStack.cs -- test` — **16/16 passing**

- [x] Empty · single · two increasing · two decreasing
- [x] Strictly increasing · strictly decreasing
- [x] The classic `[2,1,2,4,3]` → `[4,2,4,-1,-1]`
- [x] **All identical** · **ties then bigger** · one peak · one valley
- [x] Max at front · max at back · negatives · mixed signs · **long pop run**

> Two to write about. This file uses **strict `>`**, so `[3,3,3]` is all `-1`. What
> changes if you use `>=`, and why is that a question for the problem statement rather
> than a bug? And what does **long pop run** demonstrate in one step?

## 10. Benchmark

Strictly decreasing input — the brute force's worst case.

| n | brute steps | brute ms | stack steps | stack ms |
| --- | --- | --- | --- | --- |
| 1,000 | 499,500 | | 1,999 | |
| 10,000 | 49,995,000 | | 19,999 | |
| 50,000 | 1,249,975,000 | | 99,999 | |
| 100,000 | 4,999,950,000 | | 199,999 | |

`dotnet run MonotonicStack.cs -- bench` and paste the ms columns.

## 11. Where is it used?

> `NextGreaterElement`, `DailyTemperatures`, `LargestRectangleInHistogram`,
> `TrappingRainWater`, `RemoveKDigits`, `StockSpanner`. Worth naming: in
> `LargestRectangleInHistogram`, what are the "next smaller on each side" values *for*?

## 12. Recall check

- Why is the stack monotonic when nothing maintains it? Argue from what cannot have
  happened.
- A `for` around a `while` that is O(n), not O(n²). State the counting argument, and name
  the other structure in this repo whose cost is bounded the same way.
- You need *previous smaller* instead of *next greater*. What two things change?
