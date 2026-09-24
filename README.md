# DSA

## The roadmap

The source of truth. What to learn, in what order. Nothing starts before the things
pointing into it are done — the arrows are real prerequisites, not suggestions.

Folder numbers match this order, so the folder listing *is* the syllabus.

```
                        ┌────────────────────────┐
                        │  01 ARRAYS & HASHING   │
                        └───────────┬────────────┘
          ┌───────────┬─────────────┼─────────────┐
          ▼           ▼             ▼             ▼
    ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐
    │02 SORTING│ │03 STRINGS│ │  04 TWO   │ │ 05 STACK │
    └────┬─────┘ └────┬─────┘ │ POINTERS  │ └────┬─────┘
         │            │       └─┬───┬───┬─┘      │
         └────────────┼─────────┘   │   └────────┼──────┐
                      │             │            │      │
         ┌────────────┴──┐   ┌──────┴───┐   ┌────┴──────┴─┐
         ▼               ▼   ▼          ▼   ▼             ▼
   ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
   │ 06 BINARY    │ │ 07 SLIDING   │ │ 08 LINKED    │
   │    SEARCH    │ │    WINDOW    │ │    LIST      │
   └──────┬───────┘ └──────┬───────┘ └──────┬───────┘
          │                │                ▼
          │                │        ┌────────────────┐
          │                │        │  09 RECURSION  │
          │                │        └────────┬───────┘
          └────────────────┴─────────────────┤
                                             ▼
                                   ┌──────────────────┐
                                   │     10 TREES     │
                                   └─────────┬────────┘
             ┌───────────────────┬───────────┴───────────┐
             ▼                   ▼                       ▼
       ┌───────────┐       ┌───────────┐        ┌────────────────┐
       │ 11 TRIES  │       │ 12 HEAP   │        │13 BACKTRACKING │
       └───────────┘       └─────┬─────┘        └───────┬────────┘
              ┌──────────┬───────┴──────┐          ┌────┴─────┐
              ▼          ▼              │          ▼          ▼
      ┌────────────┐ ┌─────────┐        │   ┌───────────┐ ┌──────────┐
      │14 INTERVALS│ │15 GREEDY│        │   │ 16 GRAPHS │ │17 DP 1-D │
      └────────────┘ └─────────┘        │   └─────┬─────┘ └────┬─────┘
                                        │         ▼            │
                                        │  ┌──────────────┐    │
                                        │  │18 UNION-FIND │    │
                                        │  └──────┬───────┘    │
                                        └─────────┤            │
                                                  ▼            ▼
                                    ┌────────────────────┐ ┌──────────┐
                                    │19 ADVANCED GRAPHS  │ │20 DP 2-D │
                                    └────────────────────┘ └────┬─────┘
                                          ┌─────────────────────┤
                                          ▼                     ▼
                              ┌────────────────────┐  ┌──────────────────┐
                              │21 BIT MANIPULATION │  │22 MATH & GEOMETRY│
                              └─────────┬──────────┘  └────────▲─────────┘
                                        └──────────────────────┘
```

The arrows show the main path. Full prerequisites are on each node below as
*needs: 02, 04*.

Each node holds a handful of **algorithms** — 93 in total, listed further down. None of
them are problems. Problems live in each node's `Problems/` folder and are how you drill
an algorithm once you have built it:

```
04-TwoPointers/
├── TwoPointers.cs              ← the algorithms: ConvergingPointers, FastSlowPointers, …
└── Problems/
    ├── ThreeSum.cs             ← problems that USE them
    ├── ContainerWithMostWater.cs
    └── TrappingRainWater.cs
```

`ConvergingPointers` is the thing to learn. `ThreeSum` is one of half a dozen problems
you solve to make it stick.

---

## The process

### Step 1 — how the data structures are stored in memory. Before anything else.

This is the foundation the whole roadmap sits on, and it is the part almost everyone
skips. Complexity is not a set of facts to memorise; it is a **consequence** of the
memory layout. Once you can draw where the bytes are, every Big-O becomes obvious
instead of memorised — and you can derive it for a structure you have never seen.

