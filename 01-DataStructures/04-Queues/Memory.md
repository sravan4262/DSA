# Queues in memory

A queue touches **both ends** — add at the back, remove from the front. That is the
one difference from a stack, and it breaks the naive array implementation completely.


> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first — stack vs heap, value vs
> reference types, and what an object header costs. This page assumes it.

Everything below is measured by the class next door. From `04-Queues/`:
`dotnet run Queues.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The problem

Removing from the front of an array is its worst case:

```
Dequeue()
┌────┬────┬────┬────┬────┐
│ 10 │ 20 │ 30 │ 40 │    │      return 10, then...
└────┴────┴────┴────┴────┘
      ◄────┴────┴────           ...shift EVERYTHING left
┌────┬────┬────┬────┬────┐
│ 20 │ 30 │ 40 │    │    │      O(n) per dequeue
└────┴────┴────┴────┴────┘
```

n dequeues cost O(n²). A queue built this way is unusable.

## The fix: stop moving the data, move the indices

Keep two indices and leave the elements alone:

```
┌────┬────┬────┬────┬────┬────┬────┬────┐
│  · │  · │ 30 │ 40 │ 50 │  · │  · │  · │
└────┴────┴────┴────┴────┴────┴────┴────┘
            ▲              ▲
          head           tail
       (next to remove) (next to insert)
```

Dequeue reads `head` and increments it. Enqueue writes at `tail` and increments.
**Both O(1), nothing shifts.**

But now the front of the buffer is dead space, and `tail` marches toward the end
while slots 0 and 1 sit unused.

## The circular buffer

Let the indices **wrap around** with modulo. The array is treated as a ring:

```
     linear view                          ring view
┌────┬────┬────┬────┬────┬────┬────┐          0   1
│ 60 │ 70 │  · │  · │ 30 │ 40 │ 50 │        7 ┌───────┐ 2
└────┴────┴────┴────┴────┴────┴────┘          │       │
       ▲         ▲                          6 │   ●   │ 3
     tail      head                            └───────┘
                                              5   4
   head = 4, tail = 2, count = 5

   the queue in order is: 30 40 50 60 70 — it simply wraps past the end
```

```csharp
Enqueue(x)  →  items[tail] = x;  tail = (tail + 1) % items.Length;  count++;
Dequeue()   →  x = items[head];  head = (head + 1) % items.Length;  count--;  return x;
```

The `%` is the entire trick. No shifting, no wasted space, both ends O(1).

**Why `count` and not `head == tail`:** when head equals tail the queue is either
completely empty or completely full, and those cannot be told apart. Storing `count`
resolves it. (The alternative is deliberately leaving one slot empty.)

## Growing a circular buffer

The one genuinely fiddly part: you cannot just copy the block, because the data wraps.

```
old (wrapped)                      new (unwrapped on copy)
┌────┬────┬────┬────┐              ┌────┬────┬────┬────┬────┬────┬────┬────┐
│ 60 │ 70 │ 30 │ 40 │      ──►     │ 30 │ 40 │ 60 │ 70 │  · │  · │  · │  · │
└────┴────┴────┴────┘              └────┴────┴────┴────┴────┴────┴────┴────┘
  head = 2                           head = 0, tail = 4
