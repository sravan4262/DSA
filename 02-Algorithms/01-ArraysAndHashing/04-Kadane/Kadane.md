# Kadane

**Node:** 01 · Arrays & Hashing · **Needs:** array

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run Kadane.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> Never skip this.

**Approach:**

**Time:** O(?)  **Space:** O(?)

**Measured:** n = 1,000 → **500,500** steps

**What work is repeated:**

> Note the brute force in the file is already the *smart* one — it carries a running sum
> across the inner loop. The truly naive version re-sums each range and is O(n³). Say
> what the smart version still repeats.

## 3. The idea

> Two or three sentences, no code. The sentence to aim for is a **question asked at every
> index**. Write the question.

## 4. Visual trace

> Use `[-2, 1, -3, 4, -1, 2, 1, -5, 4]` — the code's input. One row per index, showing
> `extend` vs `fresh` and which one won.

```
 i   a[i]   cur+a[i]   a[i]   decision   current   best
 0    -2       -        -      seed        -2       -2
 1     1       ?        ?        ?          ?        ?
...
```

## 5. Why it is faster

> The one claim to make precise: **a negative running sum can never help a later
> subarray.** Say why that is true, and why it means a decision made at index i never has
> to be revisited.

## 6. Complexity

**Time** — Best: O(?) · Average: O(?) · Worst: O(?)

**Why are all three the same?**

**Space** — O(?)

**Which case am I quoting?**

> This one is unusual: there is no early exit on any input, so every input is the worst
> case and the step counts are **exact**, not typical. Say why that means no case label
> is really in play.

## 7. Side by side

| n | brute force | Kadane | ratio |
| --- | --- | --- | --- |
| 100 | 5,050 | 100 | 50× |
| 1,000 | 500,500 | 1,000 | 500× |
| 10,000 | 50,005,000 | 10,000 | 5,000× |
| 50,000 | 1,250,025,000 | 50,000 | 25,000× |

`dotnet run Kadane.cs -- compare`

> The thing to notice, and it is unique in this node: **both implementations are O(1)
> space.** The other four algorithms here bought their speed with an extra O(n) array.
> Kadane buys it with an idea and pays nothing. Write one line on what that means for how
> you'd arrive at it in an interview.

## 8. Implementation

- [x] Written from scratch, no library calls → `Kadane.cs`
- [ ] Written again from memory a week later

> The thing to get right from memory: **seed `best` from `a[0]` and loop from index 1.**
> Seeding at 0 is the single most common wrong answer.

## 9. Unit tests

`dotnet run Kadane.cs -- test` — **16/16 passing**

- [x] Single positive · single negative · single zero
- [x] Two elements · all positive · **all negative** · all zeros
- [x] The classic — `[-2,1,-3,4,-1,2,1,-5,4]` → 6
- [x] Best at start · best at end · whole array wins
- [x] **Big dip in middle** `[10,-100,10]` → 10 · **dip worth crossing** `[10,-1,10]` → 19
- [x] Alternating · negatives then positive
- [x] **Overflow** — 1,000 × `int.MaxValue`

> Three earn their place. Write one line each on why **all negative**, and why the two
> **dip** cases must disagree with each other.

## 10. Benchmark

| n | brute steps | brute ms | Kadane steps | Kadane ms |
| --- | --- | --- | --- | --- |
| 100 | 5,050 | 0.02 | 100 | 0.00 |
| 1,000 | 500,500 | 1.32 | 1,000 | 0.00 |
| 10,000 | 50,005,000 | 124.56 | 10,000 | 0.05 |
| 50,000 | 1,250,025,000 | 3,273.08 | 50,000 | 0.24 |

`dotnet run Kadane.cs -- bench`

## 11. Where is it used?

> Real systems and which problems it unlocks. Worth naming: what changes for **maximum
> *product* subarray**, and why that one needs two variables instead of one.

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- Write the DP recurrence this is, then say why it needs **O(1)** space rather than the
  O(n) table the recurrence implies. (Node 17 calls this "rolling the state".)
- Why does seeding `best = 0` break, and what question about the problem statement
  decides whether it's a bug at all?
- Why can Kadane commit to a decision at index `i` and never revisit it? Give the
  property of negative running sums that makes that safe.

---

## Appendix — the short version

Reference, not an exercise. This is the explanation to fall back on when the
subarray-counting gets confusing.

### The decision

At index `i` you want the best subarray **ending at `i`**. It must contain `arr[i]`. The
only open question is whether it also includes the stuff before:

```
include it      maxEnding + arr[i]
don't           arr[i]
```

You are adding `arr[i]` **either way**. The only difference between those two lines is
whether you also add `maxEnding`. So:

### The rule, both halves

| `maxEnding` | what happens | what you drop |
| --- | --- | --- |
| **negative** | restart — `maxEnding = arr[i]` | every subarray starting **before** `i` |
| **positive** | extend — `maxEnding += arr[i]` | every subarray starting **at** `i` |

One of the two families dies either way. You never drop the subarrays **ending** at `i` —
one of them is the winner.

`Math.Max(arr[i], maxEnding + arr[i])` is that sign check written as a comparison: when
`maxEnding` is negative, `arr[i]` is automatically the bigger of the two.

### The trap: the test is against ZERO, not against `arr[i]`

> *"restart when the previous sum is smaller than the current element"* — **wrong.**

```
maxEnding = 5,  arr[i] = 10        5 < 10, but you do NOT restart:

maxEnding + arr[i]  =  5 + 10  =  15     ← wins
arr[i]              =       10  =  10
```

Do the algebra and `arr[i]` cancels out of the comparison completely:

```
maxEnding + arr[i]  <  arr[i]
maxEnding           <  0
```

**Only the sign of `maxEnding` decides.** The current element never enters it.

### Naming

The same variable goes by three names across this repo and the usual write-ups — they are
all the running "best ending here":

| `Kadane.cs` | this appendix | most articles |
| --- | --- | --- |
| `current` | `maxEnding` | `maxEnding` / `maxSoFar` |
| `best` | `res` | `res` / `maxGlobal` |

### Why one comparison settles many subarrays

Take three elements. Line up the comparison at index 1 and the one at index 2:

```
index 1:    a0 + a1          vs    a1            ->  differ by a0
index 2:    a0 + a1 + a2     vs    a1 + a2       ->  differ by a0
```

**Same difference.** `a2` is added to both sides, so it cancels out of the comparison
entirely. You already learned the sign of `a0` at index 1, so the second comparison has no
new information in it.

Which family you drop depends on which way the first comparison went:

| at index 1 | means | survivor | dropped at index 2 |
| --- | --- | --- | --- |
| `a0 + a1 < a1` | `a0` negative | start **1** | **`[0..2]`** |
| `a0 + a1 > a1` | `a0` positive | start **0** | `[1..2]` |

Both rows generalise: whatever is dropped stays dropped, because every later element lands
on both candidates equally and the gap never closes.

### Worked, `arr = [3, -5, 4, 2]`

| i | `arr[i]` | `maxEnding` in | sign | best ending at `i` |
| --- | --- | --- | --- | --- |
| 0 | 3 | — | — | `3` = **3** |
| 1 | −5 | **3** | + | `3, -5` = **−2** |
| 2 | 4 | **−2** | − | `4` = **4** |
| 3 | 2 | **4** | + | `4, 2` = **6** |

```
maxEnding:   3,  -2,   4,   6
res:         3,   3,   4,   6     <- answer 6, from [4, 2]
```

`res` is a second variable because `maxEnding` is allowed to fall (step 1, 3 → −2) and the
answer is not.
