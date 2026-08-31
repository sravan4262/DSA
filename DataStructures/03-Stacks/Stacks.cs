#!/usr/bin/env dotnet
// =============================================================================
// STACKS — not a new memory layout, a RESTRICTION. One end only, so every
// operation is O(1).
//
//   dotnet run Stacks.cs                  everything
//   dotnet run Stacks.cs -- memory        how it sits in memory
//   dotnet run Stacks.cs -- ops           each operation, and what it cost
//   dotnet run Stacks.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowPushCost();
    ShowPopCost();
}


// =============================================================================
// HOW A STACK IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A STACK IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      A stack is an ARRAY plus one rule: only ever touch the end.

        STACK                HEAP
        ┌──────────┐         ┌──────────────────────────────────────┐
        │ stack ───┼────────►│ Stacks   items ──┐   count = 4       │
        └──────────┘         └──────────────────┼───────────────────┘
                                                ▼
                             ┌────┬────┬────┬────┬────┬────┬────┬────┐
                             │ 10 │ 20 │ 30 │ 40 │  · │  · │  · │  · │
                             └────┴────┴────┴────┴────┴────┴────┴────┘
                               0    1    2    3    ▲
                                                count = 4
                                     push and pop happen HERE ─┘

      That is the whole idea. An array is expensive at the FRONT and free at the
      END, so a stack simply never touches the front. Nothing shifts, ever, which
      is why no O(n) hides anywhere in the API.

        push  ->  items[count] = value; count++      one write
        pop   ->  count--; return items[count]       one read

      Memory characteristics are exactly the array's: 24 bytes of header plus
      n x sizeof(T), contiguous, cache-friendly, doubling growth.
      """);

    Console.WriteLine();
    Console.WriteLine("  THE TOP IS ALWAYS CACHE-HOT. You just touched it, so it is sitting in");
    Console.WriteLine("  L1 — which makes array-backed stacks far faster than O(1) alone implies.");
    Console.WriteLine();

    Stacks demo = new Stacks();
    for (int n = 1; n <= 5; n++)
    {
        demo.Push(n * 10);

        string cells = "";
        for (int i = 0; i < demo.items.Length; i++)
        {
            cells += (i < demo.count ? demo.items[i].ToString() : "·") + " ";
        }
        Console.WriteLine($"    after Push({n * 10,2})   [ {cells}]  count={demo.count} capacity={demo.items.Length}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      THE OTHER STACK — the call stack is literally this structure. Each method
      call pushes a frame of parameters, locals and a return address; returning
      pops it.

              ┌────────────────────┐  ← stack pointer
              │ Recurse(3)  n=3    │
              │ Recurse(2)  n=2    │
              │ Recurse(1)  n=1    │
              │ Main()             │
              └────────────────────┘  grows down, ~1 MB per thread

      Two consequences you will be asked about:

        - recursion depth is bounded; too deep throws StackOverflowException,
          which CANNOT be caught and kills the process
        - a recursive algorithm's SPACE complexity is its maximum depth. A
          recursive tree traversal is O(h) space even allocating nothing.

      Converting recursion to an explicit Stack<T> moves those frames onto the
      heap, where you have gigabytes instead of one megabyte.
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

    Stacks stack = new Stacks();
    long before;
    string cells;

    before = stack.steps;
    for (int i = 1; i <= 9; i++) stack.Push(i * 10);
    Console.WriteLine($"    Push(10..90) — nine pushes              cost {stack.steps - before,6} step(s)");

    cells = "";
    for (int i = 0; i < stack.items.Length; i++) cells += (i < stack.count ? stack.items[i].ToString() : "·") + " ";
    Console.WriteLine($"          [ {cells}]  count={stack.count} capacity={stack.items.Length}");
    Console.WriteLine("          Most pushes cost 1. The extra came from three resizes.");
    Console.WriteLine();

    before = stack.steps;
    int top = stack.Peek();
    Console.WriteLine($"    Peek() = {top}                            cost {stack.steps - before,6} step(s)");
    Console.WriteLine("          Reads items[count-1]. Never scans, never shifts.");
    Console.WriteLine();

    before = stack.steps;
    stack.Pop();
    Console.WriteLine($"    Pop()                                   cost {stack.steps - before,6} step(s)");

    cells = "";
    for (int i = 0; i < stack.items.Length; i++) cells += (i < stack.count ? stack.items[i].ToString() : "·") + " ";
    Console.WriteLine($"          [ {cells}]  count={stack.count} capacity={stack.items.Length}");
    Console.WriteLine("          The slot was blanked — on a reference type that is what lets");
    Console.WriteLine("          the GC reclaim the popped object.");
    Console.WriteLine();

    before = stack.steps;
    for (int i = 0; i < 8; i++) stack.Pop();
    Console.WriteLine($"    Pop() x8 — drain it                     cost {stack.steps - before,6} step(s)");
    Console.WriteLine($"          count={stack.count} capacity={stack.items.Length}");
    Console.WriteLine("          Capacity stays. Emptying a stack never shrinks the buffer.");
    Console.WriteLine();

    Console.WriteLine("    Pop() on an empty stack:");
    try
    {
        stack.Pop();
    }
    catch (InvalidOperationException error)
    {
        Console.WriteLine($"          threw InvalidOperationException — \"{error.Message}\"");
    }
    Console.WriteLine("          The empty case is the edge every stack test must cover.");
    Console.WriteLine();

    Console.WriteLine("    LIFO in one line — push 1,2,3 then drain:");
    Stacks order = new Stacks();
    order.Push(1);
    order.Push(2);
    order.Push(3);
    string drained = "";
    while (order.count > 0) drained += order.Pop() + " ";
    Console.WriteLine($"          {drained} — last in, first out");
}


// =============================================================================
// COMPLEXITY — one method per operation, each measuring itself
// =============================================================================

void ShowPushCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Push(x), n pushes, total elements copied");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Stacks stack = new Stacks();
        for (int i = 0; i < sizes[s]; i++) stack.Push(i);

        work[s] = stack.copies;
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
    Console.WriteLine("  Per-op flat near 1 — amortised O(1). The only cost is the occasional");
    Console.WriteLine("  doubling, exactly as in an array, because a stack IS an array.");
}

void ShowPopCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Pop(), one removal from the top");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Stacks stack = new Stacks();
        for (int i = 0; i < sizes[s]; i++) stack.Push(i);

        stack.steps = 0;
        stack.Pop();
        work[s] = stack.steps;
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
    Console.WriteLine("  Flat, worst case, no amortisation needed. Compare an array's");
    Console.WriteLine("  RemoveAt(0), which is O(n) — the restriction is doing all the work.");
    Console.WriteLine();
}


// =============================================================================
// THE CLASS
//
// An array-backed stack, which is what .NET's Stack<T> is. Every operation
// carries its own code, including its own resize block.
// =============================================================================

public class Stacks
{
    // The same contiguous block an array uses.
    public int[] items = new int[0];

    // How many slots are filled. count-1 is the index of the top.
    public int count = 0;

    public long steps = 0;
    public long copies = 0;
    public int resizes = 0;

    // -------------------------------------------------------------------------
    // Push — put a value on the top.
    //
    // O(1) amortised. Writes at index `count` — the free slot at the END, which
    // is the one place an array is cheap. Nothing shifts.
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
                steps++;
            }

            items = bigger;
            resizes++;
        }

        items[count] = value;
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // Pop — take the top value off.
    //
    // O(1) worst case — no amortisation involved, because removing never
    // reallocates. Contrast an array's RemoveAt(0), which shifts everything.
    // -------------------------------------------------------------------------
    public int Pop()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("the stack is empty");
        }

        count--;
        int value = items[count];

        // Blank the vacated slot. For ints this is tidiness; on a Stack<string>
        // this line is what lets the GC reclaim the popped object. Without it
        // the buffer still references it and it stays alive forever.
        items[count] = 0;
        steps++;

        return value;
    }

    // -------------------------------------------------------------------------
    // Peek — look at the top without removing it.
    //
    // O(1). One read at items[count-1]. The top is also whatever you touched
    // most recently, so it is almost always still in L1 cache.
    // -------------------------------------------------------------------------
    public int Peek()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("the stack is empty");
        }

        steps++;
        return items[count - 1];
    }

    // -------------------------------------------------------------------------
    // Clear — empty the stack.
    //
    // O(n) to blank the slots so referenced objects can be collected. The buffer
    // itself is kept, so refilling costs no reallocations.
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
