# Hash tables in memory

**An array you index by content instead of by position.** One arithmetic step turns a
key into a slot number, which is why the search disappears entirely.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first — stack vs heap, value vs
> reference types, and what an object header costs. This page assumes it.

Everything below is measured by the class next door. From `05-HashTables/`:
`dotnet run HashTables.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

```
  key ──► hash(key) ──► % bucketCount ──► an array index

  "apple"  ──►  9834721  ──►  9834721 % 8  ──►  bucket 1


  buckets: an array of REFERENCES        entries: separate heap objects
  ┌───┬───┬───┬───┬───┬───┬───┬───┐
  │ · │ ●─┼─· │ ●─┼─· │ · │ ●─┼─· │
  └───┴─┼─┴───┴─┼─┴───┴───┴─┼─┴───┘
    0   1   2   3   4   5   6   7
        │       │           │
        ▼       ▼           ▼
  ┌──────────┐ ┌──────────┐ ┌──────────┐
  │"apple" 3 │ │"pear"  9 │ │"fig"   1 │
  │ next ────┼┐│ next=null│ │ next=null│
  └──────────┘│└──────────┘ └──────────┘
              ▼
  ┌──────────┐    TWO KEYS IN ONE BUCKET = a COLLISION.
  │"grape" 7 │    They are chained in a linked list.
  │ next=null│
  └──────────┘
```

Two separate allocations are involved: **the bucket array**, which holds only
references, and **one entry object per key/value pair**, scattered across the heap.

## What it costs

| Part | Size |
| --- | --- |
| Bucket array | 24 + 8 × bucketCount |
| Object header per entry | 16 B |
| Key reference | 8 B |
| Value (`int`) | 4 B |
| `next` reference | 8 B |
| Padding | 4 B |
| **Per entry** | **40 B** — plus the key object itself |

A `Dictionary<string,int>` with 1,000 entries is roughly 8 KB of buckets, 40 KB of
entries, and 1,000 separate string objects. **A hash table is not compact.** You trade
memory and cache locality for the ability to skip searching.

## Where each variable in `HashTables.cs` actually lives

```csharp
public class Entry       { public string key; public int value; public Entry? next; }
public class HashTables  { public Entry?[] buckets = new Entry?[8];
                           public int count; public double maxLoad = 0.75;
                           public bool sabotageHash; public long steps; public int rehashes; }
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `t` (the local) | local | **stack** | 8 B |
| `buckets` | field, reference | **heap**, in `HashTables` | 8 B → the bucket array |
| `maxLoad` | field, `double` | **heap** | 8 B |
| `steps` | field | **heap** | 8 B |
| `count` / `rehashes` | fields | **heap** | 4 B each |
| `sabotageHash` | field, `bool` | **heap** | 1 B + padding |
| each bucket slot | inline in the array | **heap** | 8 B — a reference, or `null` |
| `key` | field of `Entry`, reference | **heap** | 8 B → a *separate* `string` object |
| `value` / `next` | fields of `Entry` | **heap** | 4 / 8 B |
| `hash`, `bucket`, `current` (in `Get`) | locals | **stack** | 4 / 4 / 8 B |

The wrapper is ~56 bytes. But notice how many **hops** one lookup crosses:

```
read t          (stack)  → the HashTables object
read buckets    (heap)   → the bucket array
read buckets[i] (heap)   → an Entry, allocated somewhere unrelated
read entry.key  (heap)   → a string, allocated somewhere else again
```

**Four objects to answer one question.** The bucket array holds only references — no
key or value is ever stored inside it. That indirection is invisible in the O(1) and is
exactly why the cache section below matters.

## Why lookup is O(1) — and only on average

Hashing computes *where* the key must be, exactly as an array computes where index `i`
must be. Nothing is scanned. The only work left is walking that one bucket's chain.

That chain length is the whole story:

| Load factor (entries ÷ buckets) | Typical chain | Lookup |
| --- | --- | --- |
| 0.25 | 0–1 | fast, wasteful |
| 0.75 | ~1 | the usual target |
| 4.00 | ~4 | degrading |
| n (all collide) | n | **O(n)** — one linked list |

So the honest claim is **O(1) average, O(n) worst**. Saying just "O(1)" is the thing
interviewers probe. The worst case is real: a bad `GetHashCode`, or an attacker feeding
colliding keys, turns the table into a linked list that also costs 40 bytes per node.

## Resizing and rehashing

When the load factor passes its threshold, the table allocates twice the buckets and
**rehashes every entry** — because `% bucketCount` changed, so almost every key now
belongs somewhere else.

```
  8 buckets, 6 entries, load 0.75          16 buckets, every entry moved
  ┌───┬───┬───┬───┬───┬───┬───┬───┐   ──►  ┌───┬───┬───┬───┬───┬───┬───┬───┬ … ┐
```

That is O(n), which is why insert is **O(1) amortised** rather than O(1) outright —
the same amortisation an array's `Add` has, for the same reason.

Note the table never shrinks. Removing entries lowers the load factor but keeps the
bucket array.

## Cache behaviour

Worse than it looks. A lookup does:

```
hash the key            arithmetic, cheap
read buckets[i]         one cache miss — random index into a big array
follow the chain        another miss per entry, scattered anywhere
compare the key         another miss — the key object is elsewhere again
```

Three or four potential misses for one "O(1)" lookup. This is why **for small n a
linear scan of an array often beats a hash lookup** — the scan is a handful of
cache-resident comparisons, while the hash costs random jumps. The crossover is usually
somewhere in the tens of elements.

## Collision strategies

**Separate chaining** (drawn above) — each bucket heads a linked list. Simple, degrades
gracefully, but every entry is its own object.

**Open addressing** — no chains; on collision, probe for another empty slot in the same
array. More compact and far more cache-friendly, but deletion is awkward (you need
tombstones) and it degrades badly at high load.

## What .NET actually ships

`Dictionary<TKey,TValue>` does **not** use per-bucket linked node objects. It uses two
parallel arrays — a `buckets` array of indices and an `entries` array of structs
holding `hashCode`, `next`, `key`, `value`. Chains are index links inside one
contiguous block, which is much more cache-friendly than real pointer chasing.

Also worth knowing:

- It grows to a **prime** bucket count, which spreads keys better than powers of two.
- `HashSet<T>` is the same machinery with no value.
- Insertion order is **not** preserved and must never be relied on.
- `string.GetHashCode()` is randomised per process by default — a deliberate defence
  against collision attacks, and the reason hash codes must never be persisted.

## Summary

| Fact | Consequence |
| --- | --- |
| Key → index by arithmetic | no search → **O(1) average** |
| Collisions chain | chain length is the real cost |
| All keys can collide | **O(n) worst case** — a linked list |
| Load factor triggers resize | rehash everything → O(1) **amortised** insert |
| Entries are separate objects | ~40 B each, scattered, several cache misses per lookup |
| Buckets hold references | no ordering, so no sorted iteration, no range queries |

Compare: [arrays](../01-Arrays/Memory.md) · [linked lists](../02-LinkedLists/Memory.md) ·
[trees](../06-Trees/Memory.md) — a BST is slower per lookup but keeps data sorted, which
a hash table cannot do at all.
