# KMP

**Node:** 03 · Strings · **Needs:** array

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run KMP.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. It is not "find a substring" — `IndexOf` does that. Say what KMP
> guarantees that a naive scan does not.

## 2. Brute force first

**Approach:** try every starting position; on a mismatch, restart one character later.

**Time:** Best O(?) · Worst O(?)  **Space:** O(?)

**Measured:** n = 100,000, m = 200, adversarial input → **19,960,200** comparisons.
KMP does **200,198**.

**What work is repeated:**

> Be specific. The answer is not "comparisons" — name the *pointer* that moves the wrong
> way, and say which characters get read more than once in `ABABDABABC` / `ABABC`.

## 3. The idea

> Two or three sentences, no code. The sentence that makes the code obvious finishes:
> *"when a match of length k fails, the longest live candidate is the longest ______ of
> the pattern that is also a ______ of it"* — and the key property of that fact is that it
> mentions **only the pattern**.

## 4. Visual trace

> Use `text = ABABDABABC`, `pattern = ABABC`. Two tables.

**The lps build** — fill this in from the definition, not from the code:

| i | `P[0..i]` | longest proper prefix that is also a suffix | `lps[i]` |
| --- | --- | --- | --- |
| 0 | `A` | | |
| 1 | `AB` | | |
| 2 | `ABA` | | |
| 3 | `ABAB` | | |
| 4 | `ABABC` | | |

**The search** — show `i` and `j` every step:

```
 i   j   T[i]   P[j]   what happens
 0   0    A      A     match, advance both
 ...
```

> Then answer in one line: **how many rows have the same `i` as the row above?** What is
> happening on those rows?

## 5. Why it is faster

> Three separate things:
>
> **(a)** `i` never decreases. Say what that means in terms of how many times each text
> character is read, and why the brute force cannot promise the same.
>
> **(b)** The lps array is built before the text is read at all. Why is that possible —
> what does the definition of `lps` not depend on?
>
> **(c)** The inner `while` means a single text character can cause several fallbacks. Why
> is the total still O(n)? Count the right thing — the argument has the same shape as
> `MonotonicStack` in node 05.

## 6. Complexity

**Time** — build O(?) · search O(?) · total O(?)  **Space** — O(?)

**Which case am I quoting?** All of these are **worst case** — unlike a hash table, nothing
here depends on the data.

> Write out why the fallback loop does not make this amortised. (Hint: amortised is about
> *spreading an occasional expensive operation*. Is any single operation here expensive?)

## 7. Side by side

**The adversarial input** — `text = AAAA...A`, `pattern = AAA...AB`.
`dotnet run KMP.cs -- compare`

| n | m | brute comparisons | KMP comparisons | ratio |
| --- | --- | --- | --- | --- |
| 1,000 | 10 | 9,910 | 2,008 | 4.9× |
| 10,000 | 50 | 497,550 | 20,048 | 24.8× |
| 50,000 | 100 | 4,990,100 | 100,098 | 49.9× |
| 100,000 | 200 | 19,960,200 | 200,198 | **99.7×** |

**Random text, 26-letter alphabet** — the honest counterweight.

| n | m | brute comparisons | KMP comparisons | ratio |
| --- | --- | --- | --- | --- |
| 1,000 | 10 | 1,034 | 1,042 | **1/1.01** |
| 10,000 | 50 | 10,421 | 10,459 | **1/1.00** |
| 50,000 | 100 | 51,920 | 51,950 | **1/1.00** |
| 100,000 | 200 | 104,087 | 104,102 | **1/1.00** |

> Three things to write about these two tables:
>
> - The first table's ratio is **m/2** at every row, and does not depend on n. Explain why
>   the pattern length sets the gap and the text length does not.
> - **The brute force wins every row of the second table.** Say why random text is its best
>   case, and what kind of text it needs to hit its worst.
> - So what *is* the argument for KMP? State it in one sentence that does not contain the
>   word "faster".

## 8. Implementation

- [x] Written from scratch, no library calls → `KMP.cs`
- [ ] Written again from memory a week later

> Six methods in the file, two wrong on purpose. Fill this in:
>
> | Method | what it does | correct? | if not, the input that breaks it |
> | --- | --- | --- | --- |
> | `BuildLps` | | | |
> | `BuildLpsBroken` | | | |
> | `BuildLpsBrute` | | | |
> | `Search` | | | |
> | `SearchNoOverlap` | | | |
> | `SearchBrute` | | | |

## 9. Unit tests

`dotnet run KMP.cs -- test` — **24/24 passing**

**lps build**
- [x] Single char · no repetition · all same · classic `ABABC`
- [x] `AABAACAABAA` · **`ABCABCC`** · period 3 · nested borders · alternating · long tail

**search**
- [x] No match · at start · at end · in middle · whole string · **pattern longer than text**
- [x] **Overlapping** `AAA` in `AAAAA` · all same · classic · `ABABCABAB` · many matches
- [x] **Near miss** `AAAB` in `AAAAAB` · single-char pattern · repeats with tail

> Three to write about:
>
> - **`ABCABCC`** gives `[0,0,0,1,2,3,0]` correctly and `[0,0,0,1,2,3,3]` with `len--`.
>   Say what false claim the `3` is making about the string.
> - **Overlapping** separates `j = lps[j-1]` from `j = 0` after a match: 3 matches or 1.
>   Which is correct? (Careful — this is a question about the problem statement.)
> - Both bugs pass **every other row** in the suite. What does that tell you about a test
>   list that was written before the bugs were known?

## 10. Benchmark

The adversarial input, with `string.IndexOf` for scale.

| n | m | brute ms | KMP ms | **IndexOf ms** |
| --- | --- | --- | --- | --- |
| 10,000 | 50 | 2.28 | 0.11 | **0.002** |
| 50,000 | 100 | 26.25 | 0.93 | **0.010** |
| 200,000 | 200 | 194.10 | 2.42 | **0.039** |

`dotnet run KMP.cs -- bench`

> **`IndexOf` is about 60× faster than KMP** — on KMP's *best* input, the one picked to
> make the brute force look terrible. Write down why, and what it means for Big-O as a
> predictor of wall-clock time.
>
> Then: given that, name three situations where you would still write KMP rather than call
> `IndexOf`.

## 11. Where is it used?

> `ImplementStrStr`, `RepeatedSubstringPattern` (what does `lps[n-1]` tell you about the
> whole string?), `ShortestPalindrome`, `LongestHappyPrefix` (this one is literally the lps
> array). Worth naming: `grep`, and why a streaming log scanner cannot use the brute force
> at all.

## 12. Recall check

- Define `lps[i]` precisely, including the word **proper** and why it is there. Then say
  why `lps[0]` is always 0.
- On a mismatch you write `j = lps[j-1]`, not `j--`. Give the pattern that proves `j--`
  wrong and say what false claim it ends up making.
- The inner `while` loop can run several times for one text character, yet the search is
  O(n). State the counting argument, and name the other algorithm in this repo whose cost
  is bounded the same way.
