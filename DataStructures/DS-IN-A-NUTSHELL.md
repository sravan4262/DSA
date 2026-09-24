# DS in a Nutshell

One screen per structure. Every cost is tied to **how the bytes actually sit in memory** —
because complexity is a consequence of layout, not a fact to memorise.

Deep version of any structure: the `Memory.md` in its folder.
Foundation for all of it: [MEMORY-MODEL.md](MEMORY-MODEL.md).

## Contents

| # | Structure | In one line |
| --- | --- | --- |
| 1 | [Array](#array) | a fixed contiguous block — everything else is built on this |
| 2 | [Dynamic Array](#dynamic-array) | array + `count` + doubling |
| 3 | [Linked List](#linked-list) | scattered nodes joined by stored addresses |
| 4 | [Stack](#stack) | an array you may only touch at one end |
| 5 | [Queue](#queue) | a circular buffer — two indices move, data never does |
| 6 | [Hash Table](#hash-table) | an array indexed by content instead of position |
| 7 | [Binary Search Tree](#binary-search-tree) | ordered nodes; every compare discards half |
| 8 | [Heap](#heap) | a tree with no pointers |
| 9 | [Trie](#trie) | the key is the path, not the node |
| 10 | [Graph](#graph) | V nodes, E edges — the layout choice sets everything |
| 11 | [Union-Find](#union-find) | a forest living in one `int[]` |

## Reference

| Section | What it answers |
| --- | --- |
| [Big-O grid](#big-o-grid) | all 11, side by side |
| [Memory grid](#memory-grid) | what each costs per element |
| [Decision table](#decision-table) | "I need X → use Y" |
| [Everything is an array](#everything-is-an-array) | why most structures are the same thing underneath |
| [Restriction buys speed](#restriction-buys-speed) | why Stack and Queue are all-O(1) |
| [Memory hierarchy](#memory-hierarchy) | why Big-O lies about linked structures |
| [Stack vs heap memory](#stack-vs-heap-memory) | where variables live, and recursion limits |
| [Amortised vs average vs worst](#amortised-vs-average-vs-worst) | three different claims |
| [Recognition signals](#recognition-signals) | problem phrase → structure |
| [Structure pairings](#structure-pairings) | problems that need two |
| [Classic bugs](#classic-bugs) | the ones that actually bite |

---

## Array

> A fixed-size contiguous run of bytes. **Every strength and weakness follows from that.**

**Layout** — one heap object; the elements live *inside* it

```
STACK              HEAP
┌──────────┐      ┌──────────┬──────────┬────────┬────┬────┬────┬────┐
│ nums ────┼─────►│ sync blk │ method   │ length │ 10 │ 20 │ 30 │ 40 │
└──────────┘      │   8 B    │ table 8B │   8 B  │ 4B │ 4B │ 4B │ 4B │
  8 bytes         └──────────┴──────────┴────────┴────┴────┴────┴────┘
  an address       └──── 24 B of header ────┘└─── the data, 16 B ───┘
```

**The rule**

| | |
| --- | --- |
| Invariant | same-width elements, adjacent, **no gaps ever** |
| Buys | `address = start + i × size` → indexing is arithmetic, and neighbours arrive free in the same cache line |
| Costs | no gaps allowed → making room means **physically moving** elements |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Get(i)` / `Set(i)` | **O(1)** | worst | one multiply-add; size is irrelevant |
| `IndexOf(x)` | O(n) | worst | insertion order, not sorted — a miss must check all n |
| Iterate | O(n) | worst | but ~16 ints per cache line, so far faster than the O(n) suggests |
| Insert / Delete | — | — | **not possible** — `T[]` is fixed size. That's [Dynamic Array](#dynamic-array) |

**Example**

```
int[] nums = new int[4];     // 24 B header + 16 B data = 40 bytes
nums[2] = 30;                // address = start + 2*4  → one instruction
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| O(1) indexing, any size | fixed size — cannot grow |
| **4 bytes per int** — the floor | O(n) search |
| best cache behaviour of anything | no insert or delete at all |
| one allocation, one GC object | 24 B header wasted on tiny arrays |

**Versus others**

| Compared to | Array **wins** | Array **loses** |
| --- | --- | --- |
| Linked list | 4 B vs 32 B · O(1) index vs O(n) · prefetching works | O(n) insert vs O(1) at a known node |
| `List<T>` | one object, one pointer hop, no 48 B wrapper | cannot grow |
| Hash table | 10× less memory, cache-friendly | O(n) search vs O(1) |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| size is known up front (DP tables, `visited[]`) | the size changes → `List<T>` |
| you index by position | you look things up by value → `Dictionary` |
| it's a hot inner loop | you insert in the middle often |

**.NET and gotchas**

| | |
| --- | --- |
| Type | `int[]`, `T[]`, `int[,]` |
| Gotcha | `Array.Resize` doesn't resize — it allocates a new array and copies |
| Gotcha | arrays ≥ **85,000 bytes** (~21,250 ints) go on the Large Object Heap, which isn't compacted |
| Gotcha | bounds are checked on every access (the JIT often elides it in `for` loops) |

**Say this:** *"Indexing is O(1) because elements are the same width and contiguous, so the address is arithmetic. That same contiguity is why insert and delete are O(n) — there are no gaps, so room has to be physically made."*

Deep dive: [01-Arrays/Memory.md](01-Arrays/Memory.md) · [↑ Contents](#contents)

---

## Dynamic Array

> A fixed array, plus an `int`, plus a rule for what to do when it fills up. **This is `List<T>`.**

**Layout** — two heap objects, not one

```
STACK          HEAP
┌──────┐      ┌───────────────────┐
│ list ┼─────►│ List object  48 B │
└──────┘      │  count = 3        │
              │  items ───────────┼──►┌────┬────┬────┬────┐
              └───────────────────┘   │ 10 │ 20 │ 30 │  · │
                                      └────┴────┴────┴────┘
                                      count=3   capacity=4
```

**The rule**

| | |
| --- | --- |
| Invariant | `count <= items.Length`. Slots past `count` exist but are meaningless |
| Buys | growth without the caller managing buffers |
| Costs | a second object, a second pointer hop, and periodic O(n) copies |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Get` / `Set` | **O(1)** | worst | same arithmetic as an array |
| `Add` | **O(1)** | **amortised** | one write; on full, allocate 2× and copy — reaching n copies ~2n total |
| `Insert(0, x)` | O(n) | worst | everything shifts right — **copy backwards** |
| `RemoveAt(count-1)` | **O(1)** | worst | nothing moves |
| `RemoveAt(0)` | O(n) | worst | everything shifts left — copy forwards |
| `IndexOf` | O(n) | worst | linear scan |

**Example**

```
Add 10,20,30,40,50   capacity:  0 → 4 → 8
                     [10 20 30 40 50 · · ·]   count=5 capacity=8
```

Doubling is the whole trick: a fixed `+4` step would make n adds cost ~n²/8 copies instead of ~2n.

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| O(1) index **and** grows | 48 B wrapper + one extra pointer hop |
| amortised O(1) append | O(n) insert/remove anywhere but the end |
| still contiguous and cache-friendly | transient **2× memory** during a resize |
| | never shrinks on its own |

**Versus others**

| Compared to | `List<T>` **wins** | `List<T>` **loses** |
| --- | --- | --- |
| `T[]` | grows automatically | +48 B, +1 pointer hop, resize pauses |
| Linked list | 4 B vs 32 B · O(1) index · cache locality | O(n) insert at the front vs O(1) |
| `Stack`/`Queue` | full random access | those guarantee O(1) on *every* operation |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| the size grows as you go | the size is known → `T[]` |
| you append and index | you insert at the front often → `Queue`/`LinkedList` |

**.NET and gotchas**

| | |
| --- | --- |
| Type | `List<T>` — doubling from capacity 4 |
| Gotcha | `new List<T>(10_000)` skips **every** resize when you know the size |
| Gotcha | `Clear()` resets `count` but **keeps** the buffer |
| Gotcha | removing from a `List<string>` must null the slot, or the object leaks |

**Say this:** *"`List<T>` is an array plus a count. Append is amortised O(1) because it doubles — individual adds can cost O(n), but doubling makes resizes exponentially rarer."*

Deep dive: [01-Arrays/Memory.md](01-Arrays/Memory.md) · [↑ Contents](#contents)

---

## Linked List

> **The exact opposite of an array.** Nothing is contiguous; the only thing joining elements is a stored address.

**Layout**

```
head ──►┌────────────┐   ┌────────────┐   ┌────────────┐
        │ hdr  16 B  │ ┌►│ hdr  16 B  │ ┌►│ hdr  16 B  │
        │ value  10  │ │ │ value  20  │ │ │ value  30  │
        │ next ──────┼─┘ │ next ──────┼─┘ │ next = null│
        └────────────┘   └────────────┘   └────────────┘
          0x4a10           0x9f88           0x2c04
          └──── addresses have no relationship at all ────┘
```

**The rule**

| | |
| --- | --- |
| Invariant | each node stores the address of the next (and `prev`, if doubly) |
| Buys | insert/remove at a **known node** is a couple of pointer writes — nothing shifts |
| Costs | no arithmetic can find node *i* → you must walk. And every hop is a cache miss |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Get(i)` | **O(n)** | worst | no address arithmetic possible — walk from `head` |
| `Search` | O(n) | worst | same walk |
| Insert at head | **O(1)** | worst | allocate, point at old head |
| Insert **at a known node** | **O(1)** | worst | two pointer writes, no shifting |
| Insert at position *i* | O(n) | worst | O(n) to walk + O(1) to splice |
| Delete at a known node | **O(1)** doubly · **O(n)** singly | worst | singly needs the *previous* node, so you walk |

**Example**

```
insert 15 between 10 and 20:

10 ──► 20          10 ──► 15 ──► 20
       ▲                   two pointer writes. No element moved.
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| O(1) insert/remove at a held node | **32 B per int** (40 B doubly) — 8–10× an array |
| grows with no resize pause | O(n) to reach anything |
| no 2× memory spike | **worst cache behaviour of any structure** |
| splitting/joining lists is cheap | one GC object per element |

**Versus others**

| Compared to | Linked list **wins** | Linked list **loses** |
| --- | --- | --- |
| Array / `List<T>` | O(1) insert at a held node · no resize pause | 8× memory · O(n) index · pointer chasing |
| `Queue<T>` | never resizes (good for hard real-time) | 8× memory, far slower in practice |
| Degenerate BST | 32 B vs 40 B, no wasted compare | — *(a degenerate BST is strictly worse)* |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| you already hold the node and splice often | you need indexing or search — that's almost always |
| implementing an LRU cache (with a Dictionary) | you just want a sequence → `List<T>` |
| a queue must never pause to resize | **most of the time** |

**.NET and gotchas**

| | |
| --- | --- |
| Type | `LinkedList<T>` — doubly linked, 40 B per node |
| Gotcha | O(1) insert only if you hold the `LinkedListNode<T>`. Finding it is O(n) |
| Gotcha | it is the standard deque substitute, since .NET has no `Deque<T>` |
| Reality | rarely the right answer — its O(1) insert is usually beaten by an array's cache locality |

**Say this:** *"Insert at a known node is O(1) because nothing shifts. But getting to that node is O(n), and each hop is a dependent load the prefetcher can't predict — so at identical O(n) it loses badly to an array."*

Deep dive: [02-LinkedLists/Memory.md](02-LinkedLists/Memory.md) · [↑ Contents](#contents)

---

## Stack

> An array you may only touch at one end. **LIFO** — last in, first out.

**Layout** — same fields as a dynamic array

```
 [10][20][30][ · ][ · ]        count = 3
           ▲
         top = items[count-1]

 count doubles as "how many" AND "where to write next"
```

**The rule**

| | |
| --- | --- |
| Invariant | only `items[count-1]` is reachable |
| Buys | all work happens at the array's **cheap end** → every operation O(1) |
| Costs | no indexing, no search, no ordering |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Push` | O(1) | **amortised** | one write at the end; resize doubles |
| `Pop` | **O(1)** | **worst** | one read + `count--`; removal never reallocates |
| `Peek` | **O(1)** | worst | `items[count-1]` — also the most recently touched, so usually in L1 |
| `Contains` | O(n) | worst | offered, but a sign you chose the wrong structure |

**Example**

```
Push 10, 20, 30   →  [10 20 30]   top = 30
Pop               →  [10 20]      returns 30
Peek              →  returns 20
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **every** operation O(1) | no indexing |
| 4 B per int, contiguous | no search |
| nothing ever shifts | no ordering, no iteration you should rely on |
| top is almost always cache-resident | |

**Versus others**

| Compared to | Stack **wins** | Stack **loses** |
| --- | --- | --- |
| `List<T>` | guarantees O(1) — no operation *can* be slow | no random access |
| Queue | simpler, one end only | wrong order if you need FIFO |
| Recursion | moves O(h) frames from the 1 MB stack to the multi-GB heap | more code than recursing |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| undo/redo, backtracking, DFS | you need FIFO → `Queue` |
| matching brackets, expression parsing | you need to look inside |
| converting deep recursion to a loop | |

**.NET and gotchas**

| | |
| --- | --- |
| Type | `Stack<T>` — array-backed, doubling |
| Gotcha | `Pop()`/`Peek()` **throw** on empty — use `TryPop`/`TryPeek` |
| Gotcha | iteration order is top-to-bottom, but don't design around it |

**Say this:** *"A stack is an array with the O(n) operations removed from the API. You only ever touch the end, which is the one place an array is cheap — so everything is O(1)."*

Deep dive: [03-Stacks/Memory.md](03-Stacks/Memory.md) · [↑ Contents](#contents)

---

## Queue

> A circular buffer. **Two indices move; the data never does.** FIFO — first in, first out.

**Layout**

```
┌────┬────┬────┬────┬────┬────┬────┬────┐
│ 60 │ 70 │  · │  · │ 30 │ 40 │ 50 │  · │
└────┴────┴────┴────┴────┴────┴────┴────┘
            ▲              ▲
          tail           head
     (next to insert) (next to remove)

 logical order: 30 40 50 60 70 — it simply wraps past the end
```

**The rule**

| | |
| --- | --- |
| Invariant | `head` = next to remove, `tail` = next to insert, both wrap with `% length` |
| Buys | removing from the front costs **nothing to shift** — an index moves instead |
| Costs | `head == tail` is ambiguous (empty *or* full), so `count` is **required for correctness** |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Enqueue` | O(1) | **amortised** | write at `tail`, `tail = (tail+1) % len` |
| `Dequeue` | **O(1)** | **worst** | read at `head`, `head = (head+1) % len` — nothing shifts |
| `Peek` | **O(1)** | worst | `items[head]` |
| `Count` | **O(1)** | worst | a stored field — every BFS loop depends on this |
| `Contains` | O(n) | worst | use a `HashSet` alongside instead |

**Example**

```
capacity 4:  Enqueue 10,20,30      [10 20 30 ·]  head=0 tail=3
             Dequeue → 10          [ · 20 30 ·]  head=1
             Enqueue 40, 50        [50 20 30 40] head=1 tail=1  ← wrapped
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **both ends O(1)** — the hard part | no indexing, no search |
| 4 B per int, contiguous | resize must *unwrap* the ring, not block-copy |
| the naive array version would be O(n²) | 56 B wrapper — the heaviest array-backed one |

**Versus others**

| Compared to | Queue **wins** | Queue **loses** |
| --- | --- | --- |
| `List<T>` | `RemoveAt(0)` is O(1) instead of **O(n)** | no random access |
| Linked-list queue | 8× less memory, cache-friendly | occasional O(n) resize pause |
| Stack | correct order for BFS / fairness | wrong order for backtracking |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| **BFS / shortest path in an unweighted graph** | you need LIFO → `Stack` |
| level-order tree traversal | you need priority order → `PriorityQueue` |
| producer/consumer, fair scheduling | you need to search it |

**.NET and gotchas**

| | |
| --- | --- |
| Type | `Queue<T>` — circular array with `_head`, `_tail`, `_size` |
| Gotcha | `Dequeue()` **throws** on empty — use `TryDequeue` |
| Gotcha | **there is no `Deque<T>` in .NET** — use `LinkedList<T>` and say why |
| Gotcha | snapshot `queue.Count` *before* draining a BFS level — it grows as you go |

**Say this:** *"A queue is a circular buffer. Dequeue is O(1) because `head` moves instead of the data shifting — eight bytes of bookkeeping buys a whole complexity class."*

Deep dive: [04-Queues/Memory.md](04-Queues/Memory.md) · [↑ Contents](#contents)

---

## Hash Table

> **An array you index by content instead of position.** One arithmetic step turns a key into a slot, so the search disappears.

**Layout**

```
 "apple" ──► hash 9834721 ──► % 8 ──► bucket 1

 ┌───┬───┬───┬───┬───┬───┬───┬───┐   the bucket array holds only REFERENCES
 │ · │ ● │ · │ ● │ · │ · │ ● │ · │
 └───┴─┼─┴───┴─┼─┴───┴───┴─┼─┴───┘
       ▼       ▼           ▼
   ["apple" 3]["pear" 9]["fig" 1]
        │
        ▼  ["grape" 7]     ← a COLLISION, chained
```

**The rule**

| | |
| --- | --- |
| Invariant | a key's slot is `hash(key) % bucketCount` — **position is computed from the value** |
| Buys | no searching at all; find and prove-absent are both ~1 comparison |
| Costs | hashing **destroys order by design** (scattering is what keeps chains short) → no sorting, no ranges, ever |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Add` | O(1) | **average** | hash, then append to one short chain |
| `ContainsKey` / lookup | **O(1)** | **average** | hash → bucket → walk a ~1-long chain |
| `Remove` | **O(1)** | **average** | unlink from the chain — **nothing shifts**, positions are absolute |
| any of the above | **O(n)** | **worst** | if every key collides it degenerates into one linked list |
| `ContainsValue` | **O(n)** | worst | **only keys are hashed** — this is a full scan |
| Sorted iteration | O(n log n) | — | you must collect and sort; there is no order to read |

**Example**

```
find "fish" in 1,000,000 entries:
  hash → bucket 2 → compare 1 key → not found.     ~1 comparison

same in an array:                                  1,000,000 comparisons
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **O(1) average** lookup, insert, delete | ~40 B per entry + the key object |
| absence is as cheap as presence | 3–4 cache misses per lookup |
| removal never shifts anything | **no order at all** — no sort, no range, no min/max |
| | O(n) worst case; a bad hash or an attacker can trigger it |

**Versus others**

| Compared to | Hash table **wins** | Hash table **loses** |
| --- | --- | --- |
| Array | O(1) search vs O(n) | 10× memory · no indexing · poor cache behaviour |
| **BST** | O(1) vs O(log n) · ~1 hop vs ~20 | **no sorted order, no ranges, no min/max, no floor/ceiling** |
| Small array (n < ~50) | — | a linear scan often **wins** — cache beats the hash + pointer chase |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| "have I seen this?" · counting · dedupe | you need sorted output or ranges → `SortedSet` |
| key → value by **exact** key | n is tiny — just scan the array |
| turning an O(n²) nested loop into O(n) | you need min/max — that's O(n) here |

**.NET and gotchas**

| | |
| --- | --- |
| Types | `Dictionary<K,V>` · `HashSet<T>` — same machinery, `HashSet` just drops the value |
| Implementation | **two parallel arrays** with index chains, not linked nodes — far better cache behaviour |
| Gotcha | `dict[k]` **throws** if missing · `Add` **throws** on a duplicate key · `dict[k] = v` overwrites silently |
| Gotcha | use `TryGetValue` — `ContainsKey` + indexer is **two** lookups |
| Gotcha | `HashSet.Add` returns `bool` — "check and mark" in one lookup |
| Gotcha | **never rely on iteration order**, and never persist a hash code (it's randomised per process) |

**Say this:** *"Lookup is O(1) average because the key computes its own slot. It's O(n) worst case if everything collides — and it can never give you sorted order, because scattering keys is the mechanism, not a side effect."*

Deep dive: [05-HashTables/Memory.md](05-HashTables/Memory.md) · [↑ Contents](#contents)

---

## Binary Search Tree

> Scattered nodes with two pointers and an ordering rule, so **every comparison throws away half the remaining data.**

**Layout**

```
                50
           ┌────┴────┐          each node: 16 B header + left 8 + right 8
          30         70                    + value 4 + padding 4 = 40 B
        ┌─┴─┐      ┌─┴─┐
       20   40    60   80       allocated anywhere — addresses unrelated
```

**The rule**

| | |
| --- | --- |
| Invariant | everything in the **left subtree** is smaller, everything **right** is larger |
| Buys | binary search on a structure you can insert into **without shifting** |
| Costs | the shape depends on insertion order — **sorted input degenerates it into a linked list** |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| Search | **O(log n)** | balanced | one compare discards half the tree |
| Search | **O(n)** | **degenerate** | sorted input → one long spine |
| Insert | O(log n) | balanced | same walk, then hang off a leaf — **nothing shifts** |
| Delete | O(log n) | balanced | 3 cases; two children → replace with the in-order successor |
| Min / Max | O(log n) | balanced | walk left / right until you can't |
| **Range query** | **O(log n + k)** | balanced | descend to `lo`, walk in-order to `hi` |
| In-order walk | O(n) time, **O(h) space** | — | sorted output free; the recursion stack is *not* O(1) |

**Example**

```
find 40:  40 < 50 → left  (discard 70,60,80)
          40 > 30 → right (discard 20)
          40 = 40 → found            3 compares for 7 nodes

1,000,000 nodes → ~20 compares.   A miss costs the same as a hit.
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **sorted order, ranges, min/max, floor/ceiling** | 40 B per int — 10× an array |
| insert without shifting (unlike a sorted array) | ~20 dependent pointer hops → poor cache behaviour |
| a miss costs the same as a hit | **plain BST degenerates to O(n) on sorted input** |
| in-order walk is sorted for free | slower per lookup than a hash table |

**Versus others**

| Compared to | BST **wins** | BST **loses** |
| --- | --- | --- |
| **Hash table** | **sorted order, ranges, min/max** — things a hash table cannot do at any price | O(log n) vs O(1) · 20 cache misses vs ~1 |
| Sorted array | O(log n) insert vs **O(n)** shifting | no O(1) indexing, 10× memory, worse cache |
| Heap | full ordering, search, ranges | 40 B vs 4 B · O(log n) min vs **O(1)** · can degenerate |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| you need `<`, `>`, `ORDER BY`, or "nearest" | you only ever look up exact keys → `Dictionary` |
| ranges: "all keys between a and b" | you only need the min/max → `PriorityQueue` |
| you need min **and** max **and** ordering | |

**.NET and gotchas**

| | |
| --- | --- |
| Types | `SortedDictionary<K,V>` · `SortedSet<T>` — **red-black trees**, so O(log n) is guaranteed |
| Gotcha | `SortedList<K,V>` is **not a tree** — two sorted arrays. O(log n) read, **O(n) insert** |
| Gotcha | **there is no plain BST in .NET**, precisely because of degeneration |
| Gotcha | recursive traversal is O(h) stack space — a degenerate tree overflows at ~16,000 nodes |

**Say this:** *"Every operation walks one root-to-leaf path, so the cost is the height — O(log n) balanced, O(n) degenerate. Sorted input produces the worst possible tree, which is why everyone ships a red-black tree instead."*

Deep dive: [06-Trees/Memory.md](06-Trees/Memory.md) · [↑ Contents](#contents)

---

## Heap

> **A tree with no pointers.** The parent/child relationship is index arithmetic, so the whole tree lives in one flat array.

**Layout**

```
        10              index  0    1    2    3    4    5
      ╱    ╲                 ┌────┬────┬────┬────┬────┬────┐
    15      20               │ 10 │ 15 │ 20 │ 25 │ 30 │ 40 │
   ╱  ╲    ╱                 └────┴────┴────┴────┴────┴────┘
  25  30  40
                  left = 2i+1    right = 2i+2    parent = (i-1)/2
```

**The rule**

| | |
| --- | --- |
| Invariant | **every parent ≤ its children** — nothing about siblings, nothing about left vs right |
| Buys | the minimum can only be at index 0 → O(1); a weak rule is cheap to restore |
| Costs | the rule gives no guidance about *where* anything else is → search is O(n), and the array is **not sorted** |
| Also requires | the tree is **complete** (no gaps) — which is why height is always log n, with no degenerate case |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Peek` | **O(1)** | worst | it's `items[0]`, always |
| `Push` | O(log n) | amortised | append at the end, then **sift up** — at most the height |
| `Pop` | O(log n) | worst | move the **last** element to the root (keeps it packed), then **sift down** |
| `Contains` | **O(n)** | worst | linear scan — the rule tells you nothing |
| `Heapify` (build from n) | **O(n)** | worst | **half the nodes are leaves and can't move at all** |

**Example**

```
Push 5 into [10,20,40,30]:
  append          [10,20,40,30,5]
  parent(4)=1 →20, 5<20 swap    [10,5,40,30,20]
  parent(1)=0 →10, 5<10 swap    [5,10,40,30,20]     2 swaps = the height
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **4 B per int** — a tree at an array's price | **cannot search** (O(n)) |
| no pointers to store or chase; cache-friendly | **not sorted** — you know the min and nothing else |
| **no degenerate case** — completeness forces height = log n | no ranges, no floor/ceiling, no ordered walk |
| building from n items is **O(n)**, not O(n log n) | |

**Versus others**

| Compared to | Heap **wins** | Heap **loses** |
| --- | --- | --- |
| Unsorted array | min in **O(1)** vs O(n) | insert O(log n) vs O(1) |
| Sorted array | insert O(log n) vs **O(n)** shifting | no sorted output, no binary search |
| **BST** | **4 B vs 40 B** · cache-friendly · **no degenerate case** | can't search, no ranges, no sorted walk |
| Hash table | knows the minimum | lookup O(n) vs O(1) |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| **top-k / kth largest** — O(n log k), O(k) memory | you want everything sorted → `Array.Sort` |
| the data is a **stream** you can't store | you need the min exactly once → just scan, O(n) |
| Dijkstra, A*, task scheduling, median stream | you need to search or look up by key |
| merging k sorted lists | k ≈ n — the advantage vanishes |

**.NET and gotchas**

| | |
| --- | --- |
| Type | `PriorityQueue<TElement,TPriority>` (.NET 6+) — an array-backed **4-ary** min-heap |
| Gotcha | it is a **min-heap**. For max-heap, negate the priority or pass a reversed comparer |
| Gotcha | **for the k *largest*, use a MIN-heap of size k** — the root is the weakest champion, the one to evict |
| Gotcha | **not stable** — equal priorities come out in unspecified order |
| Gotcha | **no DecreaseKey** — for Dijkstra, push duplicates and skip stale entries on pop |

**Say this:** *"A heap is a complete binary tree stored in a flat array — children at 2i+1 and 2i+2, so the structure costs nothing to store. It maintains only 'parent ≤ child', which is the weakest rule that still guarantees the root is the minimum."*

Deep dive: [07-Heaps/Memory.md](07-Heaps/Memory.md) · [↑ Contents](#contents)

---

## Trie

> **The letters aren't stored in the nodes.** The letter is the *edge* you take; a node's position in the tree is what identifies it.

**Layout** — insert `car`, `cart`, `cat`, `dog`

```
        (root)                     node:  16 B header
        /     \                           + 8 B children ref ──► 26 refs = 232 B
       c       d                          + 8 B isEndOfWord + padding
       │       │                          ────────────────────────────
       a       o                          ~264 bytes for ONE letter
     /   \     │
    r     t*   g*        * = a word ends here
    │
    t*                   `cart` reuses every node of `car`
```

**The rule**

| | |
| --- | --- |
| Invariant | each edge is one character; the path from the root spells the key |
| Buys | lookup costs **O(L)** — the key's length, **independent of how many keys exist** |
| Costs | ~264 B per node, nearly all of it `null` slots; `isEndOfWord` is needed because `car` is a prefix of `cart` |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Insert(word)` | **O(L)** | worst | one hop per character |
| `Search(word)` | **O(L)** | worst | walk the path, then check `isEndOfWord` |
| `StartsWith(prefix)` | **O(L)** | worst | walk the path — **the thing nothing else can do** |
| All words with a prefix | O(L + output) | worst | walk to the node, then collect the subtree |
| Space | O(total characters) | — | shared prefixes are stored **once** |

**Example**

```
Search "cat":  root →c →a →t, isEndOfWord = true → found.   3 hops
               costs the same whether the trie holds 10 words or 10 million
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **O(L), independent of n** | **~264 B per node** — the most memory-hungry structure here |
| **prefix search** — autocomplete | poor cache behaviour (scattered nodes) |
| shared prefixes stored once | only works for sequence keys (strings) |
| alphabetical iteration for free | overkill unless you need prefixes |

**Versus others**

| Compared to | Trie **wins** | Trie **loses** |
| --- | --- | --- |
| **Hash table** | **prefix queries** · no collisions · sorted iteration | ~264 B vs ~40 B per entry · O(L) vs O(1) for exact lookup |
| BST of strings | O(L) vs O(L log n) — no repeated string comparisons | far more memory |
| Sorted array + binary search | O(L) vs O(L log n) | memory, and it needs building |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| autocomplete, prefix matching | exact-match lookup only → `Dictionary` |
| word search / boggle solvers | memory is tight |
| IP routing, dictionary problems | keys aren't sequences |

**.NET and gotchas**

| | |
| --- | --- |
| Type | **doesn't exist** — you write it. That's why it's a standard interview exercise |
| Gotcha | `isEndOfWord` is essential — without it you can't tell `car` from a prefix of `cart` |
| Gotcha | a 26-slot array per node is mostly `null` — use `Dictionary<char,Node>` when sparse |
| Gotcha | a **radix tree** compresses single-child chains into one edge — big win on long unique suffixes |

**Say this:** *"A trie stores the key in the path, not in the nodes, so lookup is O(L) — independent of how many keys there are. That's what makes prefix search possible, and it's why it costs so much memory."*

Deep dive: [08-Tries/Memory.md](08-Tries/Memory.md) · [↑ Contents](#contents)

---

## Graph

> **V nodes and E edges.** The only real design decision is *how you store the edges* — and that choice sets every complexity that follows.

**Layout** — two ways to store the same graph

```
   0 ── 1        ADJACENCY MATRIX            ADJACENCY LIST
   │    │            0  1  2  3  4
   2 ── 3 ── 4    0 [ 0  1  1  0  0 ]        0 -> [1, 2]
                  1 [ 1  0  0  1  0 ]        1 -> [0, 3]
                  2 [ 1  0  0  1  0 ]        2 -> [0, 3]
                  3 [ 0  1  1  0  1 ]        3 -> [1, 2, 4]
                  4 [ 0  0  0  1  0 ]        4 -> [3]

                   V² always                  V + 2E
```

**The rule**

| | |
| --- | --- |
| Invariant | there isn't one — a graph is the general case (a tree is a graph with no cycles and V−1 edges) |
| The choice | **matrix** = O(1) edge lookup, V² memory · **list** = O(degree) lookup, V+2E memory |
| Rule of thumb | dense graph → matrix · **sparse graph → list** (almost always the right answer) |

**Operations**

| Op | Matrix | List | Why |
| --- | --- | --- | --- |
| `HasEdge(a,b)` | **O(1)** | O(degree) | one read vs walking a's neighbours |
| Neighbours of `a` | O(V) | **O(degree)** | scanning a mostly-zero row vs exactly what exists |
| Add edge | O(1) | O(1) amortised | |
| **BFS / DFS** | O(V²) | **O(V + E)** | you visit each node and each edge once |
| Space | **V²** | **V + 2E** | each undirected edge appears twice in a list |

**Example**

```
BFS from 0:   queue [0]              visited {0}
              pop 0 → push 1,2       visited {0,1,2}
              pop 1 → push 3         visited {0,1,2,3}
              pop 2 → (3 seen)
              pop 3 → push 4         visited {0,1,2,3,4}

shortest path in an UNWEIGHTED graph falls straight out of this
```

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| models anything with relationships | traversal needs a **visited set** or you loop forever |
| BFS gives unweighted shortest paths | memory: V² (matrix) or many small arrays (list) |
| DFS gives cycles, components, topological order | BFS space is O(width) — can be ~V/2 |

**Versus others**

| Compared to | Graph **wins** | Graph **loses** |
| --- | --- | --- |
| Tree | allows cycles, multiple parents, disconnected parts | needs a visited set; no ordering to exploit |
| **Union-Find** | gives **paths**, not just "are they connected?" | ~O(1) vs O(V+E) for pure connectivity questions |
| Matrix vs list | matrix: O(1) `HasEdge` · list: **V+2E** memory | matrix wastes V² on sparse graphs |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| shortest path, connectivity, dependencies | you only need "same group?" → **Union-Find** |
| social networks, maps, scheduling, routing | the data is hierarchical with one parent → a tree |
| topological sort (build order, course prereqs) | |

**.NET and gotchas**

| | |
| --- | --- |
| Type | **none** — build it. `List<int>[]` or `Dictionary<T, List<T>>` |
| Gotcha | **always** track visited — a cycle without it is an infinite loop |
| Gotcha | BFS = `Queue` (shortest path) · DFS = `Stack` or recursion (cycles, topological order) |
| Gotcha | weighted shortest path needs **Dijkstra** (a heap), not plain BFS |
| Gotcha | recursive DFS on a long path can overflow the stack — go iterative |

**Say this:** *"Adjacency list is V+2E and traverses in O(V+E); a matrix is V² but answers `HasEdge` in O(1). Real graphs are sparse, so the list wins almost always."*

Deep dive: [09-Graphs/Memory.md](09-Graphs/Memory.md) · [↑ Contents](#contents)

---

## Union-Find

> **A forest of trees with no node objects and no pointers** — one flat `int[]` where `parent[i]` holds the index of `i`'s parent.

**Layout**

```
  index    0    1    2    3    4    5    6
        ┌────┬────┬────┬────┬────┬────┬────┐
  parent│  0 │  0 │  1 │  3 │  3 │  5 │  5 │    parent[i] == i  means i is a ROOT
        └────┴────┴────┴────┴────┴────┴────┘

     0        3        5           three groups: {0,1,2} {3,4} {5,6}
     │       ╱ ╲        ╲
     1      4   …        6
     │
     2
```

**The rule**

| | |
| --- | --- |
| Invariant | every element points at a parent; the **root names the whole group** |
| Buys | "same group?" = walk both to their roots and compare — no traversal, no search |
| Requires | **path compression** + **union by rank**, or the trees grow tall and it degrades to O(n) |

**Operations**

| Op | Big-O | Case | Why |
| --- | --- | --- | --- |
| `Find(x)` | **~O(1)** | amortised α(n) | walk to the root; path compression flattens as it goes |
| `Union(a,b)` | **~O(1)** | amortised α(n) | two finds, then point the smaller root at the larger |
| `Connected(a,b)` | **~O(1)** | amortised | `Find(a) == Find(b)` |
| Without the optimisations | **O(n)** | worst | trees grow into long chains |
| Space | O(n) | — | **8 bytes per element** (`parent` + `rank`) |

**Example**

```
Union(1,2)  → 2's root now points at 1's root
Connected(0,2)? Find(0)=0, Find(2)=0 → yes
Connected(0,5)? Find(0)=0, Find(5)=5 → no
```

α(n) is the inverse Ackermann function — **below 5 for any n you will ever have**. Effectively constant.

**Strengths and weaknesses**

| ✅ | ❌ |
| --- | --- |
| **~O(1)** connectivity queries | **cannot un-union** — merges are permanent |
| **8 B per element**, two contiguous arrays | tells you *whether* connected, **never the path** |
| no pointers, cache-friendly | no way to list a group's members without scanning |
| trivial to implement | useless for anything but grouping |

**Versus others**

| Compared to | Union-Find **wins** | Union-Find **loses** |
| --- | --- | --- |
| **Graph + BFS/DFS** | ~O(1) per query vs **O(V+E)**; handles edges arriving over time | gives no path, no distance, no traversal order |
| Node-based forest | 8 B vs 40 B, contiguous, no allocations | — |
| Hash table of group IDs | merging groups is O(1), not O(group size) | can't enumerate a group |

**Use / avoid**

| Reach for it when | Don't when |
| --- | --- |
| "are these two connected?" asked repeatedly | you need the actual path → BFS |
| **Kruskal's** minimum spanning tree | you need to split groups apart |
| cycle detection in an undirected graph | you need to list group members |
| counting connected components / islands | |

**.NET and gotchas**

| | |
| --- | --- |
| Type | **none** — write it. ~20 lines, a common interview exercise |
| Gotcha | **without path compression + union by rank it is O(n)**, not O(1) |
| Gotcha | compare **roots**, never raw values — `parent[a] == parent[b]` is wrong |
| Gotcha | there is no undo. If you need rollback, use a different approach |

**Say this:** *"Union-Find keeps a forest in a flat int array — `parent[i]` is an index, not a pointer. With path compression and union by rank both operations are effectively O(1), which beats re-running BFS for every connectivity query."*

Deep dive: [10-UnionFind/Memory.md](10-UnionFind/Memory.md) · [↑ Contents](#contents)

---
---

# Reference

## Big-O grid

Every cell names its case. `*` = amortised.

| Structure | Access by index | Search | Insert | Delete | Space |
| --- | --- | --- | --- | --- | --- |
| [Array](#array) | **O(1)** | O(n) | — *(fixed)* | — *(fixed)* | O(n) |
| [Dynamic Array](#dynamic-array) | **O(1)** | O(n) | **O(1)\*** end · O(n) mid | **O(1)** end · O(n) mid | O(n) |
| [Linked List](#linked-list) | O(n) | O(n) | **O(1)** at a held node | **O(1)** doubly · O(n) singly | O(n) |
| [Stack](#stack) | — | O(n) | **O(1)\*** | **O(1)** | O(n) |
| [Queue](#queue) | — | O(n) | **O(1)\*** | **O(1)** | O(n) |
| [Hash Table](#hash-table) | — | **O(1)** avg · O(n) worst | **O(1)** avg | **O(1)** avg | O(n) |
| [BST](#binary-search-tree) *balanced* | — | O(log n) | O(log n) | O(log n) | O(n) |
| [BST](#binary-search-tree) *degenerate* | — | **O(n)** | O(n) | O(n) | O(n) |
| [Heap](#heap) | — | **O(n)** | O(log n) | O(log n) *(min only)* | O(n) |
| [Trie](#trie) | — | **O(L)** | O(L) | O(L) | O(total chars) |
| [Graph](#graph) *adj list* | — | O(V+E) traverse | O(1) edge | O(degree) | O(V+2E) |
| [Union-Find](#union-find) | — | **~O(1)** | **~O(1)** union | — | O(n) |

<sub>L = key length, independent of n · α(n) < 5 for any real n</sub>

[↑ Contents](#contents)

## Memory grid

| Structure | Bytes per `int` | 1M ints | Objects allocated | Cache |
| --- | --- | --- | --- | --- |
| Array · Stack · Queue · **Heap** | **4 B** | **4 MB** | **1** | ✅ excellent |
| Union-Find | 8 B | 8 MB | 2 | ✅ excellent |
| Dynamic Array | 4 B + spare | ~4–8 MB | 2 | ✅ excellent |
| Linked list (singly) | 32 B | 32 MB | 1,000,000 | ❌ poor |
| Linked list (doubly) · BST node | 40 B | 40 MB | 1,000,000 | ❌ poor |
| Hash entry | ~40 B + key | ~40 MB | many | ❌ poor |
| **Trie node** | **~264 B** | — | many | ❌ poor |

Wrapper objects (the class that holds the buffer): `List`/`Stack`/`Heap` 48 B · `Queue` 56 B · `Trees` 48 B.

[↑ Contents](#contents)

## Decision table

| I need to… | Use | Cost |
| --- | --- | --- |
| get the item at position `i` | `int[]` / `List<T>` | O(1) |
| "have I seen this before?" | `HashSet<T>` | O(1) avg |
| map key → value | `Dictionary<K,V>` | O(1) avg |
| count occurrences | `Dictionary<T,int>` | O(1) avg |
| process most-recent-first | `Stack<T>` | O(1) |
| process in arrival order | `Queue<T>` | O(1) |
| repeatedly get the min or max | `PriorityQueue` | O(1) peek |
| **top-k / kth largest** | `PriorityQueue` size k | O(n log k) |
| sorted order · ranges · min **and** max | `SortedSet` / `SortedDictionary` | O(log n) |
| search by **prefix** | Trie *(build it)* | O(L) |
| shortest path, unweighted | Graph + **BFS** | O(V+E) |
| shortest path, weighted | Graph + Dijkstra (heap) | O(E log V) |
| "are these two connected?" | Union-Find | ~O(1) |
| insert/remove at a node you hold | `LinkedList<T>` | O(1) |

[↑ Contents](#contents)

## Everything is an array

Six of the eleven bottom out in one contiguous block. Only three genuinely use scattered nodes.

| Structure | Storage | The rule laid on top |
| --- | --- | --- |
| Dynamic Array | **array** | just `count` |
| Stack | **array** | only touch index `count-1` |
| Queue | **array** | `head`/`tail` indices, wrap with `%` |
| Hash Table | **array** | index = `hash(key) % buckets` |
| Heap | **array** | parent ≤ children; children at `2i+1`, `2i+2` |
| Union-Find | **array** | `parent[i]` is an index, not a pointer |
| — | — | — |
| Linked List · BST · Trie | **scattered nodes** | genuinely pointer-based — 32–264 B each |

A data structure is **storage + invariant + operations**. The storage is usually an array;
the *invariant* is what makes it a different structure.

[↑ Contents](#contents)

## Restriction buys speed

| Operation | Array | Stack | Queue |
| --- | --- | --- | --- |
| add at the cheap end | O(1)* | **O(1)\*** | **O(1)\*** |
| add at front / middle | **O(n)** | ❌ not offered | ❌ not offered |
| remove at the cheap end | O(1) | **O(1)** | **O(1)** |
| remove at front / middle | **O(n)** | ❌ | ❌ |
| index / search | O(1) / O(n) | ❌ | ❌ |

**Every ❌ sits on a row that's O(n) for arrays.** Stacks and queues aren't faster than
arrays — they're arrays with the slow operations **removed from the API**.

The queue is the one with real cleverness: removing from the front is an array's worst
operation, and it makes it O(1) by moving an index instead of the data.

[↑ Contents](#contents)

## Memory hierarchy

| Level | Where | Latency | If L1 were 1 second |
| --- | --- | --- | --- |
| L1 cache | inside the core | ~1 ns | 1 second |
| L2 | inside the core | ~4 ns | 4 seconds |
| L3 | on the chip, shared | ~15 ns | 15 seconds |
| **RAM** | separate chips | **~80 ns** | **1.5 minutes** |
| SSD | a drive | ~100 µs | 1.5 days |

Two mechanisms decide whether your structure is fast:

| | What it does | Who benefits |
| --- | --- | --- |
| **Cache line** | never fetches less than **64 bytes** → 16 ints arrive together | contiguous structures |
| **Prefetcher** | spots a constant stride and fetches ahead | sequential walks |

**Pointer chasing defeats both** — the next address is unknown until the current load
finishes, so nothing can be predicted and the stalls can't overlap.

> Big-O counts operations. It cannot see that one costs 4 cycles and another costs 250.
> That's why an `int[]` beats a linked list several times over at identical O(n).

[↑ Contents](#contents)

## Stack vs heap memory

| | Stack | Heap |
| --- | --- | --- |
| Size | **~1 MB per thread** (shared by all frames) | limited by RAM |
| Holds | frames: parameters, locals, return address | every `class`, array and `string` |
| Freed | automatically on return | by the GC, when nothing points at it |
| Overflow | `StackOverflowException` — **uncatchable, kills the process** | `OutOfMemoryException` — catchable |

**The rule everyone gets wrong:** *"value types go on the stack"* is false.
**A value type goes wherever its owner goes.** An `int` field of a class lives on the
heap, inside that object. The **declaration site** decides, not the type.

| Declaration | Lives | Size |
| --- | --- | --- |
| `int x` — local | stack frame | 4 B |
| `int x` — **field of a class** | **heap**, inside the object | 4 B |
| `int[] a` — local | 8 B address on stack → object on heap | 8 B + 24 B + 4n |
| `string s` — local | 8 B address on stack → object on heap | 8 B + object |

**Recursion:** a frame is ~64 bytes, so you get roughly **16,000 calls** before overflow.

| Tree | Height | Recursion |
| --- | --- | --- |
| Balanced, 1 billion nodes | ~30 | ✅ trivially safe |
| Degenerate, 20,000 nodes | 20,000 | 💥 crash |

Converting recursion to an explicit `Stack<T>` keeps the space at O(h) but moves it to the
heap, where you have gigabytes instead of one megabyte. **Iterative is not O(1) space.**

[↑ Contents](#contents)

## Amortised vs average vs worst

Three different claims. Saying the wrong one is exactly what gets probed.

| Claim | Means | Example |
| --- | --- | --- |
| **Worst case** | never slower than this | `Pop`, `Dequeue` — O(1), no hidden path |
| **Average case** | typical, but a bad case exists | hash lookup — O(1) average, **O(n)** if all keys collide |
| **Amortised** | any single call can be slow; n calls average out | `Add`/`Push`/`Enqueue` — O(1) amortised, O(n) on the resize |

Two more worth naming:

- **Insertion-order dependent** — a BST is O(log n) on random input, **O(n) on sorted input**
- **Space including the call stack** — a recursive traversal is O(h) space, not O(1)

[↑ Contents](#contents)

## Recognition signals

| The problem says… | Reach for |
| --- | --- |
| "top k" · "kth largest" · "most frequent" · "closest" | **Heap** (size k) |
| "have I seen this" · "duplicate" · "unique" | **HashSet** |
| "count" · "frequency" · "group by" | **Dictionary** |
| "pairs that sum to" · turn O(n²) into O(n) | **Dictionary** |
| "shortest path" · "fewest steps" · "level by level" | **Queue / BFS** |
| "all paths" · "cycle" · "backtracking" · "build order" | **Stack / DFS** |
| "matching brackets" · "undo" · "previous smaller/greater" | **Stack** |
| "prefix" · "autocomplete" · "starts with" | **Trie** |
| "sorted order" · "range" · "next larger" · "floor/ceiling" | **SortedSet / BST** |
| "are these connected" · "number of islands/groups" | **Union-Find** |
| "stream" · "online" · "can't store it all" | **Heap** (fixed size) |
| "sliding window maximum" | **Deque** → `LinkedList<T>` in .NET |

[↑ Contents](#contents)

## Structure pairings

Most real problems need two.

| Problem | Combination | Why |
| --- | --- | --- |
| **BFS** | `Queue` + `HashSet` | queue keeps order, set answers "seen?" in O(1) |
| Top-k frequent | `Dictionary` + `PriorityQueue` | dictionary counts, heap selects |
| Median from a stream | **two** heaps (max + min) | the median sits between their roots |
| **LRU cache** | `Dictionary` + doubly linked list | dictionary finds in O(1), list reorders in O(1) |
| Dijkstra | Graph + `PriorityQueue` | always expand the nearest unvisited node |
| Group anagrams | `Dictionary<string, List<string>>` | sorted word as key |
| Sliding window | `Queue`/`Deque` + `Dictionary` | window order + counts |

**The trap:** using `queue.Contains(x)` instead of a `HashSet` turns BFS from
O(V+E) into O(V·E).

[↑ Contents](#contents)

## Classic bugs

| Bug | What happens | Fix |
| --- | --- | --- |
| Insert copies **forwards** | one value smeared across the array | insert copies **backwards**, remove copies **forwards** |
| Not blanking the vacated slot | the array keeps a live reference past `count` → **memory leak** | `items[count] = null` on remove |
| `head == tail` used for empty | a full queue reports empty, or you overwrite live data | store `count` — it's the **only** thing that disambiguates |
| Sift down swaps with the **larger** child | heap invariant breaks on the other branch | always swap with the **smaller** child (min-heap) |
| Not snapshotting `queue.Count` | BFS runs past the level boundary | `int levelSize = queue.Count;` **before** the inner loop |
| `ContainsKey` then indexer | two hash lookups | `TryGetValue` |
| Max-heap for "k largest" | you can't find the weakest champion to evict | use a **min**-heap of size k |
| Recursive DFS on a deep graph | uncatchable `StackOverflowException` | explicit `Stack<T>` |
| No visited set in graph traversal | infinite loop on any cycle | `HashSet` — and use `Add`'s return value |
| Union-Find without path compression | O(n) instead of ~O(1) | path compression **and** union by rank |

[↑ Contents](#contents)
