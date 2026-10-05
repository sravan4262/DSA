# MergeFromBack

**Node:** 04 · Two Pointers · **Needs:** array, [MergeSort](../../02-Sorting/01-MergeSort/MergeSort.md)

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run MergeFromBack.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence.

## 2. Brute force first

**Approach:** merge forward into a temp array, then copy it back.

**Time:** O(?)  **Space:** O(?)

**Measured:** m+n = 1,000,000 → **2,000,000** writes and **1,000,000 ints** of temp

**What work is repeated:**

> And the prior question: **why does the temp array exist at all?** `-- trace` shows the
> forward collision rather than describing it. Write what gets destroyed and when.

## 3. The idea

> Two or three sentences, no code. Three pointers, all starting where?

## 4. Visual trace

> Use `a = [1,3,5,_,_,_]`, `b = [2,4,6]`. Include the `k-i` column — it is the proof.

```
step    i    j    k   compare             write        k-i   a
   1    2    2    5   a[2]=5 <= b[2]=6    a[5] = 6       3   [1,3,5,0,0,6]
...
```

## 5. Why it is faster

> It is the same O(m+n) in time. Say what it saves, then prove the thing that makes it
> legal at all — see section 6.

## 6. Complexity

**Time** — O(?)  **Space** — brute O(?) · from-back O(?)

**The safety invariant.** `k` writes and `i` reads **in the same array**, so `k`
overtaking `i` is the only possible bug. Fill this in and prove it:

```
k - i = ______

on entry:   k - i = ____  and the right side = ____     agree?
each step:  k drops by 1, and exactly one of i or j drops by 1
              i drops -> left side ____, right side ____
              j drops -> left side ____, right side ____
loop runs while j >= 0, therefore k - i >= ____
```

> Then compare with `InPlaceWritePointer` (node 01), whose invariant was `write <= read`.
> Same obligation, opposite direction. What is the rule underneath both?

**Which drain loop is required?** One of them is, one never is. Say which and why.

## 7. Side by side

Interleaved input, m = n. Steps are **element writes**.

| m+n | forward+temp writes | from-back writes | temp extra space |
| --- | --- | --- | --- |
| 100 | 200 | 99 | 100 ints |
| 1,000 | 2,000 | 999 | 1,000 ints |
| 10,000 | 20,000 | 9,999 | 10,000 ints |
| 1,000,000 | 2,000,000 | 999,999 | **1,000,000 ints** |

`dotnet run MergeFromBack.cs -- compare`

> A flat **2×** on writes — because the forward version writes every element twice. But
> the column that matters is the last one. Write one line on why it is slightly absurd to
> allocate that temp, given what the input already contained.

## 8. Implementation

- [x] Written from scratch, no library calls → `MergeFromBack.cs`
- [ ] Written again from memory a week later

> Two things to get right from memory: the loop is driven by **`j`**, not by `k` or `i`;
> and the `i >= 0` check must come **before** `a[i]` is read.

## 9. Unit tests

`dotnet run MergeFromBack.cs -- test` — **15/15 passing**

- [x] Both empty · **b empty** · a empty
- [x] Interleaved · a all smaller · **b all smaller** · single each
- [x] Duplicates across · all identical
- [x] Uneven both ways · negatives · touching at end · **a exhausts first**
- [x] `int.MinValue` / `int.MaxValue`

> Three to write about. Which two catch a **missing drain loop for b**? Why is **b empty**
> the mirror case that exposes a `k`-driven loop? And why does this file need no `long`
> arithmetic when `ConvergingPointers` did?

## 10. Benchmark

| m+n | forward steps | forward ms | from-back steps | from-back ms |
| --- | --- | --- | --- | --- |
| 10,000 | 20,000 | 0.09 | 9,999 | 0.05 |
| 100,000 | 200,000 | 1.01 | 99,999 | 0.42 |
| 1,000,000 | 2,000,000 | 7.88 | 999,999 | 4.94 |
| 10,000,000 | 20,000,000 | **84.86** | 9,999,999 | **46.13** |

`dotnet run MergeFromBack.cs -- bench`

> Both linear, ~1.8× apart. Account for the gap: part is the second write pass, part is
> something that does not appear in the step count at all. At m+n = 10,000,000, how many
> MB is the temp?

## 11. Where is it used?

> `MergeSortedArray` is the direct problem. The merge itself is `MergeSort`'s merge step
> (node 02) with the scratch array removed, and the same backward-filling trick turns up
> in string manipulation in place. Worth naming: when is this **not** available, and what
> do you fall back to?

## 12. Recall check

- State and prove the invariant `k - i = ______`. Why does it mean a collision is
  *arithmetically impossible* rather than merely unlikely?
- The free slots are at the **front** instead: `[_,_,_,1,3,5]`. Which direction do you
  fill, and why? State the general rule in one sentence.
- Exactly one drain loop is needed. Which, and what is the concrete input that breaks if
  you write the other one instead?
