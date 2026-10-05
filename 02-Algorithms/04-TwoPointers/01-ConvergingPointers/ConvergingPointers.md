# ConvergingPointers

**Node:** 04 · Two Pointers · **Needs:** array, [MergeSort](../../02-Sorting/01-MergeSort/MergeSort.md)

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run ConvergingPointers.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence.

## 2. Brute force first

**Approach:**

**Time:** O(?)  **Space:** O(?)

**Measured:** n = 1,000, no pair exists → **499,500** pair evaluations

**What work is repeated:**

> Worth noticing before you answer: the brute force does **not** require sorted input.
> The fast version does. Say which one is more general, and what that tells you about
> where the speed comes from.

## 3. The idea

> Two or three sentences, no code.

## 4. Visual trace

> Use `[1, 3, 4, 6, 8, 11]`, target 10. Draw the window closing.

```
[ 1]  3   4   6   8 [11]    1+11 = 12   too big  -> r--
...
```

## 5. Why it is faster

> ⚠️ This is the section that matters most in this file, and "it tries fewer pairs" is
> not the answer. Each step **proves** a set of pairs impossible. Write the proof for
> `r--`, then the mirror proof for `l++`.

## 6. Complexity

**Time** — O(?) · **Space** — O(?)

**Why?** Total pointer movement is ______, so the loop runs at most ______ times.

**The precondition's cost** — fill this in, it is the real answer to "should I use this":

| Situation | Total cost |
| --- | --- |
| Input already sorted | |
| Input unsorted | |
| Need indices into the *original* array | |

## 7. Side by side

Worst case for both — no pair sums to the target, so neither exits early.

| n | brute force | converging | ratio |
| --- | --- | --- | --- |
| 100 | 4,950 | 99 | 50× |
| 1,000 | 499,500 | 999 | 500× |
| 10,000 | 49,995,000 | 9,999 | 5,000× |
| 50,000 | 1,249,975,000 | 49,999 | **25,000×** |

`dotnet run ConvergingPointers.cs -- compare`

> Both are **O(1) space** — like Kadane and unlike the rest of node 01. Say what paid
> for the speedup here, since memory did not.

## 8. Implementation

- [x] Written from scratch, no library calls → `ConvergingPointers.cs`
- [ ] Written again from memory a week later

> Also in the file: `IsPalindrome` and `ReverseInPlace`, the same shape used for
> **symmetry** rather than **ordering**. Write one line on why neither needs sorted input.

## 9. Unit tests

`dotnet run ConvergingPointers.cs -- test` — **15/15 passing**

- [x] Empty · single · two hit · two miss
- [x] Pair at both ends · adjacent in the middle · first two · last two
- [x] No pair · target below the minimum · all identical and missing
- [x] **Duplicates** — `[3,3,3,3]` target 6 returns **(0,3)**, not (0,1)
- [x] Negatives · sums to zero
- [x] **Overflow pair** — two values near `int.MaxValue`

> Two to write about. Why does `[3,3,3,3]` return the **outermost** pair, and why is that
> a *choice* rather than a correctness question? And what exactly goes wrong on the
> overflow case if the sum is computed in `int` — which pointer moves, and why?

## 10. Benchmark

| n | brute steps | brute ms | converging steps | converging ms |
| --- | --- | --- | --- | --- |
| 1,000 | 499,500 | — | 999 | — |
| 10,000 | 49,995,000 | — | 9,999 | — |
| 50,000 | 1,249,975,000 | — | 49,999 | — |
| 100,000 | 4,999,950,000 | — | 99,999 | — |

`dotnet run ConvergingPointers.cs -- bench` and paste the ms column.

## 11. Where is it used?

> Real systems and what it unlocks — `TwoSum II`, `ThreeSum` (this as the inner loop),
> `ContainerWithMostWater`, `TrappingRainWater`, `ValidPalindrome`. Worth naming: in
> `ThreeSum`, what is the outer loop doing and why is the total O(n²) rather than O(n³)?

## 12. Recall check

- Prove that `l++` is safe when `a[l] + a[r] < target`. Not "because it's sorted" — the
  actual argument about which pairs are eliminated.
- The array is unsorted and you need indices into the original. What do you use instead,
  and what does it cost?
- `IsPalindrome` uses the same two-pointer movement with no sortedness at all. What is
  each technique actually exploiting?
