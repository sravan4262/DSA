# PrefixSums

**Node:** 01 · Arrays & Hashing · **Needs:** array

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run PrefixSums.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> Never skip this. The optimisation is meaningless without the thing it beats, and
> "what's the naive approach?" is how most interviews open.

**Approach:**

**Time:** O(?) per query · O(?) for q queries  **Space:** O(?)

**Measured:** n = 1,000, 1,000 queries → **1,000,000** steps

**What work is repeated:**

> This line is the hinge. Be specific — it is not "it adds things up twice". Say exactly
> which cells get re-read and why.

## 3. The idea

> Two or three sentences, no code. If you cannot say it without code, go back.

## 4. Visual trace

> Draw one small input all the way through. Use `[3, 1, 4, 1, 5, 9]` — the same input the
> code traces. Draw **both** arrays, and show why `prefix` has one more slot than `a`.

```
a      = [ 3,  1,  4,  1,  5,  9]
prefix = [ ?, ...                ]

query a[3..5]  →  ...
```

## 5. Why it is faster

> Name the *specific* work the brute force did that this avoids. The one-line version is
> about **cancellation** — write out what cancels and what survives.

## 6. Complexity

**Build** — Time: O(?) · Space: O(?)

**Query** — Time: O(?)

**q queries over an array of n** — brute: O(?) · prefix: O(?)

**Why?**

**Which case am I quoting?** *worst* / *average* / *amortised* are three different
claims and this is exactly where follow-up questions land.

> This one is unusual — both numbers are **worst case** and neither is average or
> amortised. Say why. (Hint: what would have to be true for a case distinction to exist?)

**Where is the break-even?** With how many queries does the prefix version start to win,
and why is it not zero?

## 7. Side by side

n queries over an array of n, each spanning the whole array.

| n | brute force | prefix total | of which build | ratio |
| --- | --- | --- | --- | --- |
| 100 | 10,000 | 200 | 100 | 50× |
| 1,000 | 1,000,000 | 2,000 | 1,000 | 500× |
| 10,000 | 100,000,000 | 20,000 | 10,000 | 5,000× |
| 50,000 | 2,500,000,000 | 100,000 | 50,000 | 25,000× |

`dotnet run PrefixSums.cs -- compare`

> Read the "of which build" column: half the prefix cost is the build and the other half
> is n queries at 1 step each. Write one line on what that tells you about where the
> money goes.

## 8. Implementation

- [x] Written from scratch, no library calls → `PrefixSums.cs`
- [ ] Written again from memory a week later

> When you rewrite it, the thing to get right from memory is the **n+1 sizing and the
> leading zero**. If you reach for `if (l == 0)` you have rebuilt the buggy version.

## 9. Unit tests

`dotnet run PrefixSums.cs -- test` — **12/12 passing**

- [x] Single element
- [x] Whole array
- [x] `l == 0` and `r == last` — the two boundaries
- [x] `l == r` in the middle
- [x] Two elements
- [x] All negative · mixed signs · all zeros · sums to zero
- [x] Reverse sorted
- [x] **Overflow** — 1,000 × `int.MaxValue`, which is why `Build` returns `long[]`

> There is deliberately **no empty-array case**. Write one line on why an empty input is
> a caller error here rather than an edge case.

## 10. Benchmark

| n | brute steps | brute ms | prefix steps | prefix ms |
| --- | --- | --- | --- | --- |
| 100 | 10,000 | 0.03 | 200 | 0.01 |
| 1,000 | 1,000,000 | 2.18 | 2,000 | 0.02 |
| 10,000 | 100,000,000 | 191.00 | 20,000 | 0.13 |
| 50,000 | 2,500,000,000 | 4,757.59 | 100,000 | 0.47 |

`dotnet run PrefixSums.cs -- bench`

> The brute-force column is one of the clearest O(n²) curves in the repo — n doubles, the
> work quadruples. Check that against the ms column and note where it stops holding.

## 11. Where is it used?

> Real systems, .NET BCL, and which problems it unlocks. Worth naming: what does
> `DifferenceArray` do with this, and what does a Fenwick tree fix about it?

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- Why is the prefix array **n+1** long? The appendix below says it removes the `l == 0`
  special case — don't quote that. Write the exact expression that crashes without the
  leading zero, with real numbers in it, and name the index it tries to read.
- A single write to `a[5]` happens between two queries. What breaks, how expensive is it
  to fix, and what structure would you reach for instead?
- Why does `Build` return `long[]` when the input is `int[]`?

---

## Appendix — why both arrays are `n + 1`

Reference, not an exercise. Both PrefixSums and DifferenceArray carry one slot more than
the data — for **opposite reasons, at opposite ends.**

| | prefix sum | difference array |
| --- | --- | --- |
| the extra slot is at the | **front** — index 0 | **back** — index n |
| it holds | `0` | the cancel that falls off the end |
| it exists for the | **query** | **update** |
| because | `prefix[r+1] - prefix[l]` needs `prefix[0]` when `l = 0` | `diff[r+1] -= v` needs index `n` when `r = n-1` |
| is it ever read? | **yes** — part of every query starting at 0 | **no** — the rebuild walks `0..n-1` only |
| without it | `l == 0` needs a special case | you write `r` instead of `r+1`, silently dropping the last element of every range |

**Prefix sum:** `prefix[0] = 0` is "the sum of no elements". It gives `prefix[1] =
prefix[0] + a[0]` something valid to start from, and it makes the query formula work with
no `if` in it.

**Difference array:** `diff[n]` is a bin. An update covering the last element still has to
write its cancel somewhere, and nothing ever reads it back.

> One array needs a **zero to start from**. The other needs a **bin to throw into**.
