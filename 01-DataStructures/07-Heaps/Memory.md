# Heaps in memory

**A tree with no pointers.** The parent/child relationship is index arithmetic, so the
whole structure lives in one flat array.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first. This page assumes it.

Everything below is measured by the class next door. From `07-Heaps/`:
`dotnet run Heaps.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

A heap *looks* like a binary tree:

```
                  ┌────┐
                  │ 10 │          always the smallest (a MIN-heap)
                  └─┬──┘
          ┌─────────┴─────────┐
        ┌─▼──┐              ┌─▼──┐
        │ 15 │              │ 20 │
        └─┬──┘              └─┬──┘
     ┌────┴────┐          ┌───┘
   ┌─▼──┐   ┌──▼─┐     ┌──▼─┐
   │ 25 │   │ 30 │     │ 40 │
   └────┘   └────┘     └────┘
```

...but it is stored like this:

```
  index    0     1     2     3     4     5
        ┌────┬────┬────┬────┬────┬────┐
        │ 10 │ 15 │ 20 │ 25 │ 30 │ 40 │
        └────┴────┴────┴────┴────┴────┘
```

The shape is arithmetic:

```
  left child of i   =  2i + 1
  right child of i  =  2i + 2
  parent of i       =  (i - 1) / 2
```

Check it: index 1 holds 15; its children are at 3 and 4, holding 25 and 30; its parent is
at `(1-1)/2 = 0`, holding 10. Correct, with **no pointer ever stored**.

## What it costs

| Structure | Per int | 1,000,000 ints |
| --- | --- | --- |
| Array heap | 4 B | **~4 MB**, one object |
| Node-based tree | 40 B | ~40 MB, one million objects |

Ten times less memory *and* contiguous, so the prefetcher works and sifting walks stay in
cache. Storing a tree in a flat array is the same trick [union-find](../10-UnionFind/Memory.md)
uses — the structure lives in the numbers, not in pointers.

This only works because a heap is a **complete** tree: every level full except the last,
which fills left to right. No gaps means no wasted array slots. Maintaining completeness
is exactly why `Push` appends at the end and `Pop` moves the *last* element to the root.

## Where each variable in `Heaps.cs` actually lives

```csharp
public class Heaps
{
    public int[] items = new int[0];
    public int  count = 0;
    public long steps = 0;
    public long copies = 0;
    public int  resizes = 0;
}
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `h` (the local) | local | **stack** | 8 B |
| `items` | field, reference | **heap**, in the `Heaps` object | 8 B → the buffer |
| `count` / `resizes` | fields, value types | **heap** | 4 B each |
| `steps` / `copies` | fields | **heap** | 8 B each |
| `child`, `parent`, `swap` (while sifting) | locals | **stack** (usually registers) | 4 B each |
| the elements | inline in the `int[]` | **heap** | 4 B each |

Field-for-field **identical to `Stacks.cs`** — a 48-byte wrapper over one `int[]`. Nothing
in this class knows it is a tree.

**That is the point worth pausing on: the tree exists only in the local variables.**
`int parent = (child - 1) / 2` in `Push`, and `2 * i + 1` in `Pop`, are stack arithmetic
that lives for the duration of one sift and then vanishes. The heap object stores no structure at all —
which is why there is nothing to allocate, nothing to chase, and nothing to rebalance.

## The rule, and why it is deliberately weak

**Every parent ≤ its children.** That is all. It says nothing about siblings, and nothing
about left versus right.

Compare a BST, where left < node < right is a total ordering. The heap's rule is much
weaker, and that is the point:

- **Cheap to maintain** — restoring it after a change costs one root-to-leaf path.
- **The minimum is free** — it can only be at index 0.
- **You cannot search** — the rule tells you nothing about where a given value sits, so
  `Contains` is a linear O(n) scan.
- **The array is not sorted** — you know the smallest, but have no idea which element is
  third-smallest without popping.

A heap answers "what is the best one?" instantly and answers nothing else.

## Why the operations cost what they do

| Operation | Cost | Why |
| --- | --- | --- |
| `Peek` | **O(1)** | it is `items[0]`, always |
| `Push` | **O(log n)** | append, then sift **up**; can only travel the height |
| `Pop` | **O(log n)** | move last to root, then sift **down** the height |
| `Contains` | **O(n)** | linear scan — the rule gives no guidance |
| `Heapify` | **O(n)** | see below |
| Space | O(n) | plus spare capacity from doubling |

The height of a complete tree is always ⌊log₂ n⌋ — there is no degenerate case here, and
no rebalancing needed. Completeness guarantees the height, which is why a heap never
suffers the way an unbalanced BST does.

## Why building a heap is O(n), not O(n log n)

The favourite interview question about heaps.

Pushing n items sifts **up**, and any of the n items can travel the full height:

```
n × log n  =  O(n log n)
```

`Heapify` sifts **down**, starting from the last parent and walking backwards. The trick
is that most nodes are near the bottom and cannot move far:

| Level | Node count | Max sift distance |
| --- | --- | --- |
| leaves | n/2 | 0 |
| one above | n/4 | 1 |
| two above | n/8 | 2 |
| … | … | … |

```
n/4 × 1  +  n/8 × 2  +  n/16 × 3  + …   =  n × Σ(k / 2^k)  =  2n  →  O(n)
```

Half the nodes are leaves and do no work at all. The measured per-item cost makes this
plain — run `dotnet run Heaps.cs -- complexity`:

```
    n     n pushes   Heapify    per-item push   per-item heapify
 1000         7987      1982         7.99             1.98
16000       191631     31974        11.98             2.00
```

The push column **creeps up** with n; the heapify column stays **flat**. That is the
difference between O(n log n) and O(n), visible in the numbers.

## What .NET actually ships

`PriorityQueue<TElement,TPriority>` (added in .NET 6) is an array-backed 4-ary min-heap —
four children per node rather than two, which makes the tree shallower and sifting more
cache-friendly.

Two things to know:

- **It is a min-heap.** For a max-heap, negate the priority or supply a reversed comparer.
- **It is not stable.** Equal priorities come out in unspecified order; if you need
  ties broken by insertion order, include a sequence number in the priority.

## Summary

| Fact | Consequence |
| --- | --- |
| Tree stored in a flat array | 4 B per int, contiguous, cache-friendly — 10× better than nodes |
| Shape is index arithmetic | no pointers to store or chase |
| Complete tree | height is always log n — no degenerate case |
| Parent ≤ children only | minimum is O(1); search is O(n) |
| Sift up / sift down | Push and Pop are O(log n) |
| Most nodes are leaves | Heapify is **O(n)**, not O(n log n) |

Compare: [trees](../06-Trees/Memory.md) — same shape, pointers instead of arithmetic ·
[arrays](../01-Arrays/Memory.md) — the storage this is built on ·
[union-find](../10-UnionFind/Memory.md) — the other flat-array forest.
