# Graphs in memory

A graph is just **V nodes and E edges**. The only real design decision is *how you store
the edges* — and that choice sets every complexity that follows.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first. This page assumes it.

Everything below is measured by the class next door. From `09-Graphs/`:
`dotnet run Graphs.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The two representations

```
        0 ──── 1
        │      │
        │      │
        2 ──── 3 ──── 4
```

**Adjacency matrix** — a V × V grid:

```
         0  1  2  3  4
      0 [ 0  1  1  0  0 ]
      1 [ 1  0  0  1  0 ]      matrix[a][b] = is there an edge a→b?
      2 [ 1  0  0  1  0 ]
      3 [ 0  1  1  0  1 ]
      4 [ 0  0  0  1  0 ]
```

**Adjacency list** — per node, only its actual neighbours:

```
      0 -> [1, 2]
      1 -> [0, 3]
      2 -> [0, 3]
      3 -> [1, 2, 4]
      4 -> [3]
```

## What each costs

| | Matrix | List |
| --- | --- | --- |
| Size | **V²** always | **V + 2E** |
| `HasEdge(a,b)` | **O(1)** — one read | O(degree) — walk the list |
| Neighbours of `a` | O(V) — scan a row, mostly zeroes | **O(degree)** — exactly what exists |
| Add edge | O(1) | O(1) amortised |
| Cache behaviour | excellent (contiguous rows) | moderate (a small array per node) |

Each edge appears **twice** in an undirected adjacency list — once in each endpoint's
list — which is where the `2E` comes from.

## Where each variable in `Graphs.cs` actually lives

```csharp
public class Graphs
{
    public int[][] adjacency;    // one int[] per vertex
    public int[]   degree;       // how many slots of each row are in use
    public int     vertexCount;
    public long    steps;
}
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `g` (the local) | local | **stack** | 8 B |
| `adjacency` | field, reference | **heap**, in the `Graphs` object | 8 B → an array *of references* |
| `degree` | field, reference | **heap** | 8 B → one flat `int[]` |
| `vertexCount` | field, value type | **heap** | 4 B |
| `steps` | field | **heap** | 8 B |
| each `adjacency[v]` | slot in the outer array | **heap** | 8 B → yet another `int[]` |
| `visited` (inside a traversal) | local, reference | **stack** address → **heap** array | 8 B → V bytes |
| `queue` / `stack` (inside a traversal) | locals | **stack** address → **heap** | 8 B each |

`int[][]` is a **jagged array: V + 1 separate objects**, not one rectangle. That is what
makes the adjacency list V + 2E instead of V² — each row is sized to the vertex it
belongs to. It is also why row-to-row iteration is a pointer hop rather than a stride.

**`visited` is allocated per traversal and dies with it.** It is O(V) *auxiliary* space
that never appears in the structure's own memory footprint — the classic thing to forget
when asked for a traversal's space complexity. The queue or stack alongside it is a
second O(V).

## Density decides, and the numbers are brutal

A social network: 1,000,000 users averaging 100 friends each.

| | Cells / entries | Memory |
| --- | --- | --- |
| Matrix | 1,000,000² = 10¹² | **~1 TB** — impossible |
| List | V + 2E = 201,000,000 | ~800 MB — fine |

Measured on a sparse graph (3 edges per node):

```
     V        E    matrix cells    list entries    matrix/list
   100      300           10000             700            14x
  5000    15000        25000000           35000           714x
```

The gap widens with V because the matrix grows **quadratically** while the list grows
**linearly**. At V=5000 the matrix wastes 99.8% of its cells storing zeroes.

- **Sparse** (E ≈ V) — road networks, social graphs, dependency trees. **Use a list.**
  Almost every real graph is here.
- **Dense** (E ≈ V²) — small complete graphs, distance matrices. A matrix is fine and
  its O(1) edge check is genuinely useful.

## The `visited` array is not optional

Both traversals need one, and it is doing more than avoiding duplicate work:

```csharp
bool[] visited = new bool[vertexCount];   // V bytes — the whole extra cost
```

Without it, **any cycle loops forever**. A graph is not a tree; there is no guarantee you
cannot come back to where you started. That array is what makes traversal O(V + E)
instead of infinite.

## BFS and DFS: same cost, different guarantee

Both are **O(V + E)** — every node visited once, every edge examined once. They differ in
one line:

| | Structure | Explores | Guarantee |
| --- | --- | --- | --- |
| **BFS** | queue | in rings of increasing distance | **shortest path** in an unweighted graph |
| **DFS** | stack | as deep as possible, then backtracks | none |

Because BFS finishes distance 1 before starting distance 2, the *first* time it reaches a
node it used the fewest possible edges. DFS may dive down a long path first and arrive by
a worse route. **If the question says "shortest" or "fewest steps", only BFS answers it.**

Space: BFS holds a whole frontier, which can be O(V) on a wide graph. Recursive DFS holds
O(depth) call frames, which can overflow the ~1 MB stack on a deep one — the usual reason
to write DFS iteratively with an explicit stack.

Mark nodes **on discovery**, not on visit. Marking when you pop lets the same node be
queued several times before it is processed.

## Cycle detection differs by direction

**Undirected** — every edge looks like a 2-cycle, because `a→b` is also `b→a`. You must
ignore the node you arrived *from*:

```csharp
if (neighbour == parent) continue;   // not a cycle, just the edge we walked in on
```

**Directed** — that trick is wrong. You need to know whether a node is on the *current
path*, not merely visited before, which means tracking three states (unvisited /
in-progress / finished) or an explicit recursion-stack set.

Getting this backwards is the classic cycle-detection bug.

## What .NET actually ships

**Nothing.** There is no graph type in the BCL — you build it from `List<int>[]`,
`Dictionary<T, List<T>>`, or plain arrays. That is normal; graph shapes vary too much for
one library type to fit.

For interviews, the fastest representation to write is `List<int>[]` sized by V, or a
`Dictionary<T, List<T>>` when the nodes are not integers.

## Summary

| Fact | Consequence |
| --- | --- |
| Matrix is V² regardless of E | O(1) edge check, unusable when V is large |
| List is V + 2E | linear memory; the default for real, sparse graphs |
| Undirected edges stored twice | list holds 2E entries, not E |
| `visited` array | turns infinite looping into O(V + E) |
| BFS uses a queue | **shortest path**, frontier can be O(V) |
| DFS uses a stack | same cost, no shortest guarantee, O(depth) stack |
| Direction changes cycle detection | undirected skips the parent; directed tracks the path |

Compare: [queues](../04-Queues/Memory.md) — what BFS runs on ·
[stacks](../03-Stacks/Memory.md) — what DFS runs on ·
[union-find](../10-UnionFind/Memory.md) — answers connectivity without traversing at all.
