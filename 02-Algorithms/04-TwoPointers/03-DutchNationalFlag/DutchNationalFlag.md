# DutchNationalFlag

**Node:** 04 · Two Pointers · **Needs:** array

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run DutchNationalFlag.cs -- trace` first, then write section 3 without looking.
>
> ⚠️ **Read sections 7 and 10 before you form an opinion of this algorithm.** The
> measurements do not say what you would expect.

## 1. Purpose

> One sentence. Use the word *partition*, not *sort*, and be ready to say why.

## 2. Brute force first

**Approach:** count the 0s, 1s and 2s, then overwrite.

**Time:** O(?)  **Space:** O(?)  **Passes:** ?

**Measured:** n = 1,000 → **2,000** element touches

**What work is repeated:**

> ⚠️ Careful — the counting version is **fewer touches than the one-pass version** (see
> section 7). So "it repeats work" is wrong. State instead the one **assumption** it
> makes that the one-pass version does not, and when that assumption fails.

## 3. The idea

> Two or three sentences, no code. Name the **four regions** and what is true of each —
> that is the specification, and the pointer rules fall out of it.

```
[ 0 0 0 | 1 1 1 | ? ? ? ? ? | 2 2 2 ]
         ^       ^         ^
         low     mid       high
```

## 4. Visual trace

> Use `[2, 0, 2, 1, 1, 0]`. One row per step with all three pointers.

```
step   low  mid  high   a[mid]   action                   array
   1     0    0     5        2   swap a[0],a[5], high--   [0,0,2,1,1,2]
...
```

## 5. Why it is faster

> **It is not faster.** Write the honest version: what does it actually give you, and what
> does it cost? Three things in the file's `-- compare` output answer this.

## 6. Complexity

**Time** — O(?)  **Space** — O(?)  **Passes** — ?

**Why does it terminate?** Every iteration does what to the unknown region?

**Is it stable?** And does that matter here?

**The asymmetry** — this is the part to be able to explain cold:

| `a[mid]` is | swap with | the value you receive is | so mid |
| --- | --- | --- | --- |
| 0 | `a[low]` | | |
| 1 | — | | |
| 2 | `a[high]` | | |

## 7. Side by side

Steps are element touches. **n = 1,000,000 included so you can see the ratio is flat.**

| n | count+overwrite | three-way partition | ratio |
| --- | --- | --- | --- |
| 100 | 200 | 236 | **0.85×** |
| 1,000 | 2,000 | 2,336 | 0.86× |
| 10,000 | 20,000 | 23,322 | 0.86× |
| 1,000,000 | 2,000,000 | 2,334,418 | **0.86×** |

`dotnet run DutchNationalFlag.cs -- compare`

> **0.86× means the clever version does ~16% MORE work.** Counting does exactly 2n
> touches; the partition does n reads plus 2 per swap, and about ⅔ of elements get
> swapped. Write down why "one pass beats two passes" turned out to be false, and what
> the actual unit of cost is.

## 8. Implementation

- [x] Written from scratch, no library calls → `DutchNationalFlag.cs`
- [ ] Written again from memory a week later

> The one thing to get right from memory: **mid does not advance after a 2.**
> `-- trace` prints the broken version next to the correct one on `[1,2,0]`.

## 9. Unit tests

`dotnet run DutchNationalFlag.cs -- test` — **18/18 passing**

- [x] Empty · each single value · all-same for each value
- [x] Already sorted · exactly reversed · the classic
- [x] **2s at the front** · 0s at the back · no 1s · no 0s · no 2s
- [x] Two elements · alternating · 1,000 random

> Two things to write. Which cases catch an incorrect `mid++` after a 2 — and why does
> `[2,0,2,1,1,0]` (the trace input!) come out **correct even with the bug**? That second
> one is the lesson about test suites.

## 10. Benchmark

| n | count+overwrite ms | three-way ms | **`Array.Sort` ms** |
| --- | --- | --- | --- |
| 10,000 | 0.13 | 0.12 | 0.24 |
| 100,000 | 1.24 | 1.17 | 1.23 |
| 1,000,000 | 13.00 | 12.90 | **15.69** |

`dotnet run DutchNationalFlag.cs -- bench`

> On paper this is **O(n)** and `Array.Sort` is **O(n log n)**, so the gap should be huge.
> It is ~10–15%. Write down the two reasons:
>
> 1. how big is `log n` at n = 1,000,000, really?
> 2. what is special about three-distinct-keys input *for introsort*?
>
> Then write the one sentence this table teaches: *"it has a better Big-O" is not the same
> claim as ______*.

## 11. Where is it used?

> `SortColors` is the direct problem. The real payoff is elsewhere: this is the
> **three-way partition** that makes a quicksort immune to arrays full of duplicate
> pivots. Worth naming what two-way (Hoare/Lomuto) partition does differently and why
> duplicates hurt it.

## 12. Recall check

- Why does `mid` advance after a 0 but not after a 2? Answer with *where the incoming
  value came from*, not with the rule.
- You have **four** categories instead of three. Does this extend? What do you use?
- Is this faster than counting the three values? Give the number, and then give the two
  real reasons to use it anyway.