| Structure | Memory guide | Implementation |
| --- | --- | --- |
| Arrays | [Memory.md](DataStructures/01-Arrays/Memory.md) | [Arrays.cs](DataStructures/01-Arrays/Arrays.cs) |
| Linked lists | [Memory.md](DataStructures/02-LinkedLists/Memory.md) | [LinkedLists.cs](DataStructures/02-LinkedLists/LinkedLists.cs) |
| Stacks | [Memory.md](DataStructures/03-Stacks/Memory.md) | [Stacks.cs](DataStructures/03-Stacks/Stacks.cs) |
| Queues | [Memory.md](DataStructures/04-Queues/Memory.md) | [Queues.cs](DataStructures/04-Queues/Queues.cs) |
| Hash tables | [Memory.md](DataStructures/05-HashTables/Memory.md) | [HashTables.cs](DataStructures/05-HashTables/HashTables.cs) |
| Trees & BSTs | [Memory.md](DataStructures/06-Trees/Memory.md) | [Trees.cs](DataStructures/06-Trees/Trees.cs) |
| Heaps | [Memory.md](DataStructures/07-Heaps/Memory.md) | [Heaps.cs](DataStructures/07-Heaps/Heaps.cs) |
| Tries | [Memory.md](DataStructures/08-Tries/Memory.md) | [Tries.cs](DataStructures/08-Tries/Tries.cs) |
| Graphs | [Memory.md](DataStructures/09-Graphs/Memory.md) | [Graphs.cs](DataStructures/09-Graphs/Graphs.cs) |
| Union-Find | [Memory.md](DataStructures/10-UnionFind/Memory.md) | [UnionFind.cs](DataStructures/10-UnionFind/UnionFind.cs) |

Start with [the memory model](DataStructures/MEMORY-MODEL.md) — stack vs heap, and how
many bytes anything costs.

For revision, [DS in a Nutshell](DataStructures/DS-IN-A-NUTSHELL.md) is all eleven
structures on one page each — operations, costs, trade-offs and when each one loses to
another — plus the Big-O and memory grids side by side.

Done with a structure when you can answer, cold: *where does it live, what does one
element cost in bytes, and which operation is expensive because of that?*

### Step 2 — for every algorithm, the same loop

Not "solve some problems tagged with it". Copy
[Algorithms/TEMPLATE.md](Algorithms/TEMPLATE.md) and fill it:

1. **Brute force first.** Write it, measure it, name the work it repeats. Never skip this
   — the optimisation is meaningless without the thing it beats, and "what's the naive
   approach?" is how most interviews open.
2. **State the idea** in two sentences, no code.
3. **Draw the visual trace** — one small input, all the way through.
4. **Implement it from scratch**, no library calls.
5. **Explain why it is faster** — the *specific* comparisons or scans that disappear,
   and why skipping them is safe.
6. **Derive the complexity**, time and space, including the recursion stack. Say which
   case you mean: *worst*, *average* and *amortised* are three different claims.
7. **Unit tests** — empty, single, duplicates, negatives, reverse, maximum constraint.
8. **Benchmark** both implementations side by side, in steps and milliseconds.
9. **Then the problems** in `Problems/`, each with its own class and guide, each carrying
   brute force *and* optimised so the comparison happens every time.

For every data structure, do the same with
[DataStructures/TEMPLATE.md](DataStructures/TEMPLATE.md): why was it invented, what
problem does it solve, strengths, weaknesses, when *not* to use it, the runtime of every
operation with a **why** column, and how it works internally.

A node is finished when its algorithms come out cold a week later.

---

## Layout

```
DataStructures/            what it is, how it works
├── TEMPLATE.md
├── MEMORY-MODEL.md        stack vs heap, and what a byte costs
├── TEMPLATE.md
├── 01-Arrays/  Memory.md · Arrays.cs
└── 02-LinkedLists/ …

Algorithms/                what you can do with it
├── TEMPLATE.md
└── 04-TwoPointers/
    ├── TwoPointers.md     the guide
    ├── TwoPointers.cs     runnable: -- trace  -- test  -- bench  -- compare
    └── Problems/
        ├── ThreeSum.md    ThreeSum.cs
        └── …
```