```

Walk the queue in **logical** order and write it out flat — that unwraps it in one loop:

```csharp
for (int i = 0; i < count; i++)
{
    bigger[i] = items[(head + i) % items.Length];
}
head = 0;
tail = count;
```

Resetting `head` and `tail` afterwards is not optional: the data is no longer where the
old indices said it was. (The two-block alternative — copy head→end, then start→tail —
is equivalent; `Queues.cs` uses the modulo walk because it is one loop instead of two.)
Growth is O(n) and doubles, so enqueue stays O(1) amortised.

## Memory characteristics

Identical to an array, because it *is* an array: 24-byte header, `n × sizeof(T)`,
contiguous, cache-friendly, transient 2× during growth.

Blanking on dequeue matters here too:

```csharp
items[head] = 0;   // `null` on a reference type, or the dequeued object never gets collected
```

## Where each variable in `Queues.cs` actually lives

```csharp
public class Queues
{
    public int[] items = new int[0];
    public int  head = 0;      // next to remove
    public int  tail = 0;      // next to insert
    public int  count = 0;     // disambiguates head == tail
    public long steps = 0;
    public long copies = 0;
    public int  resizes = 0;
}
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `q` (the local) | local | **stack** | 8 B |
| `items` | field, reference | **heap**, in the `Queues` object | 8 B → the buffer object |
| `head` / `tail` / `count` / `resizes` | fields, value types | **heap** | 4 B each |
| `steps` / `copies` | fields | **heap** | 8 B each |
| `value` (parameter of `Enqueue`) | parameter | **stack** | 4 B |
| `bigger`, `i` (during a resize) | locals | **stack** | 8 / 4 B |
| the elements | inline in the `int[]` | **heap** | 4 B each |

16 B header + 40 B of fields = **56 bytes** for the wrapper — the heaviest of the
array-backed structures, and the extra 8 bytes over a stack are `head` and `tail`.

**Those two ints are the whole design.** They cost 8 bytes and they turn dequeue from
O(n) into O(1). The data never moves; only the numbers describing where it starts and
ends do. That is the trade in its smallest possible form: **8 bytes of bookkeeping buys
a complexity class.**

## Node-backed alternative

A linked list with **two** pointers gives O(1) at both ends with no wrapping logic:

```
head ──►┌──────────┐    ┌──────────┐    ┌──────────┐◄── tail
        │ value 30 │ ┌─►│ value 40 │ ┌─►│ value 50 │
        │ next ────┼─┘  │ next ────┼─┘  │ next=null│
        └──────────┘    └──────────┘    └──────────┘
        dequeue here                    enqueue here
```

| | Circular array | Node-backed |
| --- | --- | --- |
| Memory per int | 4 B + spare | 32 B |
| Enqueue/dequeue | O(1) amortised | O(1) **worst case** |
| Resize pauses | occasional O(n) | never |
| Cache behaviour | excellent | poor |
| Implementation | modulo arithmetic | pointer updates |

The array version wins in practice. The node version is used when an unbounded queue
must never pause to resize, or when producers and consumers touch opposite ends
concurrently — no shared resize to synchronise on.

## Deques

A double-ended queue allows insert/remove at both ends. Same circular buffer, with
`head` decrementing on push-front (wrapping backwards):

```
head = (head - 1 + items.Length) % items.Length;
```

The `+ items.Length` is what stops `-1 % n` producing a negative index in C#.

## .NET specifics

- `Queue<T>` is a **circular array buffer** with `_head`, `_tail`, `_size`. Doubles on
  growth, `Enqueue`/`Dequeue` O(1) amortised.
- `Dequeue` on empty throws `InvalidOperationException`; `TryDequeue` returns false.
- **There is no `Deque<T>` in .NET.** People reach for `LinkedList<T>` or `List<T>`
  and accidentally get O(n) — worth knowing before an interview.
- `ConcurrentQueue<T>` is a lock-free linked set of array segments.

## Summary

| Fact | Consequence |
| --- | --- |
| Both ends are used | naive array dequeue is O(n) — must not shift |
| Circular buffer + modulo | both ends O(1), no wasted space |
| Wrapping data | resize must unwrap, not block-copy |
| head == tail is ambiguous | track `count` (or leave one slot empty) |
| Array-backed | contiguous, cache-friendly, doubling growth |

Compare: [stacks](../03-Stacks/Memory.md) — the same idea at one end ·
[arrays](../01-Arrays/Memory.md) — the storage underneath ·
[linked lists](../02-LinkedLists/Memory.md) — the node-backed alternative ·
[graphs](../09-Graphs/Memory.md) — BFS runs on this, and that is where shortest paths come from.
