# Stacks in memory

A stack is not a new memory layout — it is a **restriction**. Push and pop happen at
one end only, and that single rule is what makes every operation O(1).


> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first — stack vs heap, value vs
> reference types, and what an object header costs. This page assumes it.

Everything below is measured by the class next door. From `03-Stacks/`:
`dotnet run Stacks.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## Why the restriction buys O(1)

An array is expensive at the front and free at the end. A stack simply never touches
the front:

```
push / pop happen HERE ──────────────┐
                                     ▼
┌────┬────┬────┬────┬────┬────┬────┬────┐
│ 10 │ 20 │ 30 │ 40 │    │    │    │    │
└────┴────┴────┴────┴────┴────┴────┴────┘
  0    1    2    3    ▲
                   count = 4
```

Push writes at `count` and increments. Pop decrements and reads. **Nothing shifts,
ever** — so no O(n) hides anywhere.

## Array-backed (what .NET uses)

```
STACK                HEAP
┌──────────┐         ┌──────────────────────────────────────────┐
│ stack ───┼────────►│ Stack   items ──┐   count = 4            │
└──────────┘         └─────────────────┼────────────────────────┘
                                       ▼
                     ┌────┬────┬────┬────┬────┬────┬────┬────┐
                     │ 10 │ 20 │ 30 │ 40 │  · │  · │  · │  · │
                     └────┴────┴────┴────┴────┴────┴────┴────┘
                       one contiguous block, exactly like an array
```

Push:

```
Push(50)   →   items[4] = 50; count = 5     one write
Pop()      →   count = 4; return items[4]   one read
```

Memory characteristics are the array's: 24-byte header plus `n × sizeof(T)`,
contiguous, cache-friendly, doubling growth with a transient 2× copy.

**The top of a stack is always cache-hot.** You just touched it, so it is in L1 —
which makes array-backed stacks extremely fast in practice, well beyond what O(1)
alone implies.

## Where each variable in `Stacks.cs` actually lives

```csharp
public class Stacks
{
    public int[] items = new int[0];   // reference type
    public int  count = 0;             // value type
    public long steps = 0;
    public long copies = 0;
    public int  resizes = 0;
}
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `s` (the local) | local | **stack** | 8 B |
| `items` | field, reference | **heap**, in the `Stacks` object | 8 B → a *second* heap object |
| `count` | field, value type | **heap**, in the `Stacks` object | 4 B |
| `steps` / `copies` | fields | **heap** | 8 B each |
| `resizes` | field | **heap** | 4 B |
| `value` (parameter of `Push`) | parameter | **stack**, in `Push`'s frame | 4 B |
| `bigger` (during a resize) | local, reference | **stack** address → **heap** array | 8 B |
| the elements | inline in the `int[]` | **heap** | 4 B each |

16 B header + 32 B of fields = **48 bytes** for the wrapper, plus a separate array
object. Identical to `Arrays.cs` — because a stack *is* an array, minus the operations
that would cost O(n).

**`count` is the entire stack discipline.** It is a single `int` on the heap, and it is
the only thing that makes this a stack rather than an array: push writes at `count`, pop
reads at `count - 1`, and no code path ever touches a lower index.

## Node-backed alternative

```
top ──►┌──────────┐    ┌──────────┐    ┌──────────┐
       │ value 40 │ ┌─►│ value 30 │ ┌─►│ value 20 │
       │ next ────┼─┘  │ next ────┼─┘  │ next=null│
       └──────────┘    └──────────┘    └──────────┘
```

Push allocates a node and points it at the old top. Pop moves `top` down one.

| | Array-backed | Node-backed |
| --- | --- | --- |
| Memory per int | 4 B + spare capacity | 32 B |
| Push/pop | O(1) amortised | O(1) **worst case** |
| Resize pauses | occasional O(n) copy | never |
| Cache behaviour | excellent | poor |
| Allocation | rare (on growth) | one object per push |

Array-backed wins nearly always. Node-backed matters only when you need a hard O(1)
worst-case guarantee with no resize pause (real-time systems), or when the stack must
share nodes with another structure.

## Blanking on pop

```csharp
count--;
int value = items[count];
items[count] = 0;          // `null` on a reference type — required there
return value;
```

`Stacks.cs` stores `int`, so blanking is only tidiness. On a stack of reference types
that line is mandatory: without it the popped object stays reachable from the buffer and
is never collected — the same leak as in arrays. .NET's `Stack<T>` does exactly this.

## The other stack — the call stack

Worth connecting, because the name is not a coincidence. Each method call pushes a
**frame** holding parameters, locals and the return address; returning pops it.

```
        ┌────────────────────┐  ← stack pointer
        │ Recurse(3)  n=3    │
        │ Recurse(2)  n=2    │
        │ Recurse(1)  n=1    │
        │ Main()             │
        └────────────────────┘  grows downward, ~1 MB per thread
```

Two consequences you will be asked about:

- Recursion depth is bounded. Too deep → **StackOverflowException**, which cannot be
  caught and kills the process.
- A recursive algorithm's **space** complexity is its maximum depth. Recursive tree
  traversal is O(h) space even allocating nothing — the frames are the space, and
  forgetting to count them is the classic complexity mistake.

Converting recursion to iteration with an explicit `Stack<T>` moves those frames onto
the heap, where you have gigabytes instead of one megabyte.

## .NET specifics

- `Stack<T>` is array-backed, doubles on growth, `Push`/`Pop`/`Peek` are O(1).
- `Pop` on an empty stack throws `InvalidOperationException`; `TryPop` returns false.
- Enumerating yields **top to bottom**, which surprises people.
- `Stack<T>` is not thread-safe; `ConcurrentStack<T>` is node-backed and lock-free.

## Summary

| Fact | Consequence |
| --- | --- |
| One end only | no shifting → every operation O(1) |
| Array-backed | contiguous, cache-hot at the top |
| Doubling growth | O(1) amortised push, brief 2× memory |
| Must blank popped slots | otherwise references leak |
| Call stack is this structure | recursion depth = space complexity |

Compare: [arrays](../01-Arrays/Memory.md) — what this is, restricted ·
[queues](../04-Queues/Memory.md) — the same idea at both ends ·
[linked lists](../02-LinkedLists/Memory.md) — the node-backed alternative ·
[graphs](../09-Graphs/Memory.md) — DFS runs on this ·
[trees](../06-Trees/Memory.md) — iterative traversal replaces recursion with one of these.
