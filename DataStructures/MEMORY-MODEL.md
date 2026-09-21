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
wall-clock cost. The next two sections are *why*.

---

## The hardware underneath — cores, cache and RAM

Everything above is about *where* bytes live. This is about *how long it takes to reach
them*, which is where all the surprising performance comes from.

### CPU vs core

**The CPU is the physical chip. A core is an independent processing engine inside it.**

The words used to mean the same thing, because a CPU had one core. Around 2005 clock
speeds stopped rising — the heat became unmanageable — so chipmakers stopped building one
faster engine and started putting several on the same die.

```
                    THE CPU (one physical chip)
┌──────────────────────────────────────────────────────────────┐
│   ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│   │  Core 0  │  │  Core 1  │  │  Core 2  │  │  Core 3  │     │
│   │ registers│  │ registers│  │ registers│  │ registers│     │
│   │ L1 cache │  │ L1 cache │  │ L1 cache │  │ L1 cache │     │
│   │ L2 cache │  │ L2 cache │  │ L2 cache │  │ L2 cache │     │
│   └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘     │
│        └─────────────┴──────┬──────┴─────────────┘           │
│        ┌────────────────────▼───────────────────┐            │
│        │       L3 cache — SHARED by all cores    │            │
│        └────────────────────┬───────────────────┘            │
└─────────────────────────────┼────────────────────────────────┘
                              │ memory bus
                   ┌──────────▼──────────┐
                   │  RAM — separate     │
                   │  chips on the board │
                   └─────────────────────┘
```

Each core has its **own** registers, arithmetic units, L1 and L2. They **share** L3 and
the route to RAM.

A core runs exactly **one thread at a time**. Eight cores means eight threads genuinely
executing at once; everything else is waiting, swapped in and out by the OS thousands of
times a second. This — not memory — is the real ceiling on useful thread count, and why
`async`/`await` and thread pools exist: ten thousand concurrent operations share a
handful of threads instead of allocating ten thousand stacks.

"8 cores, 16 logical processors" is **SMT** (hyper-threading): one core keeping two
threads' state loaded so it can switch instantly when one stalls on memory. The execution
hardware is still shared — expect ~25%, not 100%.

### Why cache exists at all

**CPUs got roughly 1000× faster since the 1980s. RAM got roughly 10×.**

A core at 3 GHz retires ~12 instructions per nanosecond. A trip to RAM takes ~80
nanoseconds. Without caches, a modern CPU would perform like a 100 MHz one — it would
spend its life waiting. Caches exist entirely to hide that gap.

RAM is slow for two unavoidable reasons. **It is physically far away** — centimetres,
across a bus, while L1 is micrometres away inside the core. At 3 GHz one cycle is 0.33 ns,
and light travels 10 cm in that time; distance is a real cost. **And it is built from
slower stuff** — DRAM is one transistor and a leaky capacitor per bit, dense and cheap but
needing constant refresh. Cache is SRAM, six transistors per bit: far faster, far larger,
far more power. 16 GB of SRAM would cost more than a car and need its own cooling.

So the hierarchy is a compromise, not a flaw.

### The hierarchy

| Level | Physically | Size | Latency | If L1 were 1 second |
| --- | --- | --- | --- | --- |
| Registers | inside the core | ~1 KB | 0 cycles | instant |
| **L1 cache** | inside the core | 32–48 KB | ~4 cy / 1 ns | **1 second** |
| L2 cache | inside the core | 0.5–2 MB | ~14 cy / 4 ns | 4 seconds |
| L3 cache | on the chip, shared | 8–64 MB | ~40 cy / 15 ns | 15 seconds |
| **Main RAM** | separate chips | 8–128 GB | ~250 cy / 80 ns | **1.5 minutes** |
| SSD (page file) | a drive | 0.5–4 TB | ~100 µs | 1.5 days |

Read the last column twice. From the core's point of view, reaching RAM is the difference
between answering from memory and making a phone call.

