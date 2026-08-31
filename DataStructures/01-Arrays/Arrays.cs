#!/usr/bin/env dotnet
// =============================================================================
// ARRAYS — how they are stored in memory, and what every operation costs.
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run Arrays.cs                  everything
//   dotnet run Arrays.cs -- memory        how it sits in memory
//   dotnet run Arrays.cs -- ops           each operation, and what it cost
//   dotnet run Arrays.cs -- complexity    measured growth curves
//
// Nothing here is shared or factored out. Every method carries its own code from
// top to bottom — its own banner, its own table, its own array-drawing loop —
// even where that repeats. You can read any single method start to finish and
// see the whole story without jumping somewhere else.
//
// The lines below are "top-level statements": C# compiles them into a hidden
// Main, so there is no Main to write and no project to create. They have to come
// before any type declaration, which is why the class sits at the bottom.
// =============================================================================

using System.Runtime.CompilerServices;

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowGetCost();
    ShowSearchCost();
    ShowRemoveCost();
    ShowAddCost();
    ShowInsertCost();
}


// =============================================================================
// HOW AN ARRAY IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW AN ARRAY IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      Your variable is a reference on the stack. It points at ONE object on the
      heap that holds a header followed by every element, back to back:

        STACK                       HEAP
        ┌──────────────┐            ┌──────────┬──────────┬────────┬────┬────┬────┬────┐
        │ items        │            │ sync blk │ method   │ length │ 10 │ 20 │ 30 │ 40 │
        │ 0x7f9a2c00 ──┼───────────►│ 8 bytes  │ table 8B │ 4, 8B  │ 4B │ 4B │ 4B │ 4B │
        └──────────────┘            └──────────┴──────────┴────────┴────┴────┴────┴────┘
          8 bytes                    └─── 24 bytes of header ───┘└── the data ──┘

      So `new int[4]` costs 24 + 16 = 40 bytes, not 16. For small arrays the
      header dominates, which is why "lots of tiny arrays" is a wasteful layout.
      """);

    // Not a claim — measured. The real gap between consecutive elements.
    int[] probe = [10, 20, 30, 40, 50, 60, 70, 80];
    nint stride = Unsafe.ByteOffset(ref probe[0], ref probe[1]);

    Console.WriteLine();
    Console.WriteLine("  MEASURED, not assumed:");
    Console.WriteLine($"    bytes between element 0 and 1 : {stride}");
    Console.WriteLine($"    bytes between element 0 and 5 : {Unsafe.ByteOffset(ref probe[0], ref probe[5])}  (= 5 x {stride})");
    Console.WriteLine();

    Console.WriteLine("  That even spacing is why reading is O(1) — the machine does");
    Console.WriteLine("  arithmetic instead of searching:");
    Console.WriteLine();
    long fakeBase = 0x7f9a2c18;
    for (int i = 0; i < 4; i++)
    {
        Console.WriteLine($"    items[{i}]  ->  0x{fakeBase:x} + {i} * {stride}  =  0x{fakeBase + i * stride:x}   holds {probe[i]}");
    }
    Console.WriteLine();
    Console.WriteLine("  It costs the same for items[0] and items[9999999]. No scanning.");

    Console.WriteLine();
    Console.WriteLine("""
      VALUE TYPES SIT INSIDE THE ARRAY. REFERENCE TYPES DO NOT.

        int[4]      ┌────┬────┬────┬────┐
                    │ 10 │ 20 │ 30 │ 40 │   the numbers ARE the storage
                    └────┴────┴────┴────┘   16 bytes, one cache line

        string[4]   ┌───────┬───────┬───────┬───────┐
                    │ ptr ─┐│ ptr ─┐│ null  │ null  │   32 bytes of POINTERS...
                    └──────┼┴──────┼┴───────┴───────┘
                           ▼       ▼
                        "alice"  "bob"      ...strings live elsewhere on the heap

      An int[1000] is one dense 4KB block. A string[1000] is 8KB of pointers plus
      1000 separate objects scattered around. Same O(n) to walk, very different
      real cost — and it is why removing an item must blank the slot, or the
      array keeps the object alive forever.
      """);

    int perLine = 64 / (int)stride;
    Console.WriteLine();
    Console.WriteLine("  THE CPU NEVER LOADS ONE INT — it loads a 64-byte cache line:");
    Console.WriteLine();
    Console.Write("    ");
    for (int i = 0; i < 32; i++)
    {
        if (i > 0 && i % perLine == 0) Console.Write(" | ");
        Console.Write("#");
    }
    Console.WriteLine();
    Console.WriteLine($"    └── {perLine} ints per fetch ──┘");
    Console.WriteLine();
    Console.WriteLine("  Touch element 0 and the next 15 arrive free. A linked list gets");
    Console.WriteLine("  none of this: every node is its own allocation, so every step is a");
    Console.WriteLine("  possible cache miss. Identical Big-O, several times slower.");

    Console.WriteLine();
    Console.WriteLine("  COUNT vs CAPACITY — a growable array is a fixed array with spare room:");
    Console.WriteLine();

    Arrays a = new Arrays();
    for (int n = 1; n <= 5; n++)
    {
        a.Add(n * 10);

        // Draw the buffer, including the slots that exist but are not used yet.
        string cells = "";
        for (int i = 0; i < a.items.Length; i++)
        {
            cells += (i < a.count ? a.items[i].ToString() : "·") + " ";
        }
        Console.WriteLine($"    after Add({n * 10,2})   [ {cells}]  count={a.count} capacity={a.items.Length}");
    }

    Console.WriteLine();
    Console.WriteLine("  When count reaches capacity, a BIGGER block is allocated and every");
    Console.WriteLine("  element is copied across. For that moment both blocks are alive —");
    Console.WriteLine("  that is the transient 2x memory cost — then the old one is garbage.");
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
    Console.WriteLine("  The class counts every element it touches in `steps`, so these");
    Console.WriteLine("  numbers are the real work done, not an estimate.");
    Console.WriteLine();

    Arrays a = new Arrays();
    long before;
    string cells;

    // --- nine appends --------------------------------------------------------
    before = a.steps;
    for (int i = 1; i <= 9; i++) a.Add(i * 10);
    Console.WriteLine($"    Add(10..90) — nine appends              cost {a.steps - before,6} step(s)");

    cells = "";
    for (int i = 0; i < a.items.Length; i++) cells += (i < a.count ? a.items[i].ToString() : "·") + " ";
    Console.WriteLine($"          [ {cells}]  count={a.count} capacity={a.items.Length}");
    Console.WriteLine("          Most adds cost 1. The expensive ones are the three resizes.");
    Console.WriteLine();

    // --- reading at both ends ------------------------------------------------
    before = a.steps;
    a.Get(0);
    Console.WriteLine($"    Get(0)                                  cost {a.steps - before,6} step(s)");

    before = a.steps;
    a.Get(8);
    Console.WriteLine($"    Get(8) — the far end                    cost {a.steps - before,6} step(s)");
    Console.WriteLine("          Same cost. That is O(1): position does not matter.");
    Console.WriteLine();

    // --- inserting at the front ----------------------------------------------
    before = a.steps;
    a.Insert(0, 5);
    Console.WriteLine($"    Insert(0, 5) — at the FRONT             cost {a.steps - before,6} step(s)");

    cells = "";
    for (int i = 0; i < a.items.Length; i++) cells += (i < a.count ? a.items[i].ToString() : "·") + " ";
    Console.WriteLine($"          [ {cells}]  count={a.count} capacity={a.items.Length}");
    Console.WriteLine("          Everything had to slide right to open a gap.");
    Console.WriteLine();

    // --- inserting at the end ------------------------------------------------
    before = a.steps;
    a.Insert(a.count, 100);
    Console.WriteLine($"    Insert(count, 100) — at the END         cost {a.steps - before,6} step(s)");
    Console.WriteLine("          Nothing to shift. Same method, wildly different cost.");
    Console.WriteLine();

    // --- removing from the front ---------------------------------------------
    before = a.steps;
    a.RemoveAt(0);
    Console.WriteLine($"    RemoveAt(0) — from the FRONT            cost {a.steps - before,6} step(s)");

    cells = "";
    for (int i = 0; i < a.items.Length; i++) cells += (i < a.count ? a.items[i].ToString() : "·") + " ";
    Console.WriteLine($"          [ {cells}]  count={a.count} capacity={a.items.Length}");
    Console.WriteLine("          Every survivor slid left to close the hole.");
    Console.WriteLine();

    // --- removing from the end -----------------------------------------------
    before = a.steps;
    a.RemoveAt(a.count - 1);
    Console.WriteLine($"    RemoveAt(count-1) — from the END        cost {a.steps - before,6} step(s)");
    Console.WriteLine("          Nothing moved.");
    Console.WriteLine();

    // --- searching -----------------------------------------------------------
    before = a.steps;
    a.IndexOf(30);
    Console.WriteLine($"    IndexOf(30) — a hit near the front      cost {a.steps - before,6} step(s)");

    before = a.steps;
    a.IndexOf(9999);
    Console.WriteLine($"    IndexOf(9999) — a MISS                  cost {a.steps - before,6} step(s)");
    Console.WriteLine("          A miss is the worst case: absence can only be proved by");
    Console.WriteLine("          checking every single element.");
    Console.WriteLine();

    // --- clearing ------------------------------------------------------------
    before = a.steps;
    a.Clear();
    Console.WriteLine($"    Clear()                                 cost {a.steps - before,6} step(s)");

    cells = "";
    for (int i = 0; i < a.items.Length; i++) cells += (i < a.count ? a.items[i].ToString() : "·") + " ";
    Console.WriteLine($"          [ {cells}]  count={a.count} capacity={a.items.Length}");
    Console.WriteLine("          Count is 0 but the buffer is kept, so refilling is free.");
}


// =============================================================================
// COMPLEXITY — one method per operation, each measuring itself
//
// How to read a growth curve without any theory: double n and watch the work.
//   stays the same -> O(1)        doubles    -> O(n)
//   creeps up      -> O(log n)    quadruples -> O(n^2)
// =============================================================================

void ShowGetCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Get(i), a single read");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Arrays a = new Arrays();
        for (int i = 0; i < sizes[s]; i++) a.Add(i);

        a.steps = 0;
        a.Get(sizes[s] / 2);
        work[s] = a.steps;
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
    Console.WriteLine("  One step no matter how big the array is. The machine computes");
    Console.WriteLine("  start + index * 4 and reads it — there is nothing to scan.");
}

void ShowSearchCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — IndexOf(missing), a single failed search");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Arrays a = new Arrays();
        for (int i = 0; i < sizes[s]; i++) a.Add(i);

        a.steps = 0;
        a.IndexOf(-1);
        work[s] = a.steps;
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
    Console.WriteLine("  Exactly n. The data is in insertion order, not sorted order, so");
    Console.WriteLine("  proving a value is ABSENT means checking every single element.");
}

void ShowRemoveCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — RemoveAt(0), one removal from the front");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Arrays a = new Arrays();
        for (int i = 0; i < sizes[s]; i++) a.Add(i);

        a.steps = 0;
        a.RemoveAt(0);
        work[s] = a.steps;
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
    Console.WriteLine("  A contiguous block cannot have a hole, so every survivor slides");
    Console.WriteLine("  one slot left. Removing the LAST element would cost 1.");
}

void ShowAddCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Add(x), n appends, total elements copied");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Arrays a = new Arrays();
        for (int i = 0; i < sizes[s]; i++) a.Add(i);

        work[s] = a.copies;
        if (work[s] > max) max = work[s];
    }

    Console.WriteLine("         n         work    ratio    per-op   reading");
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
        string perOp = ((double)work[s] / sizes[s]).ToString("0.00").PadLeft(9);
        Console.WriteLine($"   {sizes[s],7}{work[s],13}{ratio}{perOp}   {reading}");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * work[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}");
    }

    Console.WriteLine();
    Console.WriteLine("  Total copying grows with n, but the PER-OP column stays flat near 1.");
    Console.WriteLine("  That flat column is what \"amortised O(1)\" means: individual adds can");
    Console.WriteLine("  cost O(n) when they resize, but doubling makes resizes exponentially");
    Console.WriteLine("  rarer, so reaching n copies about 2n elements in total.");
}

void ShowInsertCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Insert(0, x), n front-inserts, total elements copied");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Arrays a = new Arrays();
        for (int i = 0; i < sizes[s]; i++) a.Insert(0, i);

        work[s] = a.copies;
        if (work[s] > max) max = work[s];
    }

    Console.WriteLine("         n         work    ratio    per-op   reading");
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
        string perOp = ((double)work[s] / sizes[s]).ToString("0.00").PadLeft(9);
        Console.WriteLine($"   {sizes[s],7}{work[s],13}{ratio}{perOp}   {reading}");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * work[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}");
    }

    Console.WriteLine();
    Console.WriteLine("  Work QUADRUPLES when n doubles, and the per-op column grows with n.");
    Console.WriteLine("  Each insert is O(n), so n of them is O(n^2). Compare Add above:");
    Console.WriteLine("  same class, same data, 8000x the work — the position is everything.");
    Console.WriteLine();
    Console.WriteLine("  The headline: an array is cheap at the END and expensive at the FRONT.");
    Console.WriteLine();
}


// =============================================================================
// THE CLASS
//
// A growable array of ints — this is what List<T> is, written out longhand.
// Every operation carries its own code from top to bottom, even where that
// repeats the resize block, so you can read any single method and see the
// complete story of what it costs.
// =============================================================================

public class Arrays
{
    // The real storage. Elements sit in ONE contiguous block on the heap.
    public int[] items = new int[0];

    // How many slots are filled. items.Length is how many exist.
    // count <= items.Length, always.
    public int count = 0;

    // Every method bumps `steps` once per element it touches. That turns an
    // abstract complexity into a number you can print, compare and plot.
    public long steps = 0;
    public long copies = 0;
    public int resizes = 0;

    // -------------------------------------------------------------------------
    // Add — put a value on the end.
    //
    // O(1) amortised. Almost always a single write into a free slot. When the
    // buffer is full we pay O(n) to move everything into a bigger one, but
    // doubling makes that rarer and rarer, so it averages out to a constant.
    // -------------------------------------------------------------------------
    public void Add(int value)
    {
        if (count == items.Length)
        {
            // DOUBLING is the whole trick. A fixed +4 would make n adds cost
            // about n^2/8 copies overall instead of about 2n.
            int newCapacity = items.Length == 0 ? 4 : items.Length * 2;
            int[] bigger = new int[newCapacity];

            // This loop is the O(n) part, and while it runs BOTH buffers are
            // alive — the transient 2x memory cost.
            for (int i = 0; i < count; i++)
            {
                bigger[i] = items[i];
                copies++;
                steps++;
            }

            items = bigger;
            resizes++;
        }

        // The common case: one write. No scanning, no shifting.
        items[count] = value;
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // Get — read the value at a position.
    //
    // O(1). The CPU computes start + index * 4 and reads it. Ten elements or ten
    // million, the cost is identical. That is what contiguous memory buys.
    // -------------------------------------------------------------------------
    public int Get(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count - 1}");
        }

        steps++;
        return items[index];
    }

    // -------------------------------------------------------------------------
    // Set — overwrite the value at a position.
    //
    // O(1), same reason as Get. It can only overwrite an existing element; it
    // never grows the array.
    // -------------------------------------------------------------------------
    public void Set(int index, int value)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count - 1}");
        }

        items[index] = value;
        steps++;
    }

    // -------------------------------------------------------------------------
    // Insert — put a value at a position, pushing the rest along.
    //
    // O(n). Contiguous memory has no gaps, so room must be MADE by physically
    // moving every element from `index` onwards one slot right. At the front
    // that is all of them; at the end it is none.
    // -------------------------------------------------------------------------
    public void Insert(int index, int value)
    {
        // index == count is allowed: that just means "append".
        if (index < 0 || index > count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count}");
        }

        if (count == items.Length)
        {
            int newCapacity = items.Length == 0 ? 4 : items.Length * 2;
            int[] bigger = new int[newCapacity];
            for (int i = 0; i < count; i++)
            {
                bigger[i] = items[i];
                copies++;
                steps++;
            }

            items = bigger;
            resizes++;
        }

        // Walk BACKWARDS from the end. Forwards would overwrite each value
        // before it had been copied, smearing one element across the array.
        for (int i = count - 1; i >= index; i--)
        {
            items[i + 1] = items[i];
            copies++;
            steps++;
        }

        items[index] = value;
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // RemoveAt — take out a value and close the hole.
    //
    // O(n). A contiguous block cannot have a hole, so everything after `index`
    // slides one slot left. Removing the last element is free; the first is the
    // worst case.
    // -------------------------------------------------------------------------
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} is outside 0..{count - 1}");
        }

        // Walk FORWARDS this time, pulling each survivor back one slot.
        for (int i = index; i < count - 1; i++)
        {
            items[i] = items[i + 1];
            copies++;
            steps++;
        }

        count--;

        // Blank the vacated slot. For ints this is only tidiness — but the same
        // line on a string[] is what lets the GC reclaim the object. A stale
        // reference sitting past `count` would keep it alive forever.
        items[count] = 0;
        steps++;
    }

    // -------------------------------------------------------------------------
    // IndexOf — find a value, or -1 if it is absent.
    //
    // O(n). The values are in insertion order, not sorted order, so there is no
    // way to be clever. A value that is NOT here can only be ruled out by
    // checking all n of them — which is exactly why hash tables exist.
    // -------------------------------------------------------------------------
    public int IndexOf(int value)
    {
        for (int i = 0; i < count; i++)
        {
            steps++;
            if (items[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    // -------------------------------------------------------------------------
    // Clear — empty the array.
    //
    // O(n) to blank the slots, but the buffer is KEPT, so capacity stays where
    // it was and refilling costs no reallocations.
    // -------------------------------------------------------------------------
    public void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            items[i] = 0;
            steps++;
        }

        count = 0;
    }
}
