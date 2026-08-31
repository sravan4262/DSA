# <Algorithm name>

**Node:** <roadmap node> · **Needs:** <data structures it depends on>

> Copy this file, fill every section, delete these quote lines. Write it in your own
> words — re-explaining is the exercise, copying teaches nothing. A section you cannot
> fill is the thing you have not understood yet.

## 1. Purpose

> One sentence. What job does this do?

## 2. Brute force first

> Never skip this. The optimisation is meaningless without the thing it beats, and
> "what's the naive approach?" is how most interviews open.

**Approach:**

**Time:** O(?)  **Space:** O(?)

**Measured:** n = 1,000 → ___ steps

**What work is repeated:**

> This line is the hinge. Almost every optimisation is caching a repeat, sorting to
> expose structure, or moving a pointer instead of restarting a scan.

## 3. The idea

> Two or three sentences, no code. If you cannot say it without code, go back.

## 4. Visual trace

> Draw one small input all the way through. This is the section you will actually
> remember in three months.

```
input:  [ ... ]

step 1  ...
step 2  ...

result: [ ... ]
```

## 5. Why it is faster

> Name the *specific* work the brute force did that this avoids. Not "it's smarter" —
> exactly which comparisons or scans disappear, and why they are safe to skip.

## 6. Complexity

**Time** — Best: O(?) · Average: O(?) · Worst: O(?)

**Why?**

> Derive it. "Every level processes n elements, and there are log₂(n) levels."

**Space** — O(?)

**Why?**

> Include the recursion stack. A recursive traversal is O(h) space even if it
> allocates nothing.

**Which case am I quoting?** *worst* / *average* / *amortised* are three different
claims and this is exactly where follow-up questions land.

## 7. Side by side

| n | brute force | this algorithm | ratio |
| --- | --- | --- | --- |
| 100 | | | |
| 1,000 | | | |
| 10,000 | | | |

> Run `dotnet run <Algorithm>.cs -- compare` and paste the real numbers.

## 8. Implementation

- [ ] Written from scratch, no library calls → `<Algorithm>.cs`
- [ ] Written again from memory a week later

## 9. Unit tests

- [ ] Empty input
- [ ] Single element
- [ ] Two elements
- [ ] Already in the answer state
- [ ] Reverse / worst case
- [ ] Duplicates
- [ ] Negative numbers
- [ ] Maximum constraint size

> `dotnet run <Algorithm>.cs -- test`

## 10. Benchmark

| n | steps | ms |
| --- | --- | --- |
| 100 | | |
| 1,000 | | |
| 10,000 | | |

> `dotnet run <Algorithm>.cs -- bench`
> Steps are deterministic — reason with those. Milliseconds are noisy — they make it real.

## 11. Where is it used?

> Real systems, .NET BCL, and which problems it unlocks.

## 12. Recall check

> Three questions you must answer cold. If you can't, you are not finished.

- Why is the complexity ___ and not ___?
- What breaks if ___?
- When would you *not* use this?