Three levels rather than one because **a bigger cache is a slower cache** — more addresses
to search. L1 stays tiny on purpose so it can answer in four cycles. L2 catches what
spills out. L3 is shared, which is its real job: two cores working on the same data find
one copy there instead of both going to RAM. In practice L1 catches ~90–95% of accesses
and only ~1% reach RAM — but that 1% is often most of your runtime.

### Cache is a copy, not a place

**Nothing is ever "stored in cache." Everything lives in RAM. Cache holds temporary
copies of the bits of RAM you touched recently.**

You cannot allocate into it, choose what goes there, or observe it from your program. It
is entirely automatic — below your code, below the runtime, below the OS.

```
read address 0x7f9a2c18
   ├─ in L1?  → return it                                  4 cycles
   ├─ in L2?  → copy to L1, return                        14 cycles
   ├─ in L3?  → copy to L2+L1, return                     40 cycles
   └─ else    → fetch the whole 64-byte LINE from RAM    250 cycles
                 and install it in L3, L2 and L1
```

Two consequences. **Every miss populates the cache for next time** — the fetch is a whole
64-byte line, never your four bytes, which is why the next fifteen ints are free. And
**cache is finite, so something gets evicted** — L1 holds 32 KB, so touching a 33rd KB
throws out the least-recently-used line. This is why walking a small array twice is much
cheaper than walking a huge one twice.

### So where do the stack and heap actually live?

**Both are in RAM.** They are not separate hardware. They are two regions of one
process's address space, used with different rules — strictly LIFO versus any order.
Same RAM, same caches, same physics.

```
        YOUR PROCESS  ── all of this is RAM ──
┌──────────────────────────────────────────────────────┐
│  code · static data                                  │
├──────────────────────────────────────────────────────┤
│  HEAP ──── grows up ────►                            │
│                          …unused address space…      │
│                             ◄──── grows down ── STACK│
└──────────────────────────────────────────────────────┘
```

But there is a reason the stack feels faster than "one pointer move" explains. The stack
is **small** (a few KB of live frames — it fits in L1 with room to spare), **hot** (every
call and every local touches it, so it is never evicted) and **contiguous** (frames sit
adjacent and the stack pointer moves in a straight line).

**So the top of the stack is effectively always resident in L1.** That — not the
arithmetic — is the deepest reason stack access is fast. The heap gets no such guarantee:
gigabytes, touched unpredictably, scattered. Some of it is cached; most is not.

One layer lower still, every address your program uses is **virtual**, translated to a
physical location by hardware on each access. That is why reserving a 1 MB stack costs
nothing — it reserves addresses, and physical pages are attached only as you touch them.
It is also the escape hatch when RAM runs out: cold pages are written to disk, and
touching one costs a **page fault** of ~100 µs — a thousand times worse than a cache miss,
and why a swapping machine feels broken rather than merely slow.

---

## An array walk, step by step

All of the above, on twenty elements.

```csharp
int[] nums = new int[20];       // 80 bytes of data
for (int i = 0; i < 20; i++) sum += nums[i];
```

Cache lines are 64 bytes and **aligned** — memory is carved into fixed 64-byte tiles. Say
the elements start at `0x1000`; the twenty ints straddle exactly two of them:

```
┌─────────────────────── LINE A (0x1000) ────────────────────────┬─── LINE B ────
│ [0] [1] [2] [3] [4] [5] [6] [7] [8] [9][10][11][12][13][14][15]│[16][17][18][19]
└────────────────────────────────────────────────────────────────┴───────────────
```

| Step | What happens | Cost |
| --- | --- | --- |
| `i=0` | miss through L1, L2, L3 → RAM returns **all of LINE A** | ~250 cy |
| `i=1..15` | already in L1 — they arrived with element 0 | ~4 cy each |
| `i=16` | *should* be a miss, but the prefetcher already fetched LINE B | ~4 cy |
| `i=17..19` | L1 hits | ~4 cy each |

