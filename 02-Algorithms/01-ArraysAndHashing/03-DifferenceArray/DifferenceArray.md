# DifferenceArray

**Node:** 01 · Arrays & Hashing · **Needs:** array, [PrefixSums](../02-PrefixSums/PrefixSums.md)

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
>
> **Do PrefixSums first.** This one is its mirror image and will not land otherwise.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> Never skip this. The optimisation is meaningless without the thing it beats.

**Approach:**

**Time:** O(?) per update · O(?) for u updates  **Space:** O(?)

**Measured:** n = 1,000, 1,000 updates → **1,000,000** steps

**What work is repeated:**

## 3. The idea

> Two or three sentences, no code. The sentence to aim for starts *"don't store the
> values, store the ..."*.

## 4. Visual trace

> Use the code's input — 6 zeros, then `+5@1..3`, `+2@0..2`, `+10@4..5`. Draw `diff`
> after each update, then the integrate pass. Mark which slot is written and never read.

```
diff    = [  ?,   ?,   ?,   ?,   ?,   ?,   ? ]
                                               ^ never read

running = ...
result  = [  ?,   ?,   ?,   ?,   ?,   ? ]
```

## 5. Why it is faster

> Name the specific work the brute force did that this avoids. Then answer the harder
> question: **why does a running total reconstruct the values at all?**

## 6. Complexity

**Per update** — Time: O(?) · **Integrate pass** — Time: O(?) · **Space** — O(?)

**u updates then one read** — brute: O(?) · diff: O(?)

**Why?**

**Which case am I quoting?**

**Where is the break-even?** How many updates before this wins, and why not one?

## 7. Side by side

n updates over an array of n, each spanning the whole array, then one read pass.

| n | brute force | diff total | of which integrate | ratio |
| --- | --- | --- | --- | --- |
| 100 | 10,000 | 300 | 100 | 33× |
| 1,000 | 1,000,000 | 3,000 | 1,000 | 333× |
| 10,000 | 100,000,000 | 30,000 | 10,000 | 3,333× |
| 50,000 | 2,500,000,000 | 150,000 | 50,000 | 16,667× |

`dotnet run DifferenceArray.cs -- compare`

> The diff column is **3n** — 2 writes per update plus one n-long pass. Write one line on
> where each of those three n's comes from.

## 8. Implementation

- [x] Written from scratch, no library calls → `DifferenceArray.cs`
- [ ] Written again from memory a week later

> The two things to get right from memory: **`n+1` sizing** and **the cancel at `r+1`,
> not `r`**. `dotnet run DifferenceArray.cs -- trace` prints both versions side by side
> at the bottom so you can see what the bug costs.

## 9. Unit tests

`dotnet run DifferenceArray.cs -- test` — **13/13 passing**

- [x] No updates · single cell · whole array
- [x] `l == 0` and `r == last` — the second one is what catches the off-by-one
- [x] `n == 1`
- [x] Overlapping · nested · **adjacent with no gap**
- [x] Negative `v` · cancels to zero · same range twice
- [x] **Overflow** — 1,000 × `int.MaxValue` into one cell

> Two of these earn their place. Write one line each on why **"ends at last"** and
> **"adjacent, no gap"** are the two that would catch a broken implementation.

## 10. Benchmark

| n | brute steps | brute ms | diff steps | diff ms |
| --- | --- | --- | --- | --- |
| 100 | 10,000 | 0.03 | 300 | 0.02 |
| 1,000 | 1,000,000 | 2.00 | 3,000 | 0.02 |
| 10,000 | 100,000,000 | 218.06 | 30,000 | 0.16 |

`dotnet run DifferenceArray.cs -- bench`

## 11. Where is it used?

> Real systems and which problems it unlocks. Worth naming: "how many meetings overlap at
> once", flight bookings, and what the 2-D version does with four writes instead of two.

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- Why is the cancel written at `r + 1` instead of `r`, and what exactly goes wrong if you
  use `r`? (Give the array, not the word "off-by-one".)
- You need to read `a[5]` **between** two updates. What does that cost, and what does it
  do to the whole argument for this structure?
- Fill in both blanks and say why they're the same sentence pointed two ways:
  *PrefixSums makes range ______ O(1) and leaves ______ O(n). DifferenceArray makes range
  ______ O(1) and leaves ______ O(n).*
