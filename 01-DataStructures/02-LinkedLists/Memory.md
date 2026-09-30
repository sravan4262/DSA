# Linked lists in memory

**The exact opposite of an array.** Nothing is contiguous. Each element is its own
heap object, and the only thing connecting them is a stored address.


> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first — stack vs heap, value vs
> reference types, and what an object header costs. This page assumes it.

Everything below is measured by the class next door. From `02-LinkedLists/`:
`dotnet run LinkedLists.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

```
STACK              HEAP — nodes anywhere, in any order
┌──────────┐
│ head ────┼──►┌──────────────┐      ┌──────────────┐      ┌──────────────┐
└──────────┘   │ hdr  16 B    │  ┌──►│ hdr  16 B    │  ┌──►│ hdr  16 B    │
   0x4a10      │ value  10    │  │   │ value  20    │  │   │ value  30    │
               │ next ────────┼──┘   │ next ────────┼──┘   │ next = null  │
               └──────────────┘      └──────────────┘      └──────────────┘
                   0x4a10                0x9f88                0x2c04
                     └──── note the addresses: no pattern at all ────┘
```

There is no "array" here. `head` knows one address. Everything else is found by
following `next`, one hop at a time.

## The cost of a node

A singly-linked node holding one `int`, on x64:

| Part | Size |
| --- | --- |
| Object header (sync block + method table) | 16 B |
| `next` reference | 8 B |
| `int value` | 4 B |
| Padding to 8-byte alignment | 4 B |
| **Total** | **32 bytes** |

**32 bytes to store 4 bytes of data — 8× overhead.** The same million ints:

| Structure | Memory | Objects allocated |
| --- | --- | --- |
| `int[1_000_000]` | ~4 MB | 1 |
| singly-linked list | ~32 MB | 1,000,000 |
| doubly-linked list | ~40 MB | 1,000,000 |

A doubly-linked node adds a `prev` reference: 40 bytes. That is the price of walking
backwards and of O(1) removal when you already hold the node.

## Where each variable in `LinkedLists.cs` actually lives

Two classes, so two kinds of heap object:

```csharp
public class Node          { public int value; public Node? next; }
public class LinkedLists   { public Node? head; public int count;
                             public long steps; public long allocations; }
```

```
  STACK                        HEAP
┌──────────────┐
│ Main() frame │            ┌─────────────────────────────────┐
│  list ───────┼───────────►│  LinkedLists object    48 bytes │
│  (8 bytes)   │            │  header                   16 B  │
│              │            │  steps                     8 B  │
├──────────────┤            │  allocations               8 B  │
│ Get() frame  │            │  head ──────────┐          8 B  │
│  index  = 5  │            │  count                     4 B  │
│  current ────┼──────┐     │  (padding)                 4 B  │
└──────────────┘      │     └─────────────────┼───────────────┘
                      │                       ▼
                      │     ┌──────────┐  ┌──────────┐  ┌──────────┐
                      └────►│ Node 32B │─►│ Node 32B │─►│ Node 32B │
                            └──────────┘  └──────────┘  └──────────┘
                              scattered anywhere on the heap
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `list` | local | **stack** | 8 B |
| `head` | field, reference | **heap**, in `LinkedLists` | 8 B — address of the first node |
| `count` / `steps` / `allocations` | fields, value types | **heap**, in `LinkedLists` | 4 / 8 / 8 B |
| `value` / `next` | fields | **heap**, inside each `Node` | 4 / 8 B |
| `walk`, `i` | locals in `Get` | **stack** | 8 / 4 B |

**The wrapper is 48 bytes and holds no data at all** — it only knows one address. Every
actual element is a separate 32-byte allocation reached by following `next`. Contrast
[arrays](../01-Arrays/Memory.md), where a similar 48-byte wrapper points at *one* object
containing every element.

`walk` is the important local: it is a **stack** variable that moves across **heap**
objects. Reassigning it moves the pointer, never the data.

## Why traversal is slow despite being O(n)

```
array         ████████████████  one fetch serves 16 ints, prefetcher runs ahead
linked list   █ … █ … █ … █     each node a separate address → cache miss per hop
```

You cannot compute where node 5 is. You must read node 0 to learn node 1's address,
read node 1 to learn node 2's, and so on — a **dependent** chain, so the CPU cannot
prefetch or parallelise. Every hop risks a ~100-cycle miss.

This is the single most important practical fact about linked lists: **identical O(n)
to an array, routinely several times slower.** It is why `List<T>` beats
`LinkedList<T>` for almost every real workload.

## Why there is no indexing

```
array         address(i) = start + i * size        → O(1)
linked list   walk from head, i times              → O(n)
```

Nothing to compute from. `list[5000]` cannot exist meaningfully.

## Why insert and remove are O(1) — with a catch

No shifting. Inserting is just rewiring two references:

```
before   A ──► B ──► C

insert X after A:
         A ──► B ──► C
          ╲          ▲
           ╲──► X ───┘        A.next = X;  X.next = B

after    A ──► X ──► B ──► C
```

Three writes, no matter how long the list. Compare an array, where inserting at the
front moves every element.

**The catch:** that is O(1) **given a reference to the node**. Finding the node is
O(n). So "insert into a linked list is O(1)" is only true mid-traversal or when you
already hold the node — the usual mistake is quoting the O(1) and forgetting the O(n)
search that precedes it.

Where it genuinely wins: splicing during a walk, LRU caches (you hold the node in a
hash map), and any structure needing stable references that survive insertion.

## The single-pointer bugs

```
remove B from A ──► B ──► C
    A.next = B.next;      ✓ correct — B is now unreachable, GC collects it
    B.next = null;        ✗ orphans C and loses the rest of the list
```

Order matters everywhere. Reversing a list needs three pointers (`prev`, `curr`,
`next`) precisely because overwriting `curr.next` destroys the only route onward.

## .NET specifics

- `LinkedList<T>` is **doubly** linked with a sentinel-free circular design; nodes are
  `LinkedListNode<T>` objects you can hold and pass around.
- `LinkedList<T>` has no indexer at all — the API refuses to pretend O(1) access exists.
- Millions of small nodes mean millions of gen-0 objects: real GC pressure, and the
  reason "use a list of structs in an array" is such a common optimisation.

## Array vs linked list

| | Array | Linked list |
| --- | --- | --- |
| Layout | one contiguous block | scattered independent nodes |
| Overhead per int | 4 B | 32 B (40 doubly) |
| Index by position | **O(1)** | O(n) |
| Insert/remove at front | O(n) | **O(1)** |
| Insert/remove given the node | O(n) | **O(1)** |
| Search | O(n) | O(n) |
| Cache behaviour | **excellent** | poor |
| Resizing | copies everything | never needed |

Reach for a linked list when you insert and delete constantly *while holding node
references*, and never index. Otherwise an array wins — usually by a lot.

Compare: [arrays](../01-Arrays/Memory.md) — the opposite layout ·
[stacks](../03-Stacks/Memory.md) · [queues](../04-Queues/Memory.md) — both can be built on
nodes instead · [trees](../06-Trees/Memory.md) — the same node, with a second pointer ·
[hash tables](../05-HashTables/Memory.md) — chains collisions in exactly this way.
