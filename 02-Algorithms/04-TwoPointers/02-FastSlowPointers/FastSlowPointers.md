# FastSlowPointers

**Node:** 04 · Two Pointers · **Needs:** array, linked list

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run FastSlowPointers.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence covering all three jobs, not three sentences.

## 2. Brute force first

| Job | Brute force | Cost |
| --- | --- | --- |
| FindMiddle | count the length, then walk half | **two passes** |
| NthFromEnd | count the length, then walk `len - n` | **two passes** |
| HasCycle | `HashSet` of visited nodes | **O(n) space** |

**Measured:** `HasCycle` at n = 100,000 → hashset **100,000** node visits, **5.36 ms**;
fast/slow **50,000** visits, **0.42 ms**

**What work is repeated:**

> ⚠️ Careful: for FindMiddle the honest answer is close to "nothing" — both are O(n). Say
> what the two-pass version *needs to exist* before it can start, and when that is
> impossible.

## 3. The idea

> Two or three sentences, no code. The word to build it around is **gap**.

## 4. Visual trace

> Two of the three jobs, because the gap behaves differently in each.

**Job 1 — the gap GROWS** (ratio 2:1)

```
step   slow   fast   gap   list
   0      1      1     0   sf
   1      2      3     1      s   f
...
```

**Job 2 — the gap is FIXED** (offset n, then slid)

```
phase   slow   fast   gap
   1       1      4     3   <- opened by moving fast n ahead
   2       2      5     3   <- both move, gap preserved
```

Then draw job 3, the cycle, and mark where they meet.

## 5. Why it is faster

> Three separate explanations, one per job. The hard one is job 3 — see section 6.

## 6. Complexity

**Time** — all three O(?)  **Space** — brute O(?) · fast/slow O(?)

**Why does a cycle force a meeting?** This is the proof to be able to give cold:

> Once both are inside the loop, the forward distance from fast to slow changes by ______
> each step, because fast moves ______ and slow moves ______. A quantity changing by
> exactly that amount cannot ______, therefore ______.

**Why speeds 1 and 2 and not 1 and 3?** Write what breaks.

## 7. Side by side

**FindMiddle** — steps are node visits. Both O(n): a **pass-count** win, not a
complexity win.

| n | brute (2 passes) | fast/slow (1 pass) | ratio |
| --- | --- | --- | --- |
| 100 | 150 | 99 | ~1.5× |
| 100,000 | 150,000 | 99,999 | ~1.5× |

**HasCycle** — the win is **memory**, and it does not appear in a step count at all.

| n | hashset steps | hashset memory | fast/slow steps | fast/slow memory |
| --- | --- | --- | --- | --- |
| 100,000 | 100,000 | **100,000 refs** | 50,000 | **2 refs** |

`dotnet run FastSlowPointers.cs -- compare`

> Neither table shows a complexity win. Write one line on what a two-pointer technique
> usually buys, and name the one algorithm in node 04 that is the exception.

## 8. Implementation

- [x] Written from scratch, no library calls → `FastSlowPointers.cs`
- [ ] Written again from memory a week later

> The two things to get right from memory:
>
> 1. the null checks on **fast**, in the correct order — and why checking `slow` is pointless
> 2. where `fast` starts, because it decides **which** middle you get on an even-length list

## 9. Unit tests

`dotnet run FastSlowPointers.cs -- test` — **23/23 passing**

- [x] FindMiddle — 1 through 7 nodes, including every even length
- [x] NthFromEnd — last, first, middle, single, **n too big**, **n = 0**
- [x] HasCycle — no cycle, **self loop**, loop back to head, whole list loops,
      tail to middle, **even loop length**

> Write one line each on why **even loop length**, **single self loop**, and **n too big**
> are the three that would catch a broken implementation.

## 10. Benchmark

`HasCycle` on an **acyclic** list — the worst case, since it must reach the end to answer "no".

| n | hashset steps | hashset ms | fast/slow steps | fast/slow ms |
| --- | --- | --- | --- | --- |
| 1,000 | 1,000 | 0.07 | 500 | 0.00 |
| 10,000 | 10,000 | 0.52 | 5,000 | 0.03 |
| 100,000 | 100,000 | **5.36** | 50,000 | **0.42** |

`dotnet run FastSlowPointers.cs -- bench`

> **~12× apart at identical Big-O.** Say where the hash-set version's time actually goes.
> This is the clearest case in the repo of "complexity counts operations, not what they
> cost on hardware".

## 11. Where is it used?

> `MiddleOfLinkedList`, `RemoveNthFromEnd`, `LinkedListCycle`, `ReorderList`,
> `PalindromeLinkedList`, and `FindTheDuplicateNumber` (which runs this on an array
> treated as a function). Worth naming: what does node 08's `FloydCycleDetection` add
> that this file does not do?

## 12. Recall check

- Prove that a cycle forces a meeting. Then say what breaks at speeds 1 and 3.
- `[1,2,3,4]` — which node is "the middle"? Give both answers and the one line of code
  that chooses between them.
- You can detect a cycle with this in O(1) space. What can you **not** get from it
  without a second phase?