Algorithms are **files**, not folders — nothing is ever more than two levels deep. Nodes
holding several distinct named algorithms (Sorting, Trees, Graphs, Heap, DP, Advanced
Graphs) get one `.md`/`.cs` pair per algorithm at node level.

Folders are created when you start that algorithm; the checklist below is the map, the
repo shows real progress.

---

## The 93 algorithms

### 01 · Arrays & Hashing — *needs: nothing*
- [ ] HashMapCounting — frequency, membership, seen-before
- [ ] PrefixSums — prefix and suffix accumulation
- [ ] DifferenceArray — range updates in O(1)
- [ ] Kadane — maximum subarray
- [ ] InPlaceWritePointer — compaction without extra space

### 02 · Sorting — *needs: 01*
- [ ] InsertionSort — the O(n²) baseline everything is measured against
- [ ] MergeSort
- [ ] QuickSort
- [ ] HeapSort
- [ ] CountingSort
- [ ] RadixSort
- [ ] Quickselect

### 03 · Strings — *needs: 01*
- [ ] KMP
- [ ] RabinKarp
- [ ] Manacher

### 04 · Two Pointers — *needs: 01, 02*
- [ ] ConvergingPointers
- [ ] FastSlowPointers
- [ ] DutchNationalFlag
- [ ] MergeFromBack

### 05 · Stack — *needs: 01*
- [ ] MonotonicStack
- [ ] IterativeDFS
- [ ] AuxiliaryStateStack — min-stack and friends

### 06 · Binary Search — *needs: 02, 04*
- [ ] ClassicBinarySearch
- [ ] BoundarySearch — lower and upper bound
- [ ] BinarySearchOnAnswer

### 07 · Sliding Window — *needs: 03, 04*
- [ ] FixedWindow
- [ ] VariableWindow
- [ ] AtMostKTrick — exactly K = atMost(K) − atMost(K−1)
- [ ] MonotonicDeque

### 08 · Linked List — *needs: 04, 05*
- [ ] IterativeReversal
- [ ] DummyHead
- [ ] FloydCycleDetection

### 09 · Recursion — *needs: 08*
- [ ] CallStackModel — why depth is space complexity
- [ ] DivideAndConquer
- [ ] Memoisation

### 10 · Trees — *needs: 06, 07, 08, 09*
- [ ] DFSTraversals — pre/in/post, recursive and iterative
- [ ] BFSLevelOrder
- [ ] BSTOperations — insert, search, delete (all three cases)
- [ ] LowestCommonAncestor
- [ ] SerializeDeserialize
- [ ] MorrisTraversal — O(1) space

### 11 · Tries — *needs: 03, 10*
- [ ] TrieOperations
- [ ] TrieWithWildcard

### 12 · Heap — *needs: 10*
- [ ] SiftUpSiftDown
- [ ] BuildHeapLinear — why it is O(n), not O(n log n)
- [ ] TopKBoundedHeap
- [ ] TwoHeapsMedian
- [ ] KWayMerge

### 13 · Backtracking — *needs: 09, 10*
- [ ] BacktrackingSkeleton — choose, recurse, un-choose
- [ ] EnumerationPatterns — subsets, permutations, combinations
- [ ] PruningStrategies

### 14 · Intervals — *needs: 02, 12*
- [ ] SortAndMerge
- [ ] SweepLine

### 15 · Greedy — *needs: 02, 12*
- [ ] ExchangeArgument — how to *prove* a greedy choice is safe
- [ ] IntervalScheduling
- [ ] HuffmanCoding

### 16 · Graphs — *needs: 13*
- [ ] GraphRepresentations — adjacency list vs matrix
- [ ] BFS
- [ ] DFS
- [ ] TopologicalSort — Kahn's and DFS post-order
- [ ] CycleDetection — directed and undirected differ
- [ ] BipartiteCheck
- [ ] MultiSourceBFS

### 17 · DP 1-D — *needs: 09, 13*
- [ ] TopDownMemoisation — start here, always
- [ ] BottomUpTabulation
- [ ] LIS_Quadratic
- [ ] LIS_NLogN — patience sorting

