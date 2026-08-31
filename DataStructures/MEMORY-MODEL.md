# Stack vs heap — where variables actually live

Read this once, before any data structure. Every `Memory.md` in the folders below
assumes it, and almost every complexity question eventually reduces to it.

---

## The two regions

```
        STACK                                    HEAP
 ┌───────────────────────┐              ┌──────────────────────────────┐
 │ Add() frame           │              │  int[] { 10, 20, 30, 40 }    │
 │   value = 50          │              │  Arrays { items, count, … }  │
 │   i     = 3           │              │  "hello"                     │
 │   bigger ─────────────┼─────────────►│  ...                         │
 ├───────────────────────┤              │                              │
 │ Main() frame          │              │  scattered, any order,       │
 │   a ──────────────────┼─────────────►│  cleaned up by the GC        │
 └───────────────────────┘              └──────────────────────────────┘
   grows down, ~1 MB per thread           gigabytes, garbage collected
   freed automatically on return          freed when nothing points at it
```

| | Stack | Heap |
| --- | --- | --- |
| Size | ~1 MB per thread | limited by RAM |
| Allocation | move a pointer — nearly free | find space, maybe trigger a GC |
| Cleanup | automatic when the method returns | garbage collector, non-deterministic |
| Lifetime | exactly the method call | until nothing references it |
| Layout | strictly LIFO frames | anywhere |
| Overflow | `StackOverflowException`, kills the process | `OutOfMemoryException` |

The stack is fast because freeing is just moving a pointer back. There is no
bookkeeping, no scanning, no fragmentation. The price is that everything dies when the
method returns — which is exactly why anything that must outlive the call goes on the heap.

---

## What is the stack?

**One fixed block of memory per thread — about 1 MB — used strictly last-in-first-out.**

Every method call pushes a **frame** onto it holding that call's parameters, its local
variables, and the address to return to. When the method returns, the frame is popped.

```
        ┌────────────────────┐  ← stack pointer moves down as you call deeper
        │ Add()   value=10   │
        │         i=3        │
        ├────────────────────┤
        │ Main()  a=0x7f9a…  │
        └────────────────────┘
```

Allocating is **one instruction** — move the stack pointer down by the size of the frame.
Freeing is moving it back up. There is no searching for space, no bookkeeping, no
fragmentation and no garbage collector, which is why the stack is the fastest memory
there is.

The price is rigidity:

- **Everything dies when the method returns.** Nothing on the stack can outlive its call.
- **It is small.** ~1 MB total, so deep recursion runs out — `StackOverflowException`,
  which cannot be caught and kills the process.
- **Sizes must be known at compile time.** You cannot grow a stack frame at runtime.

## What is the heap?

**A large shared pool of memory — limited only by RAM — where objects live for as long
as something still points at them.**

Anything whose lifetime is not tied to a single method call goes here: every `class`
instance, every array, every string. The stack holds an 8-byte *address*; the object
itself sits on the heap.

```
        HEAP
        ┌──────────────────────────────────────────────────┐
        │  int[]{10,20,30}   Arrays{…}   "hello"   …       │
        │  allocated in any order, freed in any order      │
        └──────────────────────────────────────────────────┘
```

Allocating is still cheap — .NET bumps a pointer in gen 0, much like the stack. **The
cost is cleanup.** Because objects die in any order rather than LIFO, the runtime cannot
simply move a pointer back. It has to periodically stop, work out what is still
reachable, free the rest, and compact the survivors. That is the garbage collector.

What you get for that cost:

- **Objects outlive the method that created them.** Return one and it stays alive.
- **Size is decided at runtime.** Arrays sized from a variable, collections that grow.
- **Shared by reference.** Two variables can point at the same object.

The short version: **the stack is fast but rigid and tiny; the heap is flexible and
large but needs collecting.**

## How many bytes does anything take?

On 64-bit .NET. This is the table to internalise — most memory questions are arithmetic
on top of it.

**Value types** — the data itself, stored inline wherever the variable lives:

| Type | Bytes |
| --- | --- |
| `bool`, `byte`, `sbyte` | 1 |
| `char`, `short`, `ushort` | 2 |
| `int`, `uint`, `float` | 4 |
| `long`, `ulong`, `double`, `DateTime` | 8 |
| `decimal`, `Guid` | 16 |
| a `struct` | sum of its fields, padded to an 8-byte boundary |

