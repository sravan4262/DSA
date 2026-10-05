# InPlaceWritePointer

**Node:** 01 · Arrays & Hashing · **Needs:** array

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run InPlaceWritePointer.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> Never skip this.

**Approach:**

**Time:** O(?)  **Space:** O(?)

**Measured:** n = 10,000, half surviving → **9,904** writes and **4,952 ints** of extra
memory

**What work is repeated:**

> ⚠️ Careful — this is the one algorithm in the node where the honest answer is
> **"nothing"**. Both versions are one pass and O(n). Say what the brute force wastes
> instead, and why that still makes it the wrong answer to this question.

## 3. The idea

> Two or three sentences, no code. Name what each of the two indices means — they are not
> symmetric and the difference is the whole pattern.

## 4. Visual trace

> Use `[0, 1, 0, 3, 0, 12]`, removing zeros. One row per `read`, showing `write` and the
> array after. Then draw the **answer / garbage split** at the end.

```
read   a[read]   keep?   write   array after
   0      0       no       0     [ 0, 1, 0, 3, 0, 12]
   1      1      yes       ?     [ ?, ...
...

final  [ 1,  3, 12,  ?,  ?,  ?]
         ^^^^^^^^^^  ^^^^^^^^^^
         answer      ?
```

## 5. Why it is faster

> It is **not** faster. Write the honest version: what does it actually save, and what is
> the invariant that proves the brute force's second array was never needed?

> The invariant to state and justify: `write <= read`, always.

## 6. Complexity

**Time** — brute: O(?) · write pointer: O(?)

**Space** — brute: O(?) · write pointer: O(?)

**Why?**

**Which case am I quoting?**

> No early exit exists on any input, so the step counts are **exact**. But the *write*
> counts are not — they depend on the data. Say what they depend on.

## 7. Side by side

n = 10,000, varying how much of the array survives. Steps are **element writes**, since
all three read every element exactly once.

| survive | brute writes | writeptr writes | swap writes | brute extra space |
| --- | --- | --- | --- | --- |
| 10% | 2,082 | **1,041** | 8,959 | 1,041 ints |
| 50% | 9,904 | **4,952** | 5,048 | 4,952 ints |
| 90% | 17,980 | 8,990 | **1,010** | 8,990 ints |
| 100% | 20,000 | 10,000 | **0** | 10,000 ints |

`dotnet run InPlaceWritePointer.cs -- compare`

> Read the last column first — that is the number this pattern exists to make zero, and
> the only column where the brute force genuinely loses.
>
> Then the crossover: the write pointer costs one write per **keeper**, swap-with-last one
> per **discard**. Write the rule for picking between them, and say why the 100% row has
> swap doing *zero* writes.

## 8. Implementation

- [x] Written from scratch, no library calls → `InPlaceWritePointer.cs`
- [ ] Written again from memory a week later

> Three implementations are in the file: brute force, write pointer, and swap-with-last.
> The thing to get right from memory in the third one: **`i` must not advance after a
> swap.** The trace shows exactly why — at `i = 2` it pulls in another `0`.

## 9. Unit tests

`dotnet run InPlaceWritePointer.cs -- test` — **14/14 passing**

- [x] Empty · single keep · single remove
- [x] Nothing to remove · **remove everything**
- [x] Remove first · remove last
- [x] The classic — `[0,1,0,3,0,12]` → `[1,3,12]`, length 3
- [x] Alternating · **adjacent removals** · duplicates kept
- [x] Negatives kept · removing a negative value · two elements

> The three implementations are **checked differently** — two by exact order, one as a
> multiset. Write one line on why holding all three to the same test would fail a correct
> implementation.
>
> And one line each on why **remove everything** and **adjacent removals** are the two
> that would catch a broken version.

## 10. Benchmark

Half the elements survive. Steps are element writes.

| n | brute steps | brute ms | writeptr steps | writeptr ms |
| --- | --- | --- | --- | --- |
| 1,000 | 996 | 0.02 | 498 | 0.01 |
| 10,000 | 9,930 | 0.13 | 4,965 | 0.07 |
| 100,000 | 100,092 | 0.98 | 50,046 | 0.67 |

`dotnet run InPlaceWritePointer.cs -- bench`

> **Both curves are linear** — this was never a time optimisation. Say what the remaining
> gap actually is, and how much memory the brute force asks for at n = 1,000,000 that the
> other never does.

## 11. Where is it used?

> Real systems and which problems it unlocks — `RemoveElement`, `RemoveDuplicates`,
> `MoveZeroes`, and the partition step of QuickSort. Worth naming what "in place, O(1)
> extra space" signals when you see it in a problem statement.

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- State the invariant that makes overwriting safe, and prove it — why can `write` never
  get ahead of `read`?
- After the call returns 3 on a 6-element array, what is in slots 3, 4 and 5? What is the
  **only** thing telling the caller where the answer stops, and what is the classic bug?
- When would you use swap-with-last instead, what do you give up, and why must `i` not
  advance after a swap?