### 18 · Union-Find — *needs: 16*
- [ ] UnionFindBasic
- [ ] PathCompressionUnionByRank

### 19 · Advanced Graphs — *needs: 12, 16, 18*
- [ ] Dijkstra
- [ ] BellmanFord
- [ ] FloydWarshall
- [ ] PrimMST
- [ ] KruskalMST
- [ ] Hierholzer — Eulerian path
- [ ] DAGShortestPath

### 20 · DP 2-D — *needs: 16, 17*
- [ ] GridDP
- [ ] LCS
- [ ] EditDistance
- [ ] Knapsack01
- [ ] UnboundedKnapsack
- [ ] IntervalDP
- [ ] StateCompression

### 21 · Bit Manipulation — *needs: 17*
- [ ] XORProperties
- [ ] BitCounting — Brian Kernighan and the DP relation
- [ ] BitmaskEnumeration
- [ ] BitwiseAddition

### 22 · Math & Geometry — *needs: 16, 20, 21*
- [ ] EuclideanGCD
- [ ] SieveOfEratosthenes
- [ ] FastExponentiation
- [ ] ModularArithmetic
- [ ] MatrixTransforms — rotation, spiral, set-zeroes in place
- [ ] OverflowSafeArithmetic

---

## Rules of the road

**Per complexity claim.** Say which case you mean. Hash lookup is O(1) **average**,
O(n) **worst**. Array append is O(1) **amortised**, O(n) **worst**. This is exactly
where follow-up questions land.

**Per problem.** Read twice and restate it · draw a concrete example by hand · say the
brute force out loud · ask what work is repeated · code it without looking · test the
edges · write down time **and** space including recursion stack · explain it aloud as if
teaching.

**Curated problem lists come last.** They are review and speed practice *after* the
roadmap — not the syllabus.

---

## Running the code

### Setting up a new machine

Every file is a single self-contained C# program with no `.csproj` or `.sln`. That needs
**.NET 10 or later**, which added running a lone `.cs` file directly.

| Install | Why | How (Windows) |
| --- | --- | --- |
| **.NET 10 SDK** (x64) | `dotnet run File.cs`. The SDK is required; a runtime alone can't build | `winget install Microsoft.DotNet.SDK.10` |
| **VS Code** | editor | [code.visualstudio.com](https://code.visualstudio.com) |
| **C# extension** (`ms-dotnettools.csharp`) | the debugger: F5, breakpoints, stepping | `code --install-extension ms-dotnettools.csharp` |

On macOS/Linux, get the SDK from [dot.net](https://dot.net) or your package manager;
the rest is the same.

**Don't install C# Dev Kit.** VS Code will suggest it. It takes over F5 with its own
project-based launcher, which ignores `.vscode/launch.json`, drops the section argument,
and made stepping hang here. The plain C# extension is all you need.

After installing, **fully quit and reopen VS Code** (not just *Reload Window*) so it
picks up the new PATH. Then check:

```bash
dotnet --version        # 10.x or later
```

**To debug:** open any `.cs` file, press **F5**, pick a section (`all` / `memory` /
`ops` / `complexity`). Breakpoints and F10/F11 work anywhere in the file. The config in
[.vscode/](.vscode/) works on whichever file is open, so it covers every file in the
repo. Keep the `.cs` tab focused when you press F5; if `launch.json` is the active tab
it will try to debug that.

### From the terminal

```bash
cd DataStructures/01-Arrays
dotnet run Arrays.cs                  # everything
dotnet run Arrays.cs -- memory        # how it is stored in memory
dotnet run Arrays.cs -- ops           # each operation and what it cost
dotnet run Arrays.cs -- complexity    # measured growth curves
```

Algorithm files follow the same shape:

```bash
dotnet run MergeSort.cs -- trace      # step by step on a small input
dotnet run MergeSort.cs -- test       # unit tests
dotnet run MergeSort.cs -- bench      # steps and milliseconds
dotnet run MergeSort.cs -- compare    # brute force vs the algorithm
```
