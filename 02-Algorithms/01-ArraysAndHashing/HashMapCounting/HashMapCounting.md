# HashMapCounting

**Node:** 01 · Arrays & Hashing · **Needs:** array, hash table

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run HashMapCounting.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> Never skip this. The optimisation is meaningless without the thing it beats, and
> "what's the naive approach?" is how most interviews open.

**Approach:**

**Time:** O(?)  **Space:** O(?)

**Measured:** n = 1,000 → **499,500** steps

**What work is repeated:**

> This line is the hinge. Almost every optimisation is caching a repeat, sorting to
> expose structure, or moving a pointer instead of restarting a scan.

## 3. The idea

> Two or three sentences, no code. If you cannot say it without code, go back.

## 4. Visual trace

> Draw one small input all the way through. This is the section you will actually
> remember in three months. Use `[4, 7, 2, 7, 9]` — the same input the code traces.

```
input:  [ ... ]

step 1  ...
step 2  ...

result: [ ... ]
```

## 5. Why it is faster

> Name the *specific* work the brute force did that this avoids. Not "it's smarter" —
> exactly which comparisons or scans disappear, and why they are safe to skip.

## 6. Complexity

**Time** — Best: O(?) · Average: O(?) · Worst: O(?)

**Why?**

> Derive it. Note that best and worst differ here for a reason worth naming.

**Space** — O(?)

**Why?**

**Which case am I quoting?** *worst* / *average* / *amortised* are three different
claims and this is exactly where follow-up questions land.

> Careful: the two implementations do **not** carry the same case label. Say why.

## 7. Side by side

Worst case for both — every value distinct, so neither can exit early.

| n | brute force | this algorithm | ratio |
| --- | --- | --- | --- |
| 100 | 4,950 | 100 | 50× |
| 1,000 | 499,500 | 1,000 | 500× |
| 10,000 | 49,995,000 | 10,000 | 5,000× |
| 100,000 | 4,999,950,000 | 100,000 | 50,000× |

`dotnet run HashMapCounting.cs -- compare`

> The ratio column is n/2. Write one line on why that means the brute force never
> catches up — and one line on why the step counts still flatter the hash set.

## 8. Implementation

- [x] Written from scratch, no library calls → `HashMapCounting.cs`
- [ ] Written again from memory a week later

## 9. Unit tests

`dotnet run HashMapCounting.cs -- test` — **12/12 passing**

- [x] Empty input
- [x] Single element
- [x] Two elements — both with and without a duplicate
- [x] Already in the answer state
- [x] Reverse / worst case
- [x] Duplicates — all identical
- [x] Negative numbers, zero, `int.MinValue`
- [x] First-not-smallest — `[9,3,9,3]` returns 9

## 10. Benchmark

| n | brute steps | brute ms | hash steps | hash ms |
| --- | --- | --- | --- | --- |
| 100 | 4,950 | 0.02 | 100 | 0.00 |
| 1,000 | 499,500 | 1.11 | 1,000 | 0.01 |
| 10,000 | 49,995,000 | 119.46 | 10,000 | 0.20 |
| 50,000 | 1,249,975,000 | 2,919.66 | 50,000 | 0.55 |

`dotnet run HashMapCounting.cs -- bench`

> Steps are deterministic — reason with those. Milliseconds are noisy — they make it real.
> At n = 50,000 the gap is **2,919 ms vs 0.55 ms**. Write down what that is as a ratio.

## 11. Where is it used?

> Real systems, .NET BCL, and which problems it unlocks.

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- Why is the hash version **O(1) average** per lookup and not O(1) worst — and what
  input would make it O(n)?
- `HashSet<T>.Add` returns a `bool`. Why does using that return value instead of
  calling `Contains` then `Add` matter?
- When would you **not** use this — i.e. what does the O(n) extra memory cost you, and
  what structure would you reach for instead?
