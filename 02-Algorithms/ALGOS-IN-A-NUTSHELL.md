# Algorithms in a Nutshell

One screen per algorithm. Every cost is tied to **the invariant** — the thing that stays
true on every step — because that is where the complexity comes from, and it is what you
have to be able to say out loud.

Deep version of any algorithm: the `.md` guide in its folder.
Structures this is all built on: [DS-IN-A-NUTSHELL.md](../01-DataStructures/DS-IN-A-NUTSHELL.md).

## Contents

| Node | Algorithm | In one line |
| --- | --- | --- |
| 01 | [Hash Map Counting](#hash-map-counting) | trade memory for time — the table deletes the inner loop |
| 01 | [Prefix Sums](#prefix-sums) | add up to each index, subtract to read a range |
| 01 | [Difference Array](#difference-array) | store the gaps, so a range update is two writes |
| 01 | [Kadane](#kadane) | one pass, two numbers, no subarray ever built |

> This file grows by one entry per algorithm. The roadmap is in
> [README.md](../README.md) — **87** algorithms across 22 nodes.

## Reference

| Section | What it answers |
| --- | --- |
| [Complexity grid](#complexity-grid) | all of them, side by side |
| [Recognition signals](#recognition-signals) | problem phrase → algorithm |
| [Classic bugs](#classic-bugs) | the ones that actually bite |

---

## Hash Map Counting

> Trade **memory for time**: one pass with a hash table replaces a nested loop.

**The sentence it answers** — *"has this been seen before?"* or *"how many times does each
value appear?"*

**The shape**

```
brute force    for each i, scan everything after it        O(n²), O(1) space
hash           for each i, ask the table                   O(n),  O(n) space
```

Three jobs, three different structures — picking the wrong one is the usual mistake:

| Job | Structure | The call |
| --- | --- | --- |
| frequency | `Dictionary<T,int>` | `freq[x] = freq.GetValueOrDefault(x) + 1` |
| membership | `HashSet<T>` built **before** the pass | `set.Contains(x)` |
| seen-before | `HashSet<T>` built **during** the pass | `if (!seen.Add(x)) ...` |

**Cost**

| Op | Big-O | Case |
| --- | --- | --- |
| one pass | O(n) | **average** |
| single lookup | **O(1)** | **average** — O(n) worst, if every key collides |
| space | O(n) | |

Note the case label. **Average, not worst** — that is the only thing separating a hash
lookup from an array index.

**Why it works** — a hash turns the key itself into an address, so *finding* becomes
*computing*. The `n` in "search n items" simply vanishes.

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| duplicates, anagrams, two-sum, counting | order matters → sort, or a BST |
| the brute force is a nested loop over the same array | memory is tight → sort in place, O(1) space |

**Classic bug** — `HashSet.Add` returns a `bool`. Calling `Contains` first and then `Add`
does the same lookup twice.

**Say this:** *"A hash table turns searching into addressing. You pay O(n) memory to
delete the inner loop."*

Deep dive: [01-HashMapCounting/](01-ArraysAndHashing/01-HashMapCounting/) · [↑ Contents](#contents)

---

## Prefix Sums

> Pay O(n) once, so every range read afterwards is **one subtraction**.

**The sentence it answers** — *"give me the sum from index 1 to 100"*, asked many times.

**The shape**

```
a        [3, 1, 4, 1, 5]
prefix   [0, 3, 4, 8, 9, 14]     <- n+1 slots, leading zero at the FRONT
          ^
     the sum of no elements

sum(l..r) = prefix[r+1] - prefix[l]
```

**Cost**

| Op | Big-O | Case |
| --- | --- | --- |
| build | O(n) | worst — **once**, up front |
| query | **O(1)** | worst |
| space | O(n) | |
| change `a[i]` | **O(n)** | rebuild — which is why the data must be frozen |

**Why it works** — telescoping. Everything before `l` appears once positive and once
negative, so it cancels and only the range survives.

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| many range reads, data never changes | one query — just loop the range |
| counts over a range (build it over a 0/1 array) | data changes between reads → **Fenwick tree** |

**Classic bug** — no leading zero, so `l == 0` reaches for `prefix[-1]`.

**Pairs with** — [Difference Array](#difference-array), its exact inverse.

**Say this:** *"Add up to each index, so you can subtract to find any range."*

Deep dive: [02-PrefixSums/](01-ArraysAndHashing/02-PrefixSums/) · [↑ Contents](#contents)

---

## Difference Array

> The inverse of a prefix sum. Two writes per range update, one rebuild at the end.

**The sentence it answers** — *"add 5 to index 1 through 100"*, **told** many times.

**The shape**

```
a        [1, 3, 5, 3]
diff     [1, 2, 2, -2, 0]        <- n+1 slots, spare at the BACK
                       ^
          somewhere to put the cancel when r is the last index

update   diff[l] += v        diff[r+1] -= v          O(1), any range length
rebuild  running += diff[i]                          a prefix sum
```

**Cost**

| Op | Big-O | Case |
| --- | --- | --- |
| build | O(n) | once — **skipped entirely** when `a` starts as zeros |
| update | **O(1)** | worst — 2 writes, however long the range |
| rebuild | O(n) | once, at the end |
| read `a[i]` mid-stream | **O(n)** | which is why reads must be deferred |

**Why it works** — adding a constant to a range does not change the gaps *inside* it. Both
neighbours move by the same amount, so it cancels in the subtraction. Only the two edges
have a gap that changes.

**The crossover** — the fixed cost is `2n` (or `n` starting from zeros), so it only pays
off past roughly **4 updates** (or **2** from zeros). Below that the brute force wins.

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| many range updates, array read once at the end | you must read between updates → **Fenwick tree** |
| flight bookings, car pooling, range addition | one or two updates — just write the cells |

**Classic bug** — writing `r` instead of `r + 1`, which silently drops the **last element
of every range**.

**Pairs with** — [Prefix Sums](#prefix-sums), its exact inverse.

**Say this:** *"Store the gaps, so a whole range costs two writes. Then add up to
rebuild."*

Deep dive: [03-DifferenceArray/](01-ArraysAndHashing/03-DifferenceArray/) · [↑ Contents](#contents)

---

## Kadane

> The largest contiguous sum, found **without ever building a subarray**.

**The sentence it answers** — *"which contiguous stretch has the biggest sum?"*

**The shape**

```
maxEnding = Math.Max(arr[i], maxEnding + arr[i]);   // best ending HERE
res       = Math.Max(res, maxEnding);               // best ending ANYWHERE

seed both from arr[0], loop from i = 1
```

**Cost**

| Op | Big-O | Case |
| --- | --- | --- |
| one pass | O(n) | worst — input shape is irrelevant |
| space | **O(1)** | two numbers |

**Why it works** — a subarray ending at `i` must contain `arr[i]`, so the only decision is
whether to also include what came before. You add `arr[i]` **either way** — the only
difference between the two candidates is whether you also add `maxEnding`. So carry it
when it is positive, drop it when it is negative.

**The rule, both halves:**

| `maxEnding` | what happens | what you drop |
| --- | --- | --- |
| **negative** | restart — `maxEnding = arr[i]` | every subarray starting **before** `i` |
| **positive** | extend — `maxEnding += arr[i]` | every subarray starting **at** `i` |

One family dies either way. The subarrays **ending** at `i` are never dropped — one of
them is the winner.

**The trap** — the test is `maxEnding < 0`, **not** `maxEnding < arr[i]`. With
`maxEnding = 5` and `arr[i] = 10` you still extend, because `15 > 10`. Do the algebra and
`arr[i]` cancels out of the comparison entirely.

**Two variables, not one** — `maxEnding` is allowed to fall; `res` is a high-water mark
and is not.

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| maximum sum of a **contiguous** stretch | gaps allowed → that's DP (House Robber, node 17) |
| any "best running total" with O(1) state | maximum **product** → track the min too, negatives flip |

**Classic bug** — seeding `res = 0`, which silently permits the empty subarray and returns
**0** for an all-negative array instead of its largest element.

**Also** — it is the smallest dynamic program. `maxEnding` is `dp[i]` with the array rolled
into one variable, which is why it appears in node 01 rather than node 17.

**Say this:** *"Keep the best sum ending at the current index. You're adding `arr[i]`
either way — the only choice is whether to also carry the running sum, and you carry it
exactly when it's positive."*

Deep dive: [04-Kadane/](01-ArraysAndHashing/04-Kadane/) · [↑ Contents](#contents)

---

# Reference

## Complexity grid

| Algorithm | Setup | Per operation | Space | Case |
| --- | --- | --- | --- | --- |
| Hash Map Counting | — | O(1) lookup | O(n) | **average** |
| Prefix Sums | O(n) build | **O(1)** read | O(n) | worst |
| Difference Array | O(n) build *(free from zeros)* | **O(1)** write | O(n) | worst |
| Kadane | — | O(1) per element | **O(1)** | worst |

**Prefix sum and difference array are mirror images.** Same range phrase, different verb:

| | the sentence | O(1) | O(n) | data must be |
| --- | --- | --- | --- | --- |
| Prefix Sums | *"**give me** the sum of 1..100"* | the **read** | the build | frozen |
| Difference Array | *"**add 5 to** 1..100"* | the **write** | the rebuild | read only at the end |

Need reads **and** writes interleaved and neither works — that is a Fenwick tree,
O(log n) for both.

## Why both are `n + 1`

| | prefix sum | difference array |
| --- | --- | --- |
| extra slot at the | **front** — index 0 | **back** — index n |
| it exists for the | **query** | **update** |
| is it ever read? | **yes** | **no** |

> One needs a **zero to start from**. The other needs a **bin to throw into**.

## Recognition signals

| The problem says | Reach for |
| --- | --- |
| "has it appeared before", "find the duplicate", "count each" | [Hash Map Counting](#hash-map-counting) |
| "sum of the subarray from l to r", many queries | [Prefix Sums](#prefix-sums) |
| "how many times does X occur between l and r" | [Prefix Sums](#prefix-sums) over a 0/1 array |
| a list of ranges in, a per-element array out | [Difference Array](#difference-array) |
| "add v to every element from l to r", repeatedly | [Difference Array](#difference-array) |
| "maximum sum of a **contiguous** subarray" | [Kadane](#kadane) |
| "best running total", one pass, O(1) state | [Kadane](#kadane) |

## Classic bugs

| Algorithm | The bug | What it looks like |
| --- | --- | --- |
| Hash Map Counting | `Contains` then `Add` | correct, but twice the lookups |
| Hash Map Counting | building the set **before** the pass for a seen-before problem | every element "already seen", including itself |
| Prefix Sums | no leading zero | `l == 0` needs a special case, or reads index −1 |
| Prefix Sums | `int[]` instead of `long[]` | silent overflow — the prefix accumulates, elements don't |
| Difference Array | `diff[r] -= v` instead of `diff[r+1]` | last element of every range silently misses the update |
| Difference Array | reading `diff[i]` as a value | it holds a change, not a value — only the rebuild gives values |
| Kadane | seeding `res = 0` | returns 0 for an all-negative array instead of its largest element |
| Kadane | collapsing `maxEnding` and `res` into one variable | the answer follows the running sum back down |
| Both | running total reads the **input** twice instead of its own previous **output** | diverges from the second element on |

---

## Adding an entry

Keep the shape, in this order, and keep it to one screen:

```
## Name
> one line — what it buys
**The sentence it answers** — the problem phrased the way it gets asked
**The shape**            a code or ascii block, the core in ~5 lines
**Cost**                 table: Op | Big-O | Case     <- the case label is not optional
**Why it works**         the invariant, in one or two sentences
**Use / avoid**          two columns
**Classic bug**          the one that actually bites
**Pairs with**           if another algorithm is its inverse or partner
**Say this:**            the sentence you'd give an interviewer
Deep dive: link · [↑ Contents](#contents)
```

Then add rows to [Complexity grid](#complexity-grid), [Recognition
signals](#recognition-signals) and [Classic bugs](#classic-bugs).
