#!/usr/bin/env dotnet
// =============================================================================
// UNION-FIND — a forest of trees stored in ONE flat int array, answering "are
// these two things connected?" in almost constant time.
//
//   dotnet run UnionFind.cs                  everything
//   dotnet run UnionFind.cs -- memory        how it sits in memory
//   dotnet run UnionFind.cs -- ops           each operation, and what it cost
//   dotnet run UnionFind.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowNaiveFindCost();
    ShowOptimisedFindCost();
}


// =============================================================================
// HOW UNION-FIND IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW UNION-FIND IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      A forest of trees, with no node objects and no pointers. ONE int array,
      where parent[i] holds the index of i's parent:

        index    0    1    2    3    4    5    6
              ┌────┬────┬────┬────┬────┬────┬────┐
        parent│  0 │  0 │  1 │  3 │  3 │  5 │  5 │
              └────┴────┴────┴────┴────┴────┴────┘

        parent[i] == i means i is a ROOT — the name of its whole group.

               0            3            5
               │          ┌─┘ └─┐        │ └─┐
               1          4     ...      6   ...
               │
               2

      Three separate groups: {0,1,2}, {3,4}, {5,6}. To ask whether two things are
      connected, walk each one up to its root and compare the roots.

      COST. One int per element, in one contiguous block:

        int[1000000]  parent   ~4 MB
        int[1000000]  rank     ~4 MB
        ----------------------------
        total                  ~8 MB    for a million elements

      Compare a node-based forest at 40 bytes each: 40 MB. And this version is
      contiguous, so the walks are cache-friendly. Storing a tree in a flat array
      is the same trick a heap uses — the shape lives in the numbers, not in
      pointers.
      """);

    Console.WriteLine();
    Console.WriteLine("  WATCH GROUPS MERGE:");
    Console.WriteLine();

    UnionFind demo = new UnionFind(7);
    Console.WriteLine("    start          " + demo.Picture() + "   every element alone");

    demo.Union(0, 1);
    Console.WriteLine("    Union(0,1)     " + demo.Picture());
    demo.Union(1, 2);
    Console.WriteLine("    Union(1,2)     " + demo.Picture());
    demo.Union(3, 4);
    Console.WriteLine("    Union(3,4)     " + demo.Picture());
    demo.Union(5, 6);
    Console.WriteLine("    Union(5,6)     " + demo.Picture());
    demo.Union(0, 3);
    Console.WriteLine("    Union(0,3)     " + demo.Picture() + "   two groups became one");

    Console.WriteLine();
    Console.WriteLine("""
      THE TWO OPTIMISATIONS, and they matter enormously:

      1. UNION BY RANK — always hang the SHORTER tree under the taller one. Do it
         the other way round and the tree grows a level every time, degenerating
         into a linked list exactly like an unbalanced BST.

      2. PATH COMPRESSION — after walking i up to its root, point every node on
         that path DIRECTLY at the root. The next Find on any of them is one hop.
         The structure literally flattens itself as you use it.

              before compression        after Find(4)
                    0                        0
                    │                     ┌──┼──┐
                    1                     1  2  4
                    │
                    2
                    │
                    4

      Together these give O(alpha(n)) — the inverse Ackermann function, which is
      below 5 for any n you will ever have. Effectively constant, but genuinely
      not O(1), which is why the honest answer is "almost constant".
      """);
}


// =============================================================================
// EVERY OPERATION, AND WHAT IT COST
// =============================================================================

void ShowOperations()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  EVERY OPERATION, AND WHAT IT COST");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  `steps` counts every parent hop.");
    Console.WriteLine();

    UnionFind uf = new UnionFind(10);
    long before;

    before = uf.steps;
    uf.Union(0, 1);
    uf.Union(2, 3);
    uf.Union(4, 5);
    Console.WriteLine($"    Union x3 — three pairs                  cost {uf.steps - before,6} step(s)");
    Console.WriteLine($"          {uf.Picture()}  groups={uf.groups}");
    Console.WriteLine();

    before = uf.steps;
    bool same = uf.Connected(0, 1);
    Console.WriteLine($"    Connected(0,1) = {same}                   cost {uf.steps - before,6} step(s)");

    before = uf.steps;
    bool apart = uf.Connected(0, 3);
    Console.WriteLine($"    Connected(0,3) = {apart}                  cost {uf.steps - before,6} step(s)");
    Console.WriteLine("          Walk both up to their roots and compare. Different roots means");
    Console.WriteLine("          different groups — no searching, no scanning.");
    Console.WriteLine();

    before = uf.steps;
    uf.Union(1, 3);
    Console.WriteLine($"    Union(1,3) — merge two groups           cost {uf.steps - before,6} step(s)");
    Console.WriteLine($"          {uf.Picture()}  groups={uf.groups}");
    Console.WriteLine($"          Connected(0,3) is now {uf.Connected(0, 3)} — one link joined four elements.");
    Console.WriteLine();

    before = uf.steps;
    uf.Union(0, 1);
    Console.WriteLine($"    Union(0,1) — already together           cost {uf.steps - before,6} step(s)");
    Console.WriteLine($"          groups still {uf.groups}. Merging a group with itself is a no-op —");
    Console.WriteLine("          and in a cycle-detection algorithm, that no-op IS the cycle.");
    Console.WriteLine();

    Console.WriteLine("    PATH COMPRESSION IN ACTION — build a deliberately deep chain:");
    Console.WriteLine();

    UnionFind chain = new UnionFind(8);
    chain.useRank = false;              // force a tall, thin tree
    for (int i = 1; i < 8; i++) chain.Union(i, i - 1);
    Console.WriteLine($"          {chain.Picture()}");

    chain.usePathCompression = false;
    chain.steps = 0;
    chain.Find(0);
    long uncompressed = chain.steps;

    chain.usePathCompression = true;
    chain.steps = 0;
    chain.Find(0);
    long firstCompressed = chain.steps;

    chain.steps = 0;
    chain.Find(0);
    long secondTime = chain.steps;

    Console.WriteLine($"          Find(0) without compression        {uncompressed,3} hops");
    Console.WriteLine($"          Find(0) with compression           {firstCompressed,3} hops  (same walk...)");
    Console.WriteLine($"          Find(0) again                      {secondTime,3} hops  (...but now flat)");
    Console.WriteLine($"          {chain.Picture()}");
    Console.WriteLine();
    Console.WriteLine("          The first Find paid the full walk AND rewired every node it");
    Console.WriteLine("          passed to point straight at the root. Every later Find is one");
    Console.WriteLine("          hop. The structure optimises itself by being used.");
}


// =============================================================================
// COMPLEXITY — the same Find, naive and optimised
// =============================================================================

void ShowNaiveFindCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Find(x) with NO optimisations");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Always hang the second tree under the first, never compress paths.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        UnionFind uf = new UnionFind(sizes[s]);
        uf.useRank = false;
        uf.usePathCompression = false;

        // Chain them: each union makes the tree one level taller.
        for (int i = 1; i < sizes[s]; i++) uf.Union(i, i - 1);

        // Union(i, i-1) leaves the LAST index as the root, so probe index 0 —
        // the deepest node, and the only one that shows the real path length.
        uf.steps = 0;
        uf.Find(0);
        work[s] = uf.steps;
        if (work[s] > max) max = work[s];
    }

    Console.WriteLine("         n         work    ratio   reading");
    for (int s = 0; s < sizes.Length; s++)
    {
        string ratio = "   -  ";
        string reading = "";
        if (s > 0 && work[s - 1] > 0)
        {
            double r = (double)work[s] / work[s - 1];
            ratio = r.ToString("0.00").PadLeft(6);
            reading = r < 1.02 ? "flat        -> O(1)"
                    : r < 1.5 ? "creeping    -> O(log n)"
                    : r < 2.4 ? "doubling    -> O(n)"
                    : r < 3.2 ? "doubling+   -> O(n log n)"
                              : "quadrupling -> O(n^2)";
        }
        Console.WriteLine($"   {sizes[s],7}{work[s],13}{ratio}   {reading}");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * work[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}");
    }

    Console.WriteLine();
    Console.WriteLine("  O(n). The forest has become one long chain — the same degeneration an");
    Console.WriteLine("  unbalanced BST suffers, for the same reason.");
}

void ShowOptimisedFindCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Find(x) with union by rank + path compression");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Identical unions, identical data. Two small rules added.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        UnionFind uf = new UnionFind(sizes[s]);
        for (int i = 1; i < sizes[s]; i++) uf.Union(i, i - 1);

        // Union(i, i-1) leaves the LAST index as the root, so probe index 0 —
        // the deepest node, and the only one that shows the real path length.
        uf.steps = 0;
        uf.Find(0);
        work[s] = uf.steps;
        if (work[s] > max) max = work[s];
    }

    Console.WriteLine("         n         work    ratio   reading");
    for (int s = 0; s < sizes.Length; s++)
    {
        string ratio = "   -  ";
        string reading = "";
        if (s > 0 && work[s - 1] > 0)
        {
            double r = (double)work[s] / work[s - 1];
            ratio = r.ToString("0.00").PadLeft(6);
            reading = r < 1.02 ? "flat        -> O(1)"
                    : r < 1.5 ? "creeping    -> O(log n)"
                    : r < 2.4 ? "doubling    -> O(n)"
                    : r < 3.2 ? "doubling+   -> O(n log n)"
                              : "quadrupling -> O(n^2)";
        }
        Console.WriteLine($"   {sizes[s],7}{work[s],13}{ratio}   {reading}");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * work[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}");
    }

    Console.WriteLine();
    Console.WriteLine("  Flat, and tiny. At n=16000 the naive version walks thousands of hops;");
    Console.WriteLine("  this one walks a couple. Two rules, an entire complexity class apart.");
    Console.WriteLine();
    Console.WriteLine("  Formally O(alpha(n)) — inverse Ackermann, under 5 for any n you will");
    Console.WriteLine("  ever see. \"Almost constant\" is the honest phrasing, not O(1).");
    Console.WriteLine();
}


// =============================================================================
// THE CLASS
//
// A disjoint-set forest in two flat int arrays. Every operation carries its own
// walking loop rather than calling a shared helper.
// =============================================================================

public class UnionFind
{
    // parent[i] is the index of i's parent. parent[i] == i means i is a root.
    public int[] parent;

    // An upper bound on each tree's height, used to keep merges shallow.
    public int[] rank;

    // How many separate groups remain. Starts at n, drops by one per real merge.
    public int groups;

    // Flags so the demos can show what each optimisation is worth.
    public bool useRank = true;
    public bool usePathCompression = true;

    public long steps = 0;

    public UnionFind(int size)
    {
        parent = new int[size];
        rank = new int[size];
        groups = size;

        // Everyone starts as their own root — n groups of one.
        for (int i = 0; i < size; i++)
        {
            parent[i] = i;
            rank[i] = 0;
        }
    }

    // -------------------------------------------------------------------------
    // Find — which group does x belong to? Returns the root index.
    //
    // O(alpha(n)) with path compression — almost constant. Walk up until a node
    // is its own parent, then REWIRE every node on that path to point straight
    // at the root, so the next call is a single hop.
    // -------------------------------------------------------------------------
    public int Find(int x)
    {
        // Walk up to the root.
        int root = x;
        while (parent[root] != root)
        {
            root = parent[root];
            steps++;
        }

        // Path compression: point everything we just walked past directly at the
        // root. This is what flattens the tree as a side effect of reading it.
        if (usePathCompression)
        {
            int walk = x;
            while (parent[walk] != root)
            {
                int next = parent[walk];
                parent[walk] = root;
                walk = next;
            }
        }

        return root;
    }

    // -------------------------------------------------------------------------
    // Union — merge the groups containing a and b.
    //
    // O(alpha(n)). Find both roots; if they match, they were already together
    // and nothing happens. Otherwise hang the SHORTER tree under the taller one
    // so the result stays shallow.
    // -------------------------------------------------------------------------
    public bool Union(int a, int b)
    {
        int rootA = a;
        while (parent[rootA] != rootA)
        {
            rootA = parent[rootA];
            steps++;
        }

        int rootB = b;
        while (parent[rootB] != rootB)
        {
            rootB = parent[rootB];
            steps++;
        }

        if (usePathCompression)
        {
            int walk = a;
            while (parent[walk] != rootA)
            {
                int next = parent[walk];
                parent[walk] = rootA;
                walk = next;
            }

            walk = b;
            while (parent[walk] != rootB)
            {
                int next = parent[walk];
                parent[walk] = rootB;
                walk = next;
            }
        }

        // Already the same group. In a cycle-detection algorithm this is the
        // moment you have found a cycle.
        if (rootA == rootB)
        {
            return false;
        }

        if (!useRank)
        {
            parent[rootB] = rootA;   // naive: always hang B under A
        }
        else if (rank[rootA] < rank[rootB])
        {
            parent[rootA] = rootB;   // A is shorter, so it goes underneath
        }
        else if (rank[rootA] > rank[rootB])
        {
            parent[rootB] = rootA;
        }
        else
        {
            // Equal heights: pick either, and the winner gets one level taller.
            parent[rootB] = rootA;
            rank[rootA]++;
        }

        groups--;
        return true;
    }

    // -------------------------------------------------------------------------
    // Connected — are a and b in the same group?
    //
    // O(alpha(n)). Two walks to two roots, then one comparison. No scanning of
    // any kind — which is why this beats re-running a graph traversal every time
    // you want to ask the question.
    // -------------------------------------------------------------------------
    public bool Connected(int a, int b)
    {
        int rootA = a;
        while (parent[rootA] != rootA)
        {
            rootA = parent[rootA];
            steps++;
        }

        int rootB = b;
        while (parent[rootB] != rootB)
        {
            rootB = parent[rootB];
            steps++;
        }

        return rootA == rootB;
    }

    // -------------------------------------------------------------------------
    // Picture — the raw parent array, for the demos.
    // -------------------------------------------------------------------------
    public string Picture()
    {
        string cells = "";
        for (int i = 0; i < parent.Length; i++)
        {
            cells += parent[i] + " ";
        }
        return "parent[ " + cells + "]";
    }
}
