#!/usr/bin/env dotnet
// =============================================================================
// LINKED LISTS — scattered nodes joined by addresses. The opposite of an array.
//
//   dotnet run LinkedLists.cs                  everything
//   dotnet run LinkedLists.cs -- memory        how it sits in memory
//   dotnet run LinkedLists.cs -- ops           each operation, and what it cost
//   dotnet run LinkedLists.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code from top
// to bottom, so you can read any one of them start to finish.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowGetCost();
    ShowAddFirstCost();
    ShowAddLastCost();
}


// =============================================================================
// HOW A LINKED LIST IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A LINKED LIST IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      There is no block. Each element is its OWN heap object, and the only thing
      joining them is an address stored inside the previous one:

        STACK              HEAP — nodes anywhere, in any order
        ┌──────────┐
        │ head ────┼──►┌────────────┐    ┌────────────┐    ┌────────────┐
        └──────────┘   │ hdr   16 B │ ┌─►│ hdr   16 B │ ┌─►│ hdr   16 B │
                       │ value  10  │ │  │ value  20  │ │  │ value  30  │
                       │ next ──────┼─┘  │ next ──────┼─┘  │ next = null│
                       └────────────┘    └────────────┘    └────────────┘
                          0x4a10            0x9f88            0x2c04
                          └─── the addresses have no pattern at all ───┘

      COST OF ONE NODE holding a single int:

        object header (sync block + method table)   16 bytes
        next reference                               8 bytes
        int value                                    4 bytes
        padding to an 8-byte boundary                4 bytes
        ------------------------------------------ ---------
        total                                       32 bytes

      32 bytes to store 4 bytes of data — 8x overhead. The same million ints:

        int[1000000]           ~4 MB    1 object
        linked list           ~32 MB    1,000,000 objects for the GC to track
      """);

    Console.WriteLine();
    Console.WriteLine("  WHY YOU CANNOT INDEX IT");
    Console.WriteLine();
    Console.WriteLine("    array         address(i) = start + i * 4      -> O(1) arithmetic");
    Console.WriteLine("    linked list   follow next, i times            -> O(n) pointer chase");
    Console.WriteLine();
    Console.WriteLine("  There is nothing to compute from. You must read node 0 to learn where");
    Console.WriteLine("  node 1 is, read node 1 to learn where node 2 is, and so on. Each read");
    Console.WriteLine("  DEPENDS on the previous one, so the CPU cannot prefetch or parallelise.");
    Console.WriteLine();
    Console.WriteLine("    array         ################   one fetch serves 16 ints");
    Console.WriteLine("    linked list   # .. # .. # .. #   a possible cache miss per hop");
    Console.WriteLine();
    Console.WriteLine("  This is the single most important practical fact here: identical O(n)");
    Console.WriteLine("  to an array, routinely several times slower in real life.");

    Console.WriteLine();
    Console.WriteLine("  WHAT IT BUYS: no shifting. Inserting is three writes, at any size.");
    Console.WriteLine();
    Console.WriteLine("    before   A ──► B ──► C");
    Console.WriteLine("                                     A.next = X");
    Console.WriteLine("    after    A ──► X ──► B ──► C     X.next = B");
    Console.WriteLine();
    Console.WriteLine("  But that O(1) is *given a reference to the node*. FINDING it is O(n),");
    Console.WriteLine("  and quoting the O(1) while forgetting the search is the classic error.");
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
    Console.WriteLine("  `steps` counts every node touched, so these are real costs.");
    Console.WriteLine();

    LinkedLists list = new LinkedLists();
    long before;
    string chain;

    before = list.steps;
    for (int i = 1; i <= 5; i++) list.AddFirst(i * 10);
    Console.WriteLine($"    AddFirst(10..50) — five inserts         cost {list.steps - before,6} step(s)");

    chain = "";
    for (Node? walk = list.head; walk != null; walk = walk.next) chain += walk.value + " -> ";
    Console.WriteLine($"          {chain}null   count={list.count}");
    Console.WriteLine("          One step each. The head is the only place that is free.");
    Console.WriteLine();

    before = list.steps;
    list.AddLast(99);
    Console.WriteLine($"    AddLast(99) — no tail pointer           cost {list.steps - before,6} step(s)");

    chain = "";
    for (Node? walk = list.head; walk != null; walk = walk.next) chain += walk.value + " -> ";
    Console.WriteLine($"          {chain}null   count={list.count}");
    Console.WriteLine("          Had to walk the whole list to find the end. Keeping a tail");
    Console.WriteLine("          pointer would make this O(1) — that is what a queue does.");
    Console.WriteLine();

    before = list.steps;
    list.Get(0);
    Console.WriteLine($"    Get(0) — the head                       cost {list.steps - before,6} step(s)");

    before = list.steps;
    list.Get(5);
    Console.WriteLine($"    Get(5) — the far end                    cost {list.steps - before,6} step(s)");
    Console.WriteLine("          Position DOES matter here. Compare an array, where both cost 1.");
    Console.WriteLine();

    before = list.steps;
    list.RemoveFirst();
    Console.WriteLine($"    RemoveFirst()                           cost {list.steps - before,6} step(s)");
    Console.WriteLine("          Move head to head.next. Nothing shifts, at any size.");
    Console.WriteLine();

    before = list.steps;
    list.InsertAfter(1, 77);
    Console.WriteLine($"    InsertAfter(index 1, 77)                cost {list.steps - before,6} step(s)");

    chain = "";
    for (Node? walk = list.head; walk != null; walk = walk.next) chain += walk.value + " -> ";
    Console.WriteLine($"          {chain}null   count={list.count}");
    Console.WriteLine("          The rewiring was 2 steps. The rest was WALKING to the node.");
    Console.WriteLine();

    before = list.steps;
    list.IndexOf(9999);
    Console.WriteLine($"    IndexOf(9999) — a MISS                  cost {list.steps - before,6} step(s)");
    Console.WriteLine("          Same as an array: absence means checking everything.");
    Console.WriteLine();

    before = list.steps;
    list.Reverse();
    Console.WriteLine($"    Reverse()                               cost {list.steps - before,6} step(s)");

    chain = "";
    for (Node? walk = list.head; walk != null; walk = walk.next) chain += walk.value + " -> ";
    Console.WriteLine($"          {chain}null   count={list.count}");
    Console.WriteLine("          One pass, three pointers, no extra memory. The thing an array");
    Console.WriteLine("          cannot do without copying.");
}


// =============================================================================
// COMPLEXITY — one method per operation, each measuring itself
// =============================================================================

void ShowGetCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Get(n-1), reading the last element");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        LinkedLists list = new LinkedLists();
        for (int i = 0; i < sizes[s]; i++) list.AddFirst(i);

        list.steps = 0;
        list.Get(sizes[s] - 1);
        work[s] = list.steps;
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
    Console.WriteLine("  This is the headline difference. An array does this in 1 step at any");
    Console.WriteLine("  size; a linked list walks every node. No indexing, ever.");
}

void ShowAddFirstCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — AddFirst(x), inserting at the head");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        LinkedLists list = new LinkedLists();
        for (int i = 0; i < sizes[s]; i++) list.AddFirst(i);

        list.steps = 0;
        list.AddFirst(-1);
        work[s] = list.steps;
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
    Console.WriteLine("  Flat, and NOT amortised — genuinely O(1) every single time. There is");
    Console.WriteLine("  no buffer to resize, so no occasional expensive call hiding in there.");
    Console.WriteLine("  An array's Insert(0, x) at n=16000 costs 16000 steps. This costs 2.");
}

void ShowAddLastCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — AddLast(x) with no tail pointer");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        LinkedLists list = new LinkedLists();
        for (int i = 0; i < sizes[s]; i++) list.AddFirst(i);

        list.steps = 0;
        list.AddLast(-1);
        work[s] = list.steps;
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
    Console.WriteLine("  O(n) — but only because this class has no tail pointer. Store one");
    Console.WriteLine("  extra reference to the last node and it becomes O(1), which is exactly");
    Console.WriteLine("  how a queue gets O(1) at both ends. One field changes the complexity.");
    Console.WriteLine();
}


// =============================================================================
// THE NODE
//
// One heap object per element. This is where the 32 bytes go.
// =============================================================================

public class Node
{
    public int value;
    public Node? next;

    public Node(int value)
    {
        this.value = value;
        this.next = null;
    }
}


// =============================================================================
// THE CLASS
//
// A singly linked list. Every operation carries its own walking loop rather than
// calling a shared helper, so each method shows its full cost in one place.
// =============================================================================

public class LinkedLists
{
    // The only thing we know. Everything else is found by following `next`.
    public Node? head = null;

    // Tracked by hand — there is nothing to measure the length from.
    public int count = 0;

    // Every method bumps `steps` once per node it touches.
    public long steps = 0;
    public long allocations = 0;

    // -------------------------------------------------------------------------
    // AddFirst — put a value at the head.
    //
    // O(1), genuinely, not amortised. Allocate a node, point it at the old head,
    // move head. Three writes whether the list holds 5 elements or 5 million.
    // -------------------------------------------------------------------------
    public void AddFirst(int value)
    {
        Node fresh = new Node(value);
        allocations++;

        fresh.next = head;
        head = fresh;
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // AddLast — put a value at the end.
    //
    // O(n), because this class stores no tail pointer: the only way to find the
    // last node is to walk there. Add a `tail` field and this becomes O(1).
    // -------------------------------------------------------------------------
    public void AddLast(int value)
    {
        Node fresh = new Node(value);
        allocations++;

        if (head == null)
        {
            head = fresh;
            count++;
            steps++;
            return;
        }

        // The walk. This loop is the entire O(n).
        Node walk = head;
        steps++;
        while (walk.next != null)
        {
            walk = walk.next;
            steps++;
        }

        walk.next = fresh;
        count++;
    }

    // -------------------------------------------------------------------------
    // Get — read the value at a position.
    //
    // O(n). There is no address arithmetic possible: each node's location is
    // only known from the previous node, so position i means i hops.
    // -------------------------------------------------------------------------
    public int Get(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count - 1}");
        }

        Node walk = head!;
        steps++;
        for (int i = 0; i < index; i++)
        {
            walk = walk.next!;
            steps++;
        }

        return walk.value;
    }

    // -------------------------------------------------------------------------
    // InsertAfter — put a value after the node at `index`.
    //
    // The rewiring is O(1) — two writes. The WALK to reach the node is O(n),
    // and conflating the two is the classic linked-list mistake.
    // -------------------------------------------------------------------------
    public void InsertAfter(int index, int value)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count - 1}");
        }

        Node walk = head!;
        steps++;
        for (int i = 0; i < index; i++)
        {
            walk = walk.next!;
            steps++;
        }

        // This part is O(1), at any list size.
        Node fresh = new Node(value);
        allocations++;
        fresh.next = walk.next;
        walk.next = fresh;
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // RemoveFirst — drop the head.
    //
    // O(1). Move head to head.next; the old node becomes unreachable and the GC
    // reclaims it. No shifting, unlike an array's RemoveAt(0).
    // -------------------------------------------------------------------------
    public int RemoveFirst()
    {
        if (head == null)
        {
            throw new InvalidOperationException("the list is empty");
        }

        int value = head.value;
        head = head.next;
        count--;
        steps++;
        return value;
    }

    // -------------------------------------------------------------------------
    // RemoveAt — drop the node at `index`.
    //
    // O(n) to walk, O(1) to unlink. Note it stops at index-1: to remove a node
    // you need the one BEFORE it, which is why singly linked lists are awkward
    // and why doubly linked lists exist.
    // -------------------------------------------------------------------------
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count - 1}");
        }

        if (index == 0)
        {
            head = head!.next;
            count--;
            steps++;
            return;
        }

        Node walk = head!;
        steps++;
        for (int i = 0; i < index - 1; i++)
        {
            walk = walk.next!;
            steps++;
        }

        // Skip over the doomed node. Nothing points at it now, so it is garbage.
        walk.next = walk.next!.next;
        count--;
        steps++;
    }

    // -------------------------------------------------------------------------
    // IndexOf — find a value, or -1.
    //
    // O(n), and a miss is the worst case: proving absence means visiting every
    // node. Same as an array, but each hop is a possible cache miss.
    // -------------------------------------------------------------------------
    public int IndexOf(int value)
    {
        Node? walk = head;
        int index = 0;

        while (walk != null)
        {
            steps++;
            if (walk.value == value)
            {
                return index;
            }
            walk = walk.next;
            index++;
        }

        return -1;
    }

    // -------------------------------------------------------------------------
    // Reverse — turn the list around in place.
    //
    // O(n) time, O(1) space. Needs THREE pointers: overwriting curr.next
    // destroys the only route to the rest of the list, so `next` has to be
    // saved before the link is flipped. This is the canonical interview question.
    // -------------------------------------------------------------------------
    public void Reverse()
    {
        Node? previous = null;
        Node? current = head;

        while (current != null)
        {
            Node? saved = current.next;   // save it BEFORE destroying the link
            current.next = previous;      // flip
            previous = current;           // shuffle both pointers forward
            current = saved;
            steps++;
        }

        head = previous;
    }

    // -------------------------------------------------------------------------
    // Clear — drop everything.
    //
    // O(1). Dropping the head makes the whole chain unreachable at once, so the
    // GC collects all of it. An array's Clear must blank each slot; this does not.
    // -------------------------------------------------------------------------
    public void Clear()
    {
        head = null;
        count = 0;
        steps++;
    }
}