**References** — the variable that points at a heap object:

| | Bytes |
| --- | --- |
| any reference (`class`, array, `string`, `object`) | **8** |

That 8 bytes is the *pointer only*. The object it points at is charged separately.

**Overhead paid by every heap object:**

| | Bytes | |
| --- | --- | --- |
| Sync block index | 8 | `lock` and `GetHashCode` bookkeeping |
| Method table pointer | 8 | which type this is |
| **Object header total** | **16** | on every single object |
| Array length field | +8 | arrays only, so arrays cost **24** |
| **Minimum object size** | **24** | even a class with one `byte` |
| Alignment | — | total rounded up to a multiple of 8 |

**Worked examples:**

| What you store | Arithmetic | Total |
| --- | --- | --- |
| `int x` (local) | 4 | **4 B** |
| `int[4]` | 24 header + 4×4 | **40 B** |
| `int[1000]` | 24 + 4000 | **4,024 B** |
| `class P { int X; }` | 16 header + 4 + 4 padding | **24 B** |
| `class P { long A; long B; int C; }` | 16 + 8 + 8 + 4 + 4 pad | **40 B** |
| `string "hello"` | 16 + 4 length + 12 chars/terminator | **32 B** |
| `object o = 42` (boxed) | 16 header + 4 + 4 padding | **24 B** |
| a linked-list node holding one `int` | 16 + 8 next + 4 value + 4 pad | **32 B** |

That last row is the one worth remembering: **32 bytes to store 4 bytes of data.** Store
a million ints in an `int[]` and it costs ~4 MB; store them in a linked list and it costs
~32 MB plus a million objects for the GC to track.

## What decides where a thing lives

Two questions, in order:

**1. Is it a value type or a reference type?**

- **Value types** — `int`, `long`, `double`, `bool`, `char`, `decimal`, any `struct`,
  `DateTime`, `Guid`, enums. The variable **is** the data.
- **Reference types** — any `class`, every array (`int[]` included), `string`,
  delegates, `object`. The variable is an **8-byte address**; the data is elsewhere.

**2. Where is it declared?**

- A **local variable or parameter** lives in the current stack frame.
- A **field** lives inside its containing object — so a field of a class is on the heap,
  no matter what type it is.

```csharp
void Method()
{
    int x = 5;                  // value type, local  → stack, 4 bytes, IS the data
    int[] a = new int[4];       // reference type     → 8-byte address on the stack,
                                //                      the array object on the heap
    Arrays list = new Arrays(); // same: address on stack, object on heap
}
```

### The correction almost everyone needs

> "Value types go on the stack, reference types go on the heap."

That is wrong often enough to cause real bugs. **A value type goes wherever its owner
goes.** `count` is an `int`, but it is a field of `Arrays`, so it sits inside the
`Arrays` object — on the heap. It never touches the stack.

```csharp
class Arrays
{
    public int count;   // an int, but ON THE HEAP, inside the Arrays object
}

void M()
{
    int count;          // an int, ON THE STACK, inside M's frame
}
```

Same type, two different homes. It is the *declaration site* that decides, not the type.

---

## What an object looks like on the heap (64-bit)

```
        ┌──────────────┬──────────────┬─────────────────────────────┐
        │ sync block   │ method table │ fields…                     │
        │ index  8 B   │ pointer 8 B  │                             │
        └──────────────┴──────────────┴─────────────────────────────┘
        ▲              ▲
        │              └── the reference actually points HERE
        └── sits *behind* the pointer, at offset −8
```

| Part | Size | Purpose |
| --- | --- | --- |
| Sync block index | 8 B | `lock`, `GetHashCode` — mostly unused, always paid for |
| Method table pointer | 8 B | the type; how the runtime knows what this object is |
| Fields | varies | your data |
| Padding | 0–7 B | rounds up to an 8-byte boundary |

**Minimum object size is 24 bytes**, even for a class with a single `byte` field. This
overhead is why "millions of tiny objects" is a bad layout and why an `int[1000]` beats
a linked list of 1000 ints by roughly 8×.

