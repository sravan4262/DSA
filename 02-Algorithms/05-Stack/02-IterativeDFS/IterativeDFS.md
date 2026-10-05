# IterativeDFS

**Node:** 05 · Stack · **Needs:** array, stack

> Sections 7 and 10 are filled in — those numbers come from running the code, not from
> understanding it. Everything else is yours to write. Re-explaining is the exercise.
> `dotnet run IterativeDFS.cs -- trace` first, then write section 3 without looking.

## 1. Purpose

> One sentence. It is not "traverse a tree" — node 10 does that. Say what replacing the
> call stack with your own stack is *for*.

## 2. The recursive version first

**Approach:** three methods that differ by one line.

```
void Pre(n)  { emit(n); Pre(n.Left); Pre(n.Right);          }
void In(n)   {          In(n.Left);  emit(n); In(n.Right);  }
void Post(n) {          Post(n.Left); Post(n.Right); emit(n); }
```

**Time:** O(n) · **Space:** O(?) — and the question that matters: O(n) or O(depth)?

**Measured:** a trivial method recursed **22,439** levels before the runtime reported it
was out of room. A left chain of 100,000 nodes got through 22,439 of them and stopped.

**What the recursion is spending that space on:**

> Three things live in a frame. Name them. One of the three is invisible in the source
> code, and it is the one that matters here.

## 3. The idea

> Two or three sentences, no code. The sentence that makes the code obvious finishes:
> *"re-pushing `(n, phase + 1)` before descending is the ______"*.

## 4. Visual trace

> Use the trace tree. Show the stack as `value@phase`, top first, and mark the row where
> each value is emitted.

```
        1
      /   \
     2     3
    / \     \
   4   5     6

 popped   does what                     stack after      output
 1@0      re-push as 1, descend left 2  [2@0,1@1]
 2@0      ...
```

**Fill in the three orders from the same tree:**

| | output |
| --- | --- |
| pre-order | |
| in-order | |
| post-order | |

## 5. Why the phase is the whole trick

> Three separate things:
>
> **(a)** There are three phases because there are three ______ in the recursive body.
> Say what, and match each phase to a position in the code.
>
> **(b)** `emitPhase` 0, 1, 2 gives pre, in, post from **one** loop. Why does the emit
> position in the recursive source map onto a phase number so exactly?
>
> **(c)** The specialised pre-order needs **no** phase at all. What is true of pre-order
> that is not true of the other two? (Your answer should also explain why its peak stack
> on a chain is **1**, not n.)

## 6. Complexity

**Time** — O(?) for every version here. **Space** — O(?), and say in which *memory region*.

**Which case am I quoting?** All of these are **worst case** — every node is visited
exactly once no matter the shape, and nothing resizes in a way that needs spreading out.

> Write out why this is *not* amortised and *not* average. Then: pushes per node is **n**
> for the specialised pre-order and **3n** for the phase machine. Does that change the O?
> Does it change the runtime? (Section 10 answers the second one.)

## 7. Side by side

**Space is the whole story here** — the step counts are identical by construction, so the
interesting column is peak stack depth.

**A left chain, so depth == n.** `dotnet run IterativeDFS.cs -- compare`

| n | recursive | nodes done | iter pre-order | phase machine |
| --- | --- | --- | --- | --- |
| 1,000 | completed | 1,000 | peak **1** | peak 1,000 |
| 10,000 | completed | 10,000 | peak **1** | peak 10,000 |
| 100,000 | **RAN OUT** | 22,439 | peak **1** | peak 100,000 |
| 1,000,000 | **RAN OUT** | 22,439 | peak **1** | peak 1,000,000 |

**The same node counts, balanced.** Depth is log2(n) now.

| nodes | depth | recursive | iter pre-order | phase machine |
| --- | --- | --- | --- | --- |
| 1,023 | 10 | completed | peak 10 | peak 10 |
| 16,383 | 14 | completed | peak 14 | peak 14 |
| 262,143 | 18 | completed | peak 18 | peak 18 |
| 1,048,575 | 20 | completed | peak 20 | peak 20 |

> Three things to write about these two tables:
>
> - A million nodes finishes recursively in the second table but 100,000 fails in the
>   first. One number explains both rows — which?
> - "nodes done" reads 22,439 twice. Why does the bigger input not get *further*?
> - `iter pre-order` peaks at **1** on a chain while recursion holds n frames on the same
>   input. The recursive frames are alive for a reason — what are they waiting to do?
>   (This has a name, and node 09 is where it gets its own write-up.)

## 8. Implementation

- [x] Written from scratch, no library calls → `IterativeDFS.cs`
- [ ] Written again from memory a week later

> Six methods in the file. Fill in what each one is for and what it costs:
>
> | Method | pushes per node | orders it can produce | can it stream output? |
> | --- | --- | --- | --- |
> | `Iterative(root, emitPhase)` | | | |
> | `PreorderIterative` | | | |
> | `InorderSpine` | | | |
> | `PostorderTwoStacks` | | | |
>
> `InorderSpine` and `PostorderTwoStacks` are the two "tricks" that get memorised. Write
> one line each on how they are the phase machine with the bookkeeping removed.

## 9. Unit tests

`dotnet run IterativeDFS.cs -- test` — **14/14 passing**

- [x] Empty · single node · left child only · right child only · both children
- [x] Left chain of 4 · right chain of 4 · **zigzag**
- [x] **Symmetric tree** · full depth 3 · the trace tree
- [x] Negatives · duplicates · deep left 1,000

> Two to write about:
>
> - **symmetric tree** is in the list as the case that *cannot* catch the push-order bug.
>   Say why it is still worth having a test you know is not evidence.
> - **zigzag** is the only row where all three orders differ from each other. What would
>   stay hidden if the suite were all chains and full trees?

## 10. Benchmark

Balanced trees, pre-order, all three versions visiting every node once.

| nodes | recursive ms | iter pre-order ms | phase machine ms |
| --- | --- | --- | --- |
| 32,767 | 0.60 | 0.79 | 2.08 |
| 262,143 | 7.08 | 6.98 | 22.25 |
| 1,048,575 | **21.39** | 26.80 | 81.63 |

`dotnet run IterativeDFS.cs -- bench`

> **Recursion wins.** Write down what that does to the sentence "rewrite it iteratively to
> make it faster". Then say what the rewrite actually buys, in one line, and name a
> concrete input where that is worth 4× the time.
>
> Note the 262,143 row, where the two are within 2% and the order flips between runs.
> Why is it fair to quote the million-node row and not that one?

## 11. Where is it used?

> `BinaryTreeInorderTraversal`, `BinaryTreePostorderTraversal`, `NumberOfIslands` (grid
> flood-fill — why does this one *need* the explicit stack?), `CourseSchedule`,
> `DecodeString`, `BasicCalculator`. Worth naming: a recursive descent parser on
> untrusted input — what is the attack, and which line in `IterativeDFS.cs` is the
> defence?

## 12. Recall check

- A frame holds three things. Name all three, and say which one people leave out of their
  hand-rolled stack and what breaks as a result.
- Pre-order iterative is 4 lines and post-order iterative is not. Explain the difference
  **from the recursive source**, not from the stack code.
- `EnsureSufficientExecutionStack` vs letting it overflow: state what is different about
  the two exceptions, in the one way that decides whether you can do anything about it.