Two mechanisms did that. **Spatial locality**: the hardware bets that whoever reads one
byte wants its neighbours, so it never fetches less than a line — you asked for one int
and got sixteen. **The prefetcher**: it watched the addresses `0x1000, 0x1004, 0x1008…`,
recognised a constant stride, and fetched the next line before it was asked for.

```
i:  0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  16  17  18  19
    ███ ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·
   MISS  └────────── free, from LINE A ──────────┘  └── LINE B, prefetched ──┘
```

**One real stall in twenty reads.** ~326 cycles, against ~5,000 if every access had
missed. A 15× speedup you did nothing to earn — except lay the data out contiguously.

### The same walk over `string[20]`

```csharp
for (int i = 0; i < 20; i++) total += names[i].Length;
```

The same loop shape, but `names[i]` yields an *address*, so there are now two accesses per
iteration and they behave nothing alike:

| | What it costs |
| --- | --- |
| Read the pointer out of the array | sequential, 8 per line, prefetched — **cheap** |
| Follow it to the string | could be anywhere in gigabytes — **~250 cy, a miss** |

| | `int[20]` | `string[20]` |
| --- | --- | --- |
| Real RAM stalls | **1** | **~21** |
| Approximate cycles | ~326 | ~5,300 |

Identical `O(n)`. About 16× apart.

### Pointer chasing — the idea worth carrying everywhere

It is worse than 21 misses, because normally a core keeps **10+ memory requests in flight
at once** and the waits overlap. That cannot happen here:

> **You cannot begin fetching string #2 until you have finished loading pointer #2.**

Each fetch depends on the result of the last, so the stalls queue up end to end instead of
overlapping. The prefetcher is equally blind, for the same reason — `0x8A20, 0x4F10,
0xC330` has no stride to spot, and it cannot predict an address that does not exist yet.

```
int[]     ──────────────────────────────►   one straight run
                                             predictable · prefetched · overlapped

string[]  ──┐    ──┐    ──┐    ──┐          load pointer → jump → WAIT
            ▼      ▼      ▼      ▼           blind · dependent · serialised
           ???    ???    ???    ???
```

**One honest caveat.** Allocate those strings in a tight loop and .NET bumps a pointer, so
they land back to back and the walk is nearly as fast as `int[]`. It degrades once reality
intrudes: objects created at different times, survivors of different GC generations, items
sorted or shuffled, a service whose heap has churned for hours.

**The `int[]` is fast by construction. The `string[]` is fast only by luck.** And a linked
list is the worst case of all — the `string[]` problem with no sequential part at all,
every single step a dependent chase.

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

And the hardware half:

| Question | Answer |
| --- | --- |
| CPU vs core | CPU = the chip; core = an independent engine inside it |
| Why cache exists | CPUs got ~1000× faster, RAM ~10× — cache hides the gap |
| L1 / L2 / L3 | all inside the CPU; L1+L2 private per core, L3 shared; smaller = faster |
| RAM | separate chips, ~80 ns, roughly 50× slower than L1 |
| Where is the stack? | in RAM — but small and hot, so effectively always resident in L1 |
| Where is the heap? | in RAM — far too big to cache, so locality is *your* problem |
| Why arrays are fast | contiguous → one fetch brings 16 ints, and the prefetcher predicts the rest |
| Why pointer-chasing is slow | the next address is unknown until the current load finishes → no prefetch, no overlap |
| Cache line | 64 bytes, aligned — the smallest unit memory ever moves |
| The rule | Big-O counts operations; it cannot see that one costs 4 cycles and another 250 |

---

## Where to go next

Each structure's `Memory.md` applies all of this concretely:

[Arrays](01-Arrays/Memory.md) · [Linked lists](02-LinkedLists/Memory.md) ·
[Stacks](03-Stacks/Memory.md) · [Queues](04-Queues/Memory.md)
