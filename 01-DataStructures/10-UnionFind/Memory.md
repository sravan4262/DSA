# Union-Find in memory

**A forest of trees with no node objects and no pointers** — one flat `int` array, where
`parent[i]` holds the index of `i`'s parent.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first. This page assumes it.

Everything below is measured by the class next door. From `10-UnionFind/`:
`dotnet run UnionFind.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

```
  index    0    1    2    3    4    5    6
        ┌────┬────┬────┬────┬────┬────┬────┐
  parent│  0 │  0 │  1 │  3 │  3 │  5 │  5 │
        └────┴────┴────┴────┴────┴────┴────┘

  parent[i] == i  means i is a ROOT — the name of its whole group


         0            3            5
         │          ┌─┘ └─┐        └─┐
         1          4     …          6
         │
         2

  three groups: {0,1,2}  {3,4}  {5,6}
```

To ask whether two things are connected, walk each up to its root and compare the roots.
No searching, no traversal, no scanning.

## What it costs

| Array | Size |
| --- | --- |
| `parent` | 4 B × n |
| `rank` | 4 B × n |
| **Total** | **8 bytes per element** |

For a million elements: **~8 MB**, in two contiguous blocks. A node-based forest at 40
bytes each would be 40 MB scattered across a million objects.

This is the same trick a [heap](../07-Heaps/Memory.md) uses — **the tree structure lives
in the numbers, not in pointers.** Contiguous storage also means the walks are
cache-friendly, unlike a real pointer-based tree.

## Where each variable in `UnionFind.cs` actually lives

```csharp
public class UnionFind
{
    public int[] parent;                       // the entire structure
    public int[] rank;                         // upper bound on each tree's height
    public int   groups;
    public bool  useRank = true;               // demo switches
    public bool  usePathCompression = true;
    public long  steps;
}
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `uf` (the local) | local | **stack** | 8 B |
| `parent` / `rank` | fields, references | **heap**, in the `UnionFind` object | 8 B each → two flat `int[]` |
| `groups` | field, value type | **heap** | 4 B |
| `useRank` / `usePathCompression` | fields, `bool` | **heap** | 1 B each + padding |
| `steps` | field | **heap** | 8 B |
| `x`, `root` (walking up) | locals in `Find` | **stack** | 4 B each |
| the elements | inline in the `int[]`s | **heap** | 4 B each |

The wrapper is ~48 bytes and points at exactly **two objects** — the smallest heap
footprint of any structure here.

**There is no node type, and that is the headline.** Compare
[trees](../06-Trees/Memory.md), which needs a 40-byte `TreeNode` class, or
[hash tables](../05-HashTables/Memory.md), which needs an `Entry`. A whole forest of
trees is expressed in `int`s, so a group is identified by an array index rather than by
an object reference — which is why `Find` returns an `int` and comparing two groups is
comparing two numbers.

## The two optimisations, and why they are not optional

Without them, `Find` is O(n) and the structure is worthless.

### 1. Union by rank

Always hang the **shorter** tree under the taller one. Do it the other way and the tree
grows a level on every union, degenerating into a linked list — exactly the failure mode
an [unbalanced BST](../06-Trees/Memory.md) has.

`rank` is an upper bound on height. It only increases when two equal-height trees merge.

### 2. Path compression

After walking `i` up to its root, point **every node on that path directly at the root**:

```
   before Find(4)          after Find(4)
        0                      0
        │                   ┌──┼──┐
        1                   1  2  4
        │
        2
        │
        4
```

The structure **flattens itself as a side effect of being read**. That is unusual and
worth noticing — most structures only change when you write to them.

Measured — `dotnet run UnionFind.cs -- ops`:

```
  parent[ 1 2 3 4 5 6 7 7 ]
  Find(0) without compression    7 hops
  Find(0) with compression       7 hops   (same walk...)
  Find(0) again                  1 hops   (...but now flat)
  parent[ 7 7 7 7 7 7 7 7 ]
```

The first call pays the full walk **and** rewires everything it passed. Every later call
is one hop.

## What the complexity actually is

```
     n      naive    optimised
  1000        999            1
 16000      15999            1
```

Naive is **O(n)**. With both optimisations it is **O(α(n))** — inverse Ackermann, which
is below 5 for any `n` that fits in the observable universe.

So it is *effectively* constant but **genuinely not O(1)**. "Almost constant" is the
honest phrasing, and knowing that distinction is the point of the α.

Note this is **amortised** across a sequence of operations, not a per-call guarantee: one
unlucky `Find` on a not-yet-compressed path can still walk several levels.

## What it deliberately cannot do

- **No un-union.** Merges are one-way; there is no split. Undo needs a different structure
  (or rollback with a union-by-rank journal).
- **No enumeration of a group.** You can ask *"are these two together?"* in α(n), but
  listing everyone in a group means scanning all n and comparing roots.
- **No group sizes** unless you track them yourself (store a `size` array and update it
  on merge — cheap and often worth it).

It answers exactly one question, extremely fast.

## Where it earns its keep

- **Kruskal's MST** — sort edges, add one if its endpoints are in different groups. The
  "already connected" case *is* the cycle check.
- **Cycle detection** in an undirected graph, in one pass.
- **Connected components** without re-running a traversal for every query. A BFS answers
  connectivity in O(V + E) *per question*; union-find answers it in α(n) after an O(E)
  build. If you ask many times, that is the whole ballgame.
- **Dynamic connectivity** — edges arriving over time, where re-traversing constantly
  would be hopeless.

## What .NET actually ships

**Nothing.** No `DisjointSet` in the BCL. It is ~30 lines to write, which is why it shows
up in interviews — and why it is worth being able to produce both optimisations from
memory.

## Summary

| Fact | Consequence |
| --- | --- |
| Forest stored in a flat `int` array | 8 B per element, contiguous, cache-friendly |
| `parent[i] == i` marks a root | group identity is just an index |
| Union by rank | keeps trees shallow; without it they degenerate to a list |
| Path compression | flattens the tree *while reading it* |
| Both together | **O(α(n))** — almost constant, amortised, not O(1) |
| Merges are one-way | no un-union, no group enumeration |
| Answers connectivity directly | beats re-running BFS once you ask more than once |

Compare: [graphs](../09-Graphs/Memory.md) — traversal answers the same question in
O(V + E) per query · [heaps](../07-Heaps/Memory.md) — the other flat-array tree.
