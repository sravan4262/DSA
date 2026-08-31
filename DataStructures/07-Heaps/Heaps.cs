#!/usr/bin/env dotnet
// =============================================================================
// HEAPS — a TREE stored in a flat ARRAY, with no pointers at all. The parent /
// child relationship is pure index arithmetic.
//
//   dotnet run Heaps.cs                  everything
//   dotnet run Heaps.cs -- memory        how it sits in memory
//   dotnet run Heaps.cs -- ops           each operation, and what it cost
//   dotnet run Heaps.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowPushCost();
    ShowBuildHeapCost();
}


// =============================================================================
// HOW A HEAP IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A HEAP IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      A heap LOOKS like a binary tree:

                        ┌────┐
                        │ 10 │              always the smallest (a MIN-heap)
                        └─┬──┘
                ┌─────────┴─────────┐
              ┌─▼──┐              ┌─▼──┐
              │ 15 │              │ 20 │
              └─┬──┘              └─┬──┘
           ┌────┴────┐          ┌───┘
         ┌─▼──┐   ┌──▼─┐     ┌──▼─┐
         │ 25 │   │ 30 │     │ 40 │
         └────┘   └────┘     └────┘

      ...but it is stored as a FLAT ARRAY, with no node objects and no pointers:

        index    0     1     2     3     4     5
              ┌────┬────┬────┬────┬────┬────┐
              │ 10 │ 15 │ 20 │ 25 │ 30 │ 40 │
              └────┴────┴────┴────┴────┴────┘

      The tree shape lives in ARITHMETIC, not in memory:

        left child of i   =  2i + 1
        right child of i  =  2i + 2
        parent of i       =  (i - 1) / 2

      Check it: index 1 holds 15. Its children are at 3 and 4 — which hold 25
      and 30. Its parent is at (1-1)/2 = 0, which holds 10. Correct, with no
      pointer ever stored.

      WHY THIS IS SUCH A GOOD TRICK. Compare a pointer-based tree:

        node-based tree   40 bytes per node   scattered   cache miss per hop
        array heap         4 bytes per item   contiguous  prefetcher-friendly

      A heap of a million ints is one 4 MB block. As a node tree it would be
      40 MB across a million objects. Same structure, 10x the memory.

      THE RULE that makes it a heap: every parent <= its children. That is much
      weaker than a BST's ordering — which is exactly why it is cheap to
      maintain, and why you can find the MINIMUM instantly but cannot search.
      """);

    Console.WriteLine();
    Console.WriteLine("  WATCH IT BUILD — each push sifts UP until the parent is smaller:");
    Console.WriteLine();

    Heaps demo = new Heaps();
    int[] values = [40, 25, 30, 10, 20, 15];
    for (int i = 0; i < values.Length; i++)
    {
        demo.Push(values[i]);

        string cells = "";
        for (int k = 0; k < demo.count; k++) cells += demo.items[k] + " ";
        Console.WriteLine($"    Push({values[i],2})   [ {cells}]  min={demo.items[0]}");
    }

    Console.WriteLine();
    Console.WriteLine("  Only the ROOT is guaranteed. index 0 is the smallest, but the array is");
    Console.WriteLine("  not sorted and you have no idea which element is third-smallest without");
    Console.WriteLine("  popping. That weak guarantee is the whole trade.");
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
    Console.WriteLine("  `steps` counts every comparison and swap.");
    Console.WriteLine();

    Heaps heap = new Heaps();
    long before;
    string cells;

    before = heap.steps;
    int[] values = [40, 25, 30, 10, 20, 15, 5];
    for (int i = 0; i < values.Length; i++) heap.Push(values[i]);
    Console.WriteLine($"    Push x7                                 cost {heap.steps - before,6} step(s)");

    cells = "";
    for (int k = 0; k < heap.count; k++) cells += heap.items[k] + " ";
    Console.WriteLine($"          [ {cells}]  count={heap.count}");
    Console.WriteLine("          Each push sifts up at most log2(n) levels, not n.");
    Console.WriteLine();

    before = heap.steps;
    int min = heap.Peek();
    Console.WriteLine($"    Peek() = {min}                             cost {heap.steps - before,6} step(s)");
    Console.WriteLine("          Reads items[0]. The minimum is ALWAYS there, for free.");
    Console.WriteLine();

    before = heap.steps;
    heap.Pop();
    Console.WriteLine($"    Pop()                                   cost {heap.steps - before,6} step(s)");

    cells = "";
    for (int k = 0; k < heap.count; k++) cells += heap.items[k] + " ";
    Console.WriteLine($"          [ {cells}]  new min={heap.items[0]}");
    Console.WriteLine("          Move the LAST element to the root, then sift DOWN. Moving the");
    Console.WriteLine("          last one keeps the tree complete, which keeps the array dense.");
    Console.WriteLine();

    before = heap.steps;
    bool has = heap.Contains(30);
    Console.WriteLine($"    Contains(30) = {has}                      cost {heap.steps - before,6} step(s)");
    Console.WriteLine("          O(n) — a LINEAR SCAN. The heap rule says nothing about where a");
    Console.WriteLine("          given value is, only about parents versus children. If you need");
    Console.WriteLine("          to search, you wanted a different structure.");
    Console.WriteLine();

    Console.WriteLine("    Draining the heap gives sorted output — that is heapsort:");
    string drained = "";
    while (heap.count > 0) drained += heap.Pop() + " ";
    Console.WriteLine($"          {drained}");
    Console.WriteLine();

    Console.WriteLine("    Pop() on an empty heap:");
    try
    {
        heap.Pop();
    }
    catch (InvalidOperationException error)
    {
        Console.WriteLine($"          threw InvalidOperationException — \"{error.Message}\"");
    }
}


// =============================================================================
// COMPLEXITY — one method per operation, each measuring itself
// =============================================================================

void ShowPushCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Push(x), one insert into a heap of n");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Heaps heap = new Heaps();
        for (int i = 0; i < sizes[s]; i++) heap.Push(sizes[s] - i);

        // Push a new minimum — the worst case, sifting all the way to the root.
        heap.steps = 0;
        heap.Push(-1);
        work[s] = heap.steps;
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
    Console.WriteLine("  Look at the WORK column, not the ratio: it goes up by ONE each time n");
    Console.WriteLine("  doubles. That is the signature of O(log n) — doubling the data adds a");
    Console.WriteLine("  single extra level to the tree, so a single extra comparison.");
    Console.WriteLine("  n=16000 is 16x n=1000, but the work only grew by 4.");
}

void ShowBuildHeapCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — building a heap: n pushes vs Heapify");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Two ways to turn n loose values into a heap. They are NOT the same.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] pushWork = new long[sizes.Length];
    long[] heapifyWork = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Heaps byPush = new Heaps();
        for (int i = 0; i < sizes[s]; i++) byPush.Push(sizes[s] - i);
        pushWork[s] = byPush.steps;

        int[] raw = new int[sizes[s]];
        for (int i = 0; i < sizes[s]; i++) raw[i] = sizes[s] - i;
        Heaps byHeapify = new Heaps();
        byHeapify.Heapify(raw);
        heapifyWork[s] = byHeapify.steps;

        if (pushWork[s] > max) max = pushWork[s];
    }

    Console.WriteLine("         n     n pushes     Heapify    per-item push   per-item heapify");
    for (int s = 0; s < sizes.Length; s++)
    {
        string pushPer = ((double)pushWork[s] / sizes[s]).ToString("0.00").PadLeft(13);
        string heapPer = ((double)heapifyWork[s] / sizes[s]).ToString("0.00").PadLeft(18);
        Console.WriteLine($"   {sizes[s],7}{pushWork[s],13}{heapifyWork[s],12}{pushPer}{heapPer}");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * pushWork[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}   n pushes");
    }
    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * heapifyWork[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}   Heapify");
    }

    Console.WriteLine();
    Console.WriteLine("  n pushes is O(n log n) — the per-item column CREEPS UP as n grows.");
    Console.WriteLine("  Heapify is O(n) — its per-item column stays FLAT.");
    Console.WriteLine();
    Console.WriteLine("  Why: pushing sifts UP, so every one of the n items can travel the full");
    Console.WriteLine("  log n height. Heapify sifts DOWN from the middle backwards, and half");
    Console.WriteLine("  the nodes are leaves that cannot move at all, a quarter move one level,");
    Console.WriteLine("  an eighth move two... that series sums to O(n), not O(n log n).");
    Console.WriteLine();
    Console.WriteLine("  \"Building a heap is O(n), not O(n log n)\" is a favourite interview");
    Console.WriteLine("  question, and this is the reason.");
    Console.WriteLine();
}


// =============================================================================
// THE CLASS
//
// A binary MIN-heap in a flat array. Every operation carries its own sifting
// loop rather than calling a shared helper.
// =============================================================================

public class Heaps
{
    // The tree, flattened. No nodes, no pointers — the shape is arithmetic.
    public int[] items = new int[0];

    public int count = 0;

    public long steps = 0;
    public long copies = 0;
    public int resizes = 0;

    // -------------------------------------------------------------------------
    // Push — add a value.
    //
    // O(log n). Put it in the first free slot at the end (keeping the tree
    // complete), then SIFT UP: while it is smaller than its parent, swap. It can
    // only travel the height of the tree, which is log2(n).
    // -------------------------------------------------------------------------
    public void Push(int value)
    {
        if (count == items.Length)
        {
            int newCapacity = items.Length == 0 ? 4 : items.Length * 2;
            int[] bigger = new int[newCapacity];
            for (int i = 0; i < count; i++)
            {
                bigger[i] = items[i];
                copies++;
            }
            items = bigger;
            resizes++;
        }

        items[count] = value;
        int child = count;
        count++;

        // Sift up. Note (child - 1) / 2 is the parent — no pointer needed.
        while (child > 0)
        {
            int parent = (child - 1) / 2;
            steps++;

            if (items[parent] <= items[child])
            {
                break;   // the heap rule already holds
            }

            int swap = items[parent];
            items[parent] = items[child];
            items[child] = swap;
            child = parent;
        }
    }

    // -------------------------------------------------------------------------
    // Pop — remove and return the minimum.
    //
    // O(log n). The root is the answer. To fill the hole, move the LAST element
    // there — which keeps the tree complete and the array dense — then SIFT DOWN
    // by swapping with the smaller child until the rule holds again.
    // -------------------------------------------------------------------------
    public int Pop()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("the heap is empty");
        }

        int smallest = items[0];

        count--;
        items[0] = items[count];
        items[count] = 0;

        // Sift down. Children of i are 2i+1 and 2i+2.
        int parent = 0;
        while (true)
        {
            int left = 2 * parent + 1;
            int right = 2 * parent + 2;
            int target = parent;

            if (left < count)
            {
                steps++;
                if (items[left] < items[target]) target = left;
            }

            if (right < count)
            {
                steps++;
                if (items[right] < items[target]) target = right;
            }

            if (target == parent)
            {
                break;   // both children are larger; done
            }

            int swap = items[parent];
            items[parent] = items[target];
            items[target] = swap;
            parent = target;
        }

        return smallest;
    }

    // -------------------------------------------------------------------------
    // Peek — the minimum, without removing it.
    //
    // O(1). It is always at index 0. This is the entire selling point of a heap:
    // the best element, instantly, without sorting anything.
    // -------------------------------------------------------------------------
    public int Peek()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("the heap is empty");
        }

        steps++;
        return items[0];
    }

    // -------------------------------------------------------------------------
    // Contains — is this value anywhere in the heap?
    //
    // O(n), a plain linear scan. The heap rule only relates parents to children,
    // so it tells you NOTHING about where a particular value sits. A heap is for
    // finding the extreme, not for searching.
    // -------------------------------------------------------------------------
    public bool Contains(int value)
    {
        for (int i = 0; i < count; i++)
        {
            steps++;
            if (items[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Heapify — turn a loose array into a heap in O(n).
    //
    // Not O(n log n), which is the surprise. Start at the last PARENT and sift
    // down, walking backwards to the root. Half the nodes are leaves and cannot
    // move at all; a quarter move at most one level; an eighth at most two. That
    // series sums to less than 2n.
    // -------------------------------------------------------------------------
    public void Heapify(int[] source)
    {
        items = new int[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            items[i] = source[i];
            copies++;
        }
        count = source.Length;

        // The last parent is at (count / 2) - 1. Everything after it is a leaf.
        for (int start = count / 2 - 1; start >= 0; start--)
        {
            int parent = start;

            while (true)
            {
                int left = 2 * parent + 1;
                int right = 2 * parent + 2;
                int target = parent;

                if (left < count)
                {
                    steps++;
                    if (items[left] < items[target]) target = left;
                }

                if (right < count)
                {
                    steps++;
                    if (items[right] < items[target]) target = right;
                }

                if (target == parent)
                {
                    break;
                }

                int swap = items[parent];
                items[parent] = items[target];
                items[target] = swap;
                parent = target;
            }
        }
    }
}