**Field order is chosen by the runtime, not by you.** It packs to avoid padding, so the
declaration order in your source usually is not the memory order. (`[StructLayout]`
overrides this; you rarely want to.)

An **array** has one extra header field — its length — between the method table pointer
and the elements, so its overhead is 24 bytes rather than 16.

---

## Arrays of values vs arrays of references

This one distinction explains most surprising performance:

```csharp
int[]    numbers = new int[4];    // the ints ARE the storage
string[] names   = new string[4]; // 8-byte addresses; the strings are elsewhere
```

```
int[4]      ┌────┬────┬────┬────┐  16 bytes of data, one cache line, dense
            │ 10 │ 20 │ 30 │ 40 │
            └────┴────┴────┴────┘

string[4]   ┌───────┬───────┬───────┬───────┐  32 bytes of pointers…
            │ ptr ─┐│ ptr ─┐│ null  │ null  │
            └──────┼┴──────┼┴───────┴───────┘
                   ▼       ▼
                "alice"  "bob"                  …plus separate objects, anywhere
```

Walking the first is one straight run through memory. Walking the second and touching
each string is a potential cache miss per element. Identical `O(n)`, very different
wall-clock cost.

---

## Boxing — a value type sent to the heap

```csharp
int x = 42;
object o = x;       // BOXING: allocates a heap object, copies 42 into it
int y = (int)o;     // unboxing: copies it back out
```

24 bytes allocated and a GC eventually needed, to store 4 bytes. In a loop this is a
classic invisible performance bug. Generics exist largely to avoid it: `List<int>`
stores ints inline, while the old non-generic `ArrayList` boxed every single one.

---

## How the heap gets cleaned up

The GC is **generational**, on the observation that most objects die young:

- **Gen 0** — new objects. Collected often and very cheaply.
- **Gen 1** — survived one collection. A buffer between 0 and 2.
- **Gen 2** — long-lived. Collected rarely; the expensive one.
- **Large Object Heap** — anything **≥ 85,000 bytes** (an `int[]` of ~21,250). Not
  compacted by default, collected only with gen 2. A `List<T>` that doubles past this
  boundary starts allocating LOH blocks on every growth — a real fragmentation source
  in long-running services.

Something is collectable when nothing reachable points at it. This is why a stale
reference sitting past `count` in an array keeps an object alive forever — the array
still points at it, so the GC must assume it is in use.

---

## The stack is also a data structure

The call stack is literally a stack, and two consequences get asked about constantly:

```
        ┌────────────────────┐  ← stack pointer
        │ Recurse(3)  n=3    │
        │ Recurse(2)  n=2    │
        │ Recurse(1)  n=1    │
        │ Main()             │
        └────────────────────┘  each frame: parameters, locals, return address
```

- **Recursion depth is bounded** by ~1 MB. Too deep throws `StackOverflowException`,
  which **cannot be caught** and terminates the process.
- **A recursive algorithm's space complexity is its maximum depth.** A recursive tree
  traversal is `O(h)` space even though it allocates nothing on the heap. Forgetting to
  count the frames is the single most common complexity mistake.

Converting recursion to an explicit `Stack<T>` moves those frames to the heap, where you
have gigabytes instead of one megabyte.

---

## Quick reference

| Declaration | Lives | Size |
| --- | --- | --- |
| `int x` — local | stack frame | 4 B |
| `int x` — field of a class | heap, inside the object | 4 B |
| `int[] a` — local | address on stack, object on heap | 8 B + 24 B + 4n |
| `string s` — local | address on stack, object on heap | 8 B + object |
| `struct` — local | stack, inline | sum of fields |
| `struct` — field of a class | heap, inline in the object | sum of fields |
| `object o = 42` | heap, boxed | 8 B + 24 B |
| method parameters | stack frame | per type |
| `class` instance | heap | 16 B header + fields |

---

## Where to go next

Each structure's `Memory.md` applies all of this concretely:

[Arrays](01-Arrays/Memory.md) · [Linked lists](02-LinkedLists/Memory.md) ·
[Stacks](03-Stacks/Memory.md) · [Queues](04-Queues/Memory.md)
