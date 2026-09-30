# Binary search trees in memory

**Scattered nodes like a linked list, but each holds two pointers and obeys an ordering
rule** — so every comparison throws away half the remaining data.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first. This page assumes it.

Everything below is measured by the class next door. From `06-Trees/`:
`dotnet run Trees.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

```
                    ┌──────────────┐
                    │ hdr    16 B  │
                    │ value    50  │
                    │ left  ──┐    │
                    │ right ──┼─┐  │
                    └─────────┼─┼──┘
                ┌─────────────┘ └─────────────┐
                ▼                             ▼
       ┌──────────────┐               ┌──────────────┐
       │ value    30  │               │ value    70  │
       │ left  · right│               │ left  · right│
       └──────────────┘               └──────────────┘
          0x9f88                         0x2c04
          └──── the addresses have no relationship ────┘
```

Every node is its own heap object, allocated anywhere. Exactly like a linked list, with
one extra reference.

## What one node costs

| Part | Size |
| --- | --- |
| Object header | 16 B |
| `left` reference | 8 B |
| `right` reference | 8 B |
| `int value` | 4 B |
| Padding | 4 B |
| **Total** | **40 bytes** to store 4 bytes of data |

A million ints: **4 MB** as an `int[]`, **40 MB** as a BST, plus a million objects for
the GC to track. You are paying 10× for ordered data you can insert into cheaply.

## Where each variable in `Trees.cs` actually lives

```csharp
public class TreeNode { public int value; public TreeNode? left; public TreeNode? right; }
public class Trees    { public TreeNode? root; public int count;
                        public long steps; public long allocations; }
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `t` (the local) | local | **stack** | 8 B |
| `root` | field, reference | **heap**, in the `Trees` object | 8 B → the top node, or `null` |
| `count` | field, value type | **heap** | 4 B |
| `steps` / `allocations` | fields | **heap** | 8 B each |
| `value` / `left` / `right` | fields of `TreeNode` | **heap**, inside each node | 4 / 8 / 8 B |
| `walk` (in `Contains`) | local, reference | **stack** address → **heap** node | 8 B |
| `stack` / `queue` (in `InOrder`, `Height`) | locals, references | **stack** address → **heap** array | 8 B → O(n) |
| recursion frames | one per level *if* you recurse | **stack** | O(h) — see below |

The `Trees` wrapper is 48 bytes and stores no data; like a linked list it knows exactly
one address. Everything else is 40-byte nodes scattered across the heap.

**The part people miss is the last row.** A recursive walk allocates nothing on the
heap, yet it is not O(1) space — each level in flight is a stack frame holding
parameters and a return address, and on a degenerate tree that is n frames against a
~1 MB limit.

**Nothing in `Trees.cs` recurses.** `InOrder` carries an explicit `TreeNode?[]` stack and
`Height` an explicit queue; `Insert`, `Contains` and `Remove` are plain loops. That is
deliberate — it moves the O(h) space onto the heap where it is both visible in the code
and effectively unbounded.

## The ordering rule, and what it buys

```
                  50
             ┌────┴────┐
            30         70
          ┌─┴─┐      ┌─┴─┐
         20   40    60   80
```

Everything left of a node is smaller; everything right is larger. Comparing against 50
tells you which **half** of the tree to discard — you never look at the other side.

That is binary search, but on a structure you can insert into without shifting. Keeping
a *sorted array* sorted costs O(n) per insert because everything after the insertion
point moves. Here you just hang a new node off a leaf. **That is what the 40 bytes buy.**

## Why the complexity is the height

Every operation walks one root-to-leaf path, so every cost is the tree's **height**:

| Shape | Height | Search |
| --- | --- | --- |
| Balanced | log₂(n) | **O(log n)** |
| Degenerate | n | **O(n)** |

And the shape depends entirely on **insertion order**:

```
insert 50,30,70,20,40,60,80        insert 10,20,30,40,50,60,70

          50                        10
     ┌────┴────┐                      └20
    30         70                        └30
  ┌─┴─┐      ┌─┴─┐                          └40
 20   40    60   80                            └50 …

 height 3  →  O(log n)             height n  →  O(n), a linked list
```

Same values, same class, same code. **Sorted input — the most likely input in real
life — produces the worst possible tree.** That is why nobody ships a plain BST, and why
self-balancing variants (AVL, red-black) exist: they rotate on insert to keep the height
at log n regardless of order.

A miss costs the same as a hit here — you walk to a leaf either way. Contrast an array,
where proving absence means touching all n.

## Cache behaviour

As bad as a linked list, for the same reason: each hop is a dependent read of a
separately-allocated object, so the CPU cannot prefetch. A balanced BST does ~log n hops
where a hash table does ~1, and each hop risks a ~100-cycle miss.

This is why a **heap**, which stores its tree in a flat array with no pointers at all, is
dramatically faster per operation despite being the same shape on paper.

## In-order traversal, and the space nobody counts

Left, self, right yields **sorted output for free** — the ordering rule cashed in. A hash
table cannot do this at any price.

The cost is O(n) time and **O(h) space for the recursion stack**. On a balanced tree
that is log n frames; on a degenerate one it is n frames, which can overflow the ~1 MB
call stack. Forgetting to count recursion frames is the single most common complexity
mistake — the traversal allocates nothing on the heap but is not O(1) space.

## Deleting: three genuinely different cases

1. **Leaf** — detach it.
2. **One child** — promote the child.
3. **Two children** — you cannot detach it without orphaning two subtrees. Replace its
   value with the **in-order successor** (the smallest value in the right subtree — the
   only value that keeps the ordering rule true), then delete *that* node, which has at
   most one child and reduces to case 1 or 2.

Case 3 is what interviews probe.

## What .NET actually ships

- `SortedDictionary<K,V>` and `SortedSet<T>` are **red-black trees** — self-balancing, so
  O(log n) guaranteed rather than hoped for.
- `SortedList<K,V>` is *not* a tree; it is two sorted arrays. O(log n) lookup by binary
  search but **O(n) insert** because of shifting. Faster to read, slower to write, and
  much more compact.
- There is no plain BST in the BCL, precisely because of the degeneration problem.

## Summary

| Fact | Consequence |
| --- | --- |
| Nodes scattered, 2 pointers each | 40 B per int; pointer chasing, poor cache behaviour |
| Ordering rule | each comparison discards half → O(log n) *when balanced* |
| Height determines everything | degenerate tree → O(n), no better than a list |
| Shape depends on insertion order | sorted input is the worst case → use a balancing tree |
| Insert hangs off a leaf | no shifting, unlike a sorted array's O(n) insert |
| In-order walk | sorted output free, but **O(h) stack space** |

Compare: [linked lists](../02-LinkedLists/Memory.md) — a degenerate BST *is* one ·
[hash tables](../05-HashTables/Memory.md) — faster lookup, no ordering ·
[heaps](../07-Heaps/Memory.md) — same tree shape, stored in a flat array instead.
