# Arrays in memory

**One contiguous run of bytes.** Every strength and weakness follows from that.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first — stack vs heap, value vs
> reference types, and what an object header costs. This page assumes it.

Everything below is measured by the class next door. From `01-Arrays/`:
`dotnet run Arrays.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

```
STACK (your variable)            HEAP (the object it points at)
┌──────────────┐
│ items        │        ┌──────────┬──────────┬────────┬────┬────┬────┬────┐
│ 0x7f9a2c00 ──┼───────►│ sync blk │ method   │ length │ 10 │ 20 │ 30 │ 40 │
└──────────────┘        │ 8 bytes  │ table 8B │ 4, 8B  │ 4B │ 4B │ 4B │ 4B │
  8 bytes               └──────────┴──────────┴────────┴────┴────┴────┴────┘
  just an address        └──── 24 bytes of header ────┘└─── the elements ───┘
```

The variable holds an address. The array is **one object**, and the elements live
*inside* it — not allocated separately, not scattered.

| Part | Size (x64) | Purpose |
| --- | --- | --- |
| Sync block index | 8 B | locking, hash code — sits *behind* the reference |
| Method table pointer | 8 B | the type: how the runtime knows this is an `int[]` |
| Length | 8 B | the bounds checked on every access |
| Elements | n × sizeof(T) | the data |

So `new int[4]` costs **40 bytes**, not 16. For tiny arrays the header dominates —
which is why "an array of many small arrays" wastes so much.

## Where each variable in `Arrays.cs` actually lives

The class in this folder is five fields. Here is every one of them, and where it sits.

```csharp
public class Arrays
{
    public int[] items = new int[0];   // reference type
    public int  count  = 0;            // value type
    public long steps  = 0;            // value type
    public long copies = 0;            // value type
    public int  resizes = 0;           // value type
}
```

```csharp
Arrays a = new Arrays();
a.Add(10);
```

```
  STACK                          HEAP
┌────────────────┐
│ Main() frame   │             ┌──────────────────────────────────────┐
│                │             │  Arrays object            48 bytes   │
│  a  ───────────┼────────────►├──────────────────────────────────────┤
│  (8 bytes,     │             │  sync block index            8 B     │
│   an address)  │             │  method table pointer        8 B     │
│                │             │  steps    = 1                8 B     │
├────────────────┤             │  copies   = 0                8 B     │
│ Add() frame    │             │  items ──────────────┐       8 B     │
│  value = 10    │             │  count    = 1                4 B     │
│  i     = 0     │             │  resizes  = 1                4 B     │
│  bigger ───────┼──────┐      └──────────────────────┼───────────────┘
└────────────────┘      │                             │
                        │      ┌──────────────────────▼───────────────┐
                        └─────►│  int[4] object            40 bytes   │
                               ├──────────────────────────────────────┤
                               │  sync block index            8 B     │
                               │  method table pointer        8 B     │
                               │  length = 4                  8 B     │
                               │  [0]=10  [1]=0  [2]=0  [3]=0  16 B   │
                               └──────────────────────────────────────┘
```

| Variable | Kind | Lives | Size | Notes |
| --- | --- | --- | --- | --- |
| `a` | local | **stack** | 8 B | just an address |
| `items` | field, reference type | **heap**, in the `Arrays` object | 8 B | an address pointing at a *second* heap object |
| `count` | field, value type | **heap**, in the `Arrays` object | 4 B | an `int` that never touches the stack |
| `steps` | field, value type | **heap**, in the `Arrays` object | 8 B | |
| `copies` | field, value type | **heap**, in the `Arrays` object | 8 B | |
| `resizes` | field, value type | **heap**, in the `Arrays` object | 4 B | |
| `value` | parameter | **stack**, in `Add`'s frame | 4 B | copied in, dies on return |
| `i` | loop local | **stack** (usually a register) | 4 B | |
| `bigger` | local, reference type | **stack** address → **heap** array | 8 B | during a resize, two arrays are alive |
| the elements | inside the `int[]` object | **heap** | 4 B each | contiguous, inline |

### Three things worth pausing on

**`count` is an `int` on the heap.** "Value types live on the stack" is the
oversimplification that trips people. `count` is a field of `Arrays`, so it lives inside
the `Arrays` object, on the heap. Only the *local* `int i` inside `Add` is on the stack.
The declaration site decides, not the type.

**There are two objects, not one.** `Arrays` and its `int[]` are separate heap
allocations. `a` points at the `Arrays` object; the `items` field inside it points at the
array. So `a.items[0]` is:

```
read a           (stack)          → address of Arrays object
read items       (heap, +offset)  → address of int[] object
compute start + 0 * 4             → address of element 0
read                              → 10
```

Two pointer hops, then arithmetic. The hops are why the *wrapper* costs something; the
arithmetic is why the *indexing* is O(1) regardless of size.

**Field order in memory is not declaration order.** The runtime packs fields to avoid
padding — the two `long`s and the reference (8 B each) get grouped, then the two `int`s
(4 B each) pair up to fill a slot. Declared order would leave gaps. Total: 16 B header +
32 B of fields = **48 bytes** for the wrapper, before a single element is stored.

Which is the real lesson: a `List<T>` holding four ints costs 48 + 40 = **88 bytes** to
store 16 bytes of data. Fine for one list, ruinous for a million tiny ones.

## Why indexing is O(1)

Same width, adjacent — so the location is arithmetic, not a search:

```
address(i) = start + i * sizeof(T)

