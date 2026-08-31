# Tries in memory

**The letters are not stored in the nodes.** The letter is the *edge* you take, and a
node's position in the tree is what identifies it.

> Read [../MEMORY-MODEL.md](../MEMORY-MODEL.md) first. This page assumes it.

Everything below is measured by the class next door. From `08-Tries/`:
`dotnet run Tries.cs -- memory`, `-- ops`, or `-- complexity` (or no argument for all three).

## The shape

Insert `car`, `cart`, `cat`, `dog`:

```
                 (root)
                /      \
              c          d
              │          │
              a          o
            /   \        │
          r       t*     g*
          │
          *  \
              t*                * = "a word ends here"
```

Walking c-a-r spells `car`. Walking c-a-r-t spells `cart` and **reuses every node of
`car`** — the shared prefix is stored once, not twice. That is the entire idea.

Note there is no `'c'` stored anywhere. `c` is *slot 2* of the root's children array;
being in that slot is what makes the node mean `c`.

## What one node costs

```
┌──────────────────────────────────────────┐
│ object header                    16 B    │
│ children array reference          8 B    │──►┌────────────────────────┐
│ isEndOfWord (bool) + padding      8 B    │   │ 26 references          │
└──────────────────────────────────────────┘   │ 24 + 26×8 = 232 bytes  │
              the node: 32 B                   └────────────────────────┘
```

| Part | Size |
| --- | --- |
| The node object | 32 B |
| Its 26-slot children array | 232 B |
| **Total per node** | **~264 bytes — for one letter** |

**A trie is fast and very memory-hungry** — the opposite trade from an array. Almost all
26 slots in every array are `null`; that emptiness is where the memory goes.

Two standard fixes:

- **`Dictionary<char, Node>` instead of a fixed array** — far smaller when nodes are
  sparse (most are), slightly slower per hop.
- **Radix tree** — compress chains of single-child nodes into one edge holding a
  substring. Big win on data with long unique suffixes.

## Where each variable in `Tries.cs` actually lives

```csharp
public class TrieNode { public TrieNode?[] children = new TrieNode?[26];
                        public bool isEndOfWord = false; }
public class Tries    { public TrieNode root = new TrieNode();
                        public int wordCount; public int nodeCount; public long steps; }
```

| Variable | Kind | Lives | Size |
| --- | --- | --- | --- |
| `t` (the local) | local | **stack** | 8 B |
| `root` | field, reference | **heap**, in the `Tries` object | 8 B — never `null`, unlike a BST's root |
| `wordCount` / `nodeCount` | fields, value types | **heap** | 4 B each |
| `steps` | field | **heap** | 8 B |
| `children` | field of `TrieNode`, reference | **heap** | 8 B → a *separate* 232-byte array |
| `isEndOfWord` | field of `TrieNode`, `bool` | **heap** | 1 B + 7 padding |
| `walk` (walking the word) | local, reference | **stack** address → **heap** node | 8 B |
| `slot = word[i] - 'a'` | local | **stack** | 4 B |

The `Tries` wrapper is only ~40 bytes, but **every node is two allocations** — the
32-byte node and its 232-byte children array — which is where the ~264 bytes per letter
comes from.

**`slot` is the whole trick, and it lives on the stack for one instruction.**
`word[i] - 'a'` converts a character into an array position, so choosing the next node
is O(1) arithmetic rather than a search through 26 candidates. The letter is never
written anywhere; it exists only as the offset you computed to get there.

## Why lookup does not depend on n

This is the headline property. Lookup walks one node per character:

```
Contains("cart")  →  4 hops, whether the trie holds 10 words or 10,000,000
```

**`n` does not appear in the complexity at all.** Cost is O(m), the length of the *key*.
Measured — `dotnet run Tries.cs -- complexity`:

```
     n     work
  1000        8
 16000        8      ← 8-letter word, 8 hops, dictionary size irrelevant
```

Compare the alternatives:

| Structure | Lookup | Note |
| --- | --- | --- |
| Trie | **O(m)** | independent of n |
| Hash set | O(m) average | must hash the whole key anyway; O(n·m) worst case |
| Balanced BST | O(m log n) | log n comparisons, each comparing up to m characters |

A miss is often **cheaper** than a hit — the walk stops the moment the path breaks, so a
wrong first letter costs one hop.

## Why `isEndOfWord` has to exist

Insert `car`, and the path for `ca` now exists. Without a flag, every prefix of every
word would falsely report as a word:

```
Contains("ca")   path exists  →  but isEndOfWord is false  →  correctly false
Contains("car")  path exists  →  isEndOfWord is true       →  true
```

The flag is what separates "a word" from "on the way to a word".

## What prefix sharing actually saves

Measured on 8-letter words:

```
     n    letters     nodes   nodes/letter   shared away
  1000       8000      6703       0.84           16%
 16000     128000     96703       0.76           24%
```

The saving **improves** as the dictionary grows, because more words collide on early
letters. But keep the constant in view: at ~264 bytes per node, saving 24% of nodes is a
real improvement on an expensive baseline. A trie is still far heavier than storing the
same strings in a `HashSet<string>`.

## What you are actually paying for

Two things a hash set cannot do at any price:

1. **Prefix queries.** `StartsWith`, and enumerating every word under a prefix — which is
   autocomplete. Hashing destroys the relationship between `car` and `cart`; a hash set
   has no idea they share anything.
2. **Lookup cost independent of dictionary size**, with no hashing of the full key and no
   collision risk.

If you never do a prefix query, you almost certainly want a hash set instead.

## Cache behaviour

Poor. Every hop is a dependent read of a separately-allocated node, and each node drags
in a 232-byte children array of which you use 8 bytes. A hash set touching one object is
often faster in wall-clock terms despite identical Big-O — the usual reminder that
complexity counts operations, not what they cost on real hardware.

## What .NET actually ships

**Nothing.** There is no `Trie<T>` in the BCL. If you need one you write it, which is
part of why it comes up in interviews. `HashSet<string>` covers exact-match lookup, and
for prefix work people usually reach for a sorted structure plus binary search, or a
purpose-built trie.

## Summary

| Fact | Consequence |
| --- | --- |
| Letter is the edge, not the payload | position in the tree identifies the node |
| Shared prefixes stored once | fewer nodes than total letters; improves with n |
| 26-slot child array per node | **~264 B per letter** — very memory-hungry |
| Walk is one node per character | **O(m)**, independent of dictionary size |
| Path exists ≠ word | needs an `isEndOfWord` flag |
| Subtree under a prefix | prefix queries and autocomplete — the reason to pay |
| Scattered node objects | poor cache behaviour; a hash set often wins on wall clock |

Compare: [hash tables](../05-HashTables/Memory.md) — faster in practice, no prefix
queries · [trees](../06-Trees/Memory.md) — the other pointer-based tree.
