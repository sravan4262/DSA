# MergeSort

**Node:** 02 · Sorting · **Needs:** array, recursion

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run MergeSort.cs -- trace` first, then write section 3 without looking.
>
> **This is the only sorting algorithm in the roadmap.** The README records why, and what
> was cut.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> The baseline here is **insertion sort**, and it is in the file so `-- compare` can run it.

**Approach:**

**Time:** Best O(?) · Average O(?) · Worst O(?)  **Space:** O(?)

**Measured:** n = 1,000 random → **250,194** comparisons. n = 1,000 **already sorted** →
**999**.

**What work is repeated:**

> Careful — insertion sort has a genuinely good best case, so "it's just slow" is wrong.
> Say what it repeats *on unsorted data*, and what it is doing right when the data is
> already in order.

## 3. The idea

> Two or three sentences, no code. Three words to build it around: **split, sort, merge.**
> Then say which of the three does the actual work.

## 4. Visual trace

> Use `[38, 27, 43, 3, 9, 82, 10]` — the code's input. Draw the call tree going down and
> the merges coming back up. Indentation is depth.

```
split [38,27,43,3,9,82,10]  ->  [38,27,43,3] and [9,82,10]
  split ...
    leaf [38]
  MERGE ... -> ...
```

Then one merge on its own, pointer by pointer:

```
left  = [3, 27, 38, 43]
right = [9, 10, 82]

compare   take   output
3 <= 9     L 3   [3]
...
```

## 5. Why it is faster

> Two separate things to explain, and they are not the same question.
>
> **(a)** Why does merging two sorted halves take **n** comparisons instead of n²? The
> answer is a sentence about where the smallest unplaced element can possibly be.
>
> **(b)** Why does the recursion have to finish *before* the merge starts?

## 6. Complexity

**Time** — Best: O(?) · Average: O(?) · Worst: O(?)

**Why are all three the same?** Derive it, do not quote it. Two facts multiplied:

| | |
| --- | --- |
| How many levels? | halving n to 1 takes ______ steps |
| How much work per level? | every element is merged ______ time(s) → ______ |
| Total | ______ |

**Space** — O(?) for the scratch array, **plus** O(?) for the recursion stack.

**Which case am I quoting?**

> Mergesort is the rare algorithm where best = average = worst, so no case label is
> really in play. Say why, and then say what that **guarantee** is worth compared with
> insertion sort's O(n) best / O(n²) worst.

## 7. Side by side

Steps are **element comparisons**. Three input shapes, because this is the one place in
the roadmap where the brute force sometimes **wins**.

**Random**

| n | insertion sort | merge sort | ratio |
| --- | --- | --- | --- |
| 100 | 2,485 | 535 | 5× |
| 1,000 | 250,194 | 8,725 | 29× |
| 10,000 | 24,882,158 | 120,465 | 207× |
| 50,000 | 628,074,114 | 718,047 | **875×** |

**Already sorted** — insertion sort wins

| n | insertion sort | merge sort | ratio |
| --- | --- | --- | --- |
| 100 | **99** | 356 | 1/3.6 |
| 1,000 | **999** | 5,044 | 1/5.0 |
| 10,000 | **9,999** | 69,008 | 1/6.9 |
| 50,000 | **49,999** | 401,952 | **1/8.0** |

**Reverse sorted** — insertion sort's worst case

| n | insertion sort | merge sort | ratio |
| --- | --- | --- | --- |
| 100 | 4,950 | 316 | 16× |
| 1,000 | 499,500 | 4,932 | 101× |
| 10,000 | 49,995,000 | 64,608 | 774× |
| 50,000 | 1,249,975,000 | 382,512 | **3,268×** |

`dotnet run MergeSort.cs -- compare`

> The middle table is the interesting one. Write one line on what insertion sort is
> exploiting that mergesort structurally cannot, and one line on why mergesort is still
> the right default.
>
> Also note mergesort's own numbers wobble ~2× across the three shapes (718,047 /
> 401,952 / 382,512 at n=50,000). The file explains why. Say it in your own words.

## 8. Implementation

- [x] Written from scratch, no library calls → `MergeSort.cs`
- [ ] Written again from memory a week later

> Three details to get right from memory:
>
> 1. `mid = lo + (hi - lo) / 2`, **not** `(lo + hi) / 2` — say why
> 2. the scratch array is allocated **once** in the public method, not inside the recursion
> 3. `temp[l] <= temp[r]`, not `<`

## 9. Unit tests

`dotnet run MergeSort.cs -- test` — **16/16 passing, plus a separate stability check**

- [x] Empty · single · two sorted · two reversed · three reversed
- [x] Already sorted · reverse sorted · all identical · duplicates
- [x] Negatives · `int.MaxValue`/`MinValue` · odd length · even length
- [x] One element out of place · 1,000 random
- [x] **Stability** — tagged `(key, tag)` pairs, ties must come out `x y z a b`

> Two things to write down. Why is `Array.Sort` used here as an **oracle** rather than as
> the implementation? And why can `[7,7,7,7]` and `[3,1,3,1,2]` **not** test stability,
> even though they are full of ties?

## 10. Benchmark

Random input. Steps are element comparisons.

| n | insertion steps | insertion ms | merge steps | merge ms |
| --- | --- | --- | --- | --- |
| 1,000 | 243,796 | 1.03 | 8,720 | 0.14 |
| 10,000 | 24,896,436 | 72.26 | 120,508 | 1.72 |
| 50,000 | 628,179,078 | 1,879.00 | 718,133 | 9.52 |
| 100,000 | 2,500,211,358 | **7,483.67** | 1,536,419 | **21.74** |

`dotnet run MergeSort.cs -- bench`

> 7.5 seconds vs 21 milliseconds. Work out the ratio, then check it against the n/log n
> you would predict.

## 11. Where is it used?

> Real systems and what it unlocks. Worth naming:
>
> - `Array.Sort` is **introsort**, not mergesort — what is introsort, and why does it keep
>   insertion sort around? (The middle table in section 7 is the answer.)
> - `OrderBy` in LINQ **is** a stable sort. Why does LINQ guarantee that when `Array.Sort`
>   does not?
> - The **merge step alone** is reused by `MergeFromBack` (04) and `KWayMerge` (12).
> - Why is mergesort the standard choice for **external** sorting — data too big for RAM?

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- Derive O(n log n) from scratch: where does the `log n` come from, where does the `n`
  come from, and why does every level cost the same?
- What makes mergesort **stable**, and what is the exact one-character change that breaks
  it? Give the input and both outputs.
- You have 8 GB of RAM and 500 GB of data to sort. Which of mergesort's properties makes
  it the one that can do this at all, and which of quicksort's makes it unable to?