address(0) = 0x7f9a2c18
address(3) = 0x7f9a2c18 + 3 * 4 = 0x7f9a2c24
```

Independent of array size. `items[9999999]` costs exactly what `items[0]` costs.
This is also why C# has no array of variably-sized things — the arithmetic needs a
fixed stride. You can measure the stride rather than trust it:

```csharp
int[] probe = [10, 20, 30, 40];
nint stride = System.Runtime.CompilerServices.Unsafe.ByteOffset(ref probe[0], ref probe[1]); // 4
```

## Why insert and remove are O(n)

A contiguous block has no gaps and cannot have holes. Room must be **physically made**:

```
Insert(1, 99)                     RemoveAt(1)
┌────┬────┬────┬────┬────┐        ┌────┬────┬────┬────┬────┐
│ 10 │ 20 │ 30 │ 40 │    │        │ 10 │ 20 │ 30 │ 40 │    │
└────┴──┬─┴──┬─┴──┬─┴────┘        └────┴────┴─┬──┴─┬──┴────┘
        └────┴────┴──► shift right        ◄───┴────┘ shift left
┌────┬────┬────┬────┬────┐        ┌────┬────┬────┬────┬────┐
│ 10 │ 99 │ 20 │ 30 │ 40 │        │ 10 │ 30 │ 40 │  0 │    │
└────┴────┴────┴────┴────┘        └────┴────┴────┴────┴────┘
```

Direction matters and is a classic bug: **insert copies backwards** (forwards would
overwrite each value before copying it), **remove copies forwards**.

Cost by position: front = all n, middle ≈ n/2, end = 0.

## Value types vs reference types

```csharp
int[]    numbers = new int[4];    // the ints ARE the storage
string[] names   = new string[4]; // 8-byte references; strings live elsewhere
```

```
int[4]      ┌────┬────┬────┬────┐   16 bytes, dense, one cache line
            │ 10 │ 20 │ 30 │ 40 │
            └────┴────┴────┴────┘

string[4]   ┌───────┬───────┬───────┬───────┐   32 bytes of pointers…
            │ ptr ─┐│ ptr ─┐│ null  │ null  │
            └──────┼┴──────┼┴───────┴───────┘
                   ▼       ▼
                "alice"  "bob"                   …objects scattered on the heap
```

An `int[1000]` is one dense 4 KB block. A `string[1000]` is 8 KB of pointers *plus*
1000 separate objects. Walking the second means 1000 potential cache misses.

This is also why removal must blank the vacated slot:

```csharp
items[count] = 0;   // on a string[] this would be `null`, and it is what frees the object
```

`Arrays.cs` uses `int`, so that line is only tidiness here. On a `string[]` it is what
lets the GC collect the removed object — skip it and the array holds a live reference
past `count` forever, a real, shipped category of memory leak.

## Cache lines — what Big-O cannot see

The CPU never loads one `int`. It loads a **64-byte cache line**:

```
one memory fetch
├────────────────────────────────────────────────┤
│ 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 │  16 ints, free once you touch one
└────────────────────────────────────────────────┘
```

Then the prefetcher spots the straight-line pattern and fetches ahead. This is why
array iteration beats linked-list iteration several times over at identical O(n).

Practical: iterate **in order**; `matrix[row][col]` walking `col` fastest matches the
layout and is much faster than the reverse.

## Growth and the 2× spike

```
capacity 4, full          copy 4 elements          capacity 8
┌────┬────┬────┬────┐          ──►          ┌────┬────┬────┬────┬────┬────┬────┬────┐
│ 10 │ 20 │ 30 │ 40 │                       │ 10 │ 20 │ 30 │ 40 │    │    │    │    │
└────┴────┴────┴────┘                       └────┴────┴────┴────┴────┴────┴────┴────┘
```

Both blocks are alive during the copy — the transient **2n** memory cost. The old one
becomes garbage. Doubling means reaching n copies ~2n elements *in total*, so appends
average out to O(1) amortised. Growing by a fixed step instead would be O(n²) overall.

Arrays never shrink on their own; `Clear()` resets count but keeps the buffer.

## .NET specifics

- `List<T>` is exactly this: doubling from 4. `new List<T>(10_000)` skips every resize.
- `T[]` is fixed size; `Array.Resize` allocates a new array and copies.
- Arrays ≥ **85,000 bytes** (~21,250 ints) go on the **Large Object Heap**, which is not
  compacted by default — a real fragmentation source in long-running services.

## Summary

| Fact | Consequence |
| --- | --- |
| Elements contiguous | indexing is arithmetic → **O(1)** |
| Elements contiguous | no gaps, so insert/remove shift → **O(n)** |
| Elements share cache lines | iteration far faster than Big-O suggests |
| Values inline, references not | `int[]` dense; `string[]` is pointers to scattered objects |
| Growth copies everything | O(n) per resize, brief 2× memory, GC pressure |
| Stale references past `count` | must blank slots or leak |

Compare: [linked lists](../02-LinkedLists/Memory.md) — the opposite layout ·
[stacks](../03-Stacks/Memory.md) and [queues](../04-Queues/Memory.md) — this, with a rule ·
[heaps](../07-Heaps/Memory.md) — a tree hidden in one of these ·
[hash tables](../05-HashTables/Memory.md) — indexed by content instead of position.
