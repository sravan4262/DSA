#!/usr/bin/env dotnet
// =============================================================================
// DIFFERENCEARRAY — make range UPDATES O(1) by writing only where change starts
// and stops.
//
// Node 01 · Arrays & Hashing.  Needs: array, prefix sums.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The exact mirror image of PrefixSums. Read that one first — this will not land
// otherwise.
//
//   PrefixSums        makes range READS   O(1), leaves writes O(n)
//   DifferenceArray   makes range WRITES  O(1), leaves reads  O(n)
//
// The job: apply many updates of the form "add v to every element in a[l..r]",
// then look at the finished array. Doing it directly means walking the range on
// every update, so u updates over a range of length n cost O(n) each.
//
// Instead, do not store the values at all. Store only the CHANGES:
//
//   diff[l]     += v        "from index l onward, everything is v higher"
//   diff[r + 1] -= v        "from index r+1 onward, cancel that"
//
// Two writes. The range width never enters it, so updating 10 cells and
// updating 10,000,000 cells cost exactly the same.
//
// Then ONE prefix-sum pass at the very end turns the changes back into values:
//
//   a[i] = a[i - 1] + diff[i]
//
// That last line is literally the PrefixSums build loop. A difference array is
// prefix sums run backwards: you write deltas and integrate once, instead of
// writing values and differencing on demand.
//
//   brute force   walk the range, every update            O(n) per update
//   this          two writes per update + one O(n) pass   O(1) per update
//
// WHY diff IS n+1 LONG
//
// Same reason the prefix array was, and the same off-by-one trap. The slot
// diff[r+1] has to exist even when r is the last index, or "stop adding after
// the end" has nowhere to be written. For an update covering the whole array
// that cancel lands in slot n — a slot you allocate and then never read. One
// wasted slot buys a formula with no "if (r == n-1)" branch in it.
//
// WHY THE CANCEL IS AT r+1 AND NOT r
//
// The range is INCLUSIVE of r. diff[i] means "the change that begins at i", so
// the change must still be in effect AT r and stop at the next index. Writing
// the cancel at r would switch it off one cell early and quietly lose the last
// element of every range. It is the single most common bug in this algorithm.
//
// WHEN NOT TO USE IT
//
// You cannot read a value until you have integrated, and integrating is O(n).
// So this wins only when the updates are BATCHED — all of them, then one read
// pass. If you have to answer "what is a[5] right now?" between updates, you
// are back to O(n) per read and this is worse than useless.
//
// Needing both O(log n) updates AND O(log n) reads interleaved is what a Fenwick
// tree or segment tree is for. This structure is the cheap answer to the much
// more common special case: a pile of updates, then one look at the result.
//
// The same trick generalises: 2-D difference arrays for rectangle updates on a
// grid (four corner writes instead of two), and it is the standard way to solve
// "count overlapping intervals" / "how many meetings at once" problems.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run DifferenceArray.cs                everything
//   dotnet run DifferenceArray.cs -- trace       one small input, step by step
//   dotnet run DifferenceArray.cs -- test        the edge cases
//   dotnet run DifferenceArray.cs -- compare     brute force vs this, in steps
//   dotnet run DifferenceArray.cs -- bench       steps and milliseconds
//
// Nothing here is shared or factored out. Every method carries its own code from
// top to bottom — its own banner, its own table, its own drawing loop — even
// where that repeats. You can read any single method start to finish and see the
// whole story without jumping somewhere else.
//
// The lines below are "top-level statements": C# compiles them into a hidden
// Main, so there is no Main to write and no project to create. They have to come
// before any type declaration, which is why the classes sit at the bottom.
// =============================================================================

using System.Diagnostics;

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "trace") ShowTrace();
if (mode is "all" or "test") RunTests();
if (mode is "all" or "compare") ShowCompare();
if (mode is "all" or "bench") ShowBench();


// =============================================================================
// THE TRACE — one small input, all the way through
// =============================================================================

void ShowTrace()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TRACE");
    Console.WriteLine(new string('=', 78));

    int n = 6;
    (int l, int r, int v)[] updates = [(1, 3, 5), (0, 2, 2), (4, 5, 10)];

    Console.WriteLine();
    Console.WriteLine($"  array of {n} zeros, then {updates.Length} range updates:");
    Console.WriteLine();
    foreach (var (l, r, v) in updates)
        Console.WriteLine($"    add {v,2} to a[{l}..{r}]");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. Walk the range on every update.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — walk the range, every time");
    Console.WriteLine();
    Console.WriteLine("    update              cells touched   array after");
    Console.WriteLine("    -----------------   -------------   ------------------------");

    long[] brute = new long[n];
    long bruteWrites = 0;
    foreach (var (l, r, v) in updates)
    {
        int touched = 0;
        for (int i = l; i <= r; i++) { brute[i] += v; touched++; bruteWrites++; }
        Console.WriteLine($"    add {v,2} to a[{l}..{r}]   {touched,13}   [{string.Join(", ", brute.Select(x => x.ToString().PadLeft(2)))}]");
    }
    Console.WriteLine();
    Console.WriteLine($"    {bruteWrites} cell writes for {updates.Length} updates.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The difference array, narrated. Two writes per update, nothing else.
    // -------------------------------------------------------------------------
    Console.WriteLine("  DIFFERENCE ARRAY — two writes per update, n+1 slots");
    Console.WriteLine();
    Console.WriteLine("    update              writes                          diff after");
    Console.WriteLine("    -----------------   -----------------------------   ----------------------------");

    long[] diff = new long[n + 1];          // n+1: slot n holds a cancel that is never read
    long diffWrites = 0;
    foreach (var (l, r, v) in updates)
    {
        diff[l] += v;
        diff[r + 1] -= v;
        diffWrites += 2;
        Console.WriteLine($"    add {v,2} to a[{l}..{r}]   diff[{l}] += {v,2}, diff[{r + 1}] -= {v,2}       [{string.Join(", ", diff.Select(x => x.ToString().PadLeft(3)))}]");
    }
    Console.WriteLine();
    Console.WriteLine($"    {diffWrites} cell writes for {updates.Length} updates — 2 each, whatever the width.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Integrate. This loop IS the PrefixSums build.
    // -------------------------------------------------------------------------
    Console.WriteLine("  THE ONE PASS AT THE END — a running total over diff");
    Console.WriteLine();
    Console.WriteLine("    i   diff[i]   running   a[i]   same as brute force?");
    Console.WriteLine("    -   -------   -------   ----   --------------------");

    long[] result = new long[n];
    long running = 0;
    for (int i = 0; i < n; i++)
    {
        running += diff[i];
        result[i] = running;
        Console.WriteLine($"    {i}   {diff[i],7}   {running,7}   {result[i],4}   {(result[i] == brute[i] ? "yes" : "NO")}");
    }

    Console.WriteLine();
    Console.WriteLine($"    brute force : [{string.Join(", ", brute.Select(x => x.ToString().PadLeft(2)))}]");
    Console.WriteLine($"    difference  : [{string.Join(", ", result.Select(x => x.ToString().PadLeft(2)))}]");
    Console.WriteLine();

    Console.WriteLine("""
      THE WORK THAT DISAPPEARED

      The brute force paid for the WIDTH of every range: 3 + 3 + 2 = 8 writes for
      three updates. The difference array paid 2 per update regardless — 6 — and
      then one pass of 6 to integrate. On this tiny input that is a wash. Make the
      ranges wide and the updates many and it is not close.

      Why does a running total reconstruct the values? Because diff only ever
      records CHANGE. diff[1] = +5 says "from index 1 on, everything is 5 higher";
      diff[4] = -5 says "that stops now". Carrying a running total forward applies
      each change from where it starts and unapplies it where it stops, so the
      total at index i is exactly the sum of every update covering i.

          diff   [  2,   5,   0,  -2,   5,   0, -10]
                    |    |         |    |         |
                    +2   +5        -2   +5    (never read)
          running    2    7    7    5   10   10
                                         ^
                                         slot 4 holds +5, which is update 1's
                                         -5 and update 3's +10 SUMMED in the
                                         same cell — marks just add up

      That is integration, and it is the prefix-sum build loop with a different
      name. Which is the thing worth remembering: PrefixSums differentiates on
      demand, DifferenceArray integrates once. Same machinery, opposite direction.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The off-by-one, shown rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE OFF-BY-ONE, SHOWN");
    Console.WriteLine();
    Console.WriteLine("  Same single update, add 5 to a[1..3]. Cancel at r+1 vs at r:");
    Console.WriteLine();

    long[] right = new long[n + 1];
    right[1] += 5; right[4] -= 5;                 // correct: cancel at r+1
    long[] wrong = new long[n + 1];
    wrong[1] += 5; wrong[3] -= 5;                 // bug: cancel at r

    long[] rOut = new long[n], wOut = new long[n];
    long rr = 0, ww = 0;
    for (int i = 0; i < n; i++) { rr += right[i]; rOut[i] = rr; ww += wrong[i]; wOut[i] = ww; }

    Console.WriteLine($"    cancel at r+1 (correct) : [{string.Join(", ", rOut.Select(x => x.ToString().PadLeft(2)))}]   <- a[3] is 5");
    Console.WriteLine($"    cancel at r   (bug)     : [{string.Join(", ", wOut.Select(x => x.ToString().PadLeft(2)))}]   <- a[3] is 0, lost");
    Console.WriteLine();
    Console.WriteLine("  The range is inclusive of r, so the change must still be in effect");
    Console.WriteLine("  AT r. Cancelling at r switches it off one cell early and silently");
    Console.WriteLine("  drops the last element of every single range.");
    Console.WriteLine();
}


// =============================================================================
// THE TESTS — the eight cases from the template
// =============================================================================

void RunTests()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TESTS");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("    case                      n   updates                     expected result");
    Console.WriteLine("    -----------------------   -   -------------------------   ------------------------------");

    int passed = 0, total = 0;

    void Check(string name, int n, (int l, int r, int v)[] updates, long[] expect)
    {
        total++;
        long[] brute = Ranges.ApplyBrute(n, updates);
        long[] diff = Ranges.ApplyDifference(n, updates);
        bool ok = brute.SequenceEqual(expect) && diff.SequenceEqual(expect);
        if (ok) passed++;

        string ups = updates.Length == 0 ? "(none)" : string.Join(" ", updates.Select(u => $"+{u.v}@{u.l}..{u.r}"));
        if (ups.Length > 25) ups = ups[..22] + "...";
        string exp = $"[{string.Join(",", expect)}]";
        if (exp.Length > 30) exp = exp[..27] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {n,1}   {ups,-25}   {exp,-30}");
    }

    Check("no updates", 3, [], [0, 0, 0]);
    Check("single cell", 3, [(1, 1, 5)], [0, 5, 0]);
    Check("whole array", 3, [(0, 2, 4)], [4, 4, 4]);
    Check("starts at 0", 4, [(0, 1, 7)], [7, 7, 0, 0]);
    Check("ends at last", 4, [(2, 3, 7)], [0, 0, 7, 7]);
    Check("n == 1", 1, [(0, 0, 9)], [9]);
    Check("overlapping", 4, [(0, 2, 1), (1, 3, 10)], [1, 11, 11, 10]);
    Check("nested", 5, [(0, 4, 1), (2, 2, 100)], [1, 1, 101, 1, 1]);
    Check("adjacent, no gap", 4, [(0, 1, 3), (2, 3, 3)], [3, 3, 3, 3]);
    Check("negative v", 3, [(0, 2, 5), (1, 1, -5)], [5, 0, 5]);
    Check("cancels to zero", 3, [(0, 2, 8), (0, 2, -8)], [0, 0, 0]);
    Check("same range twice", 3, [(1, 2, 3), (1, 2, 3)], [0, 6, 6]);

    // Overflow: 1,000 updates of int.MaxValue over the same cell. Each v fits in
    // an int; the accumulated value does not. This is why everything is long.
    var many = new (int, int, int)[1000];
    for (int i = 0; i < 1000; i++) many[i] = (0, 0, int.MaxValue);
    Check("overflow (int.Max)", 1, many, [1000L * int.MaxValue]);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Two cases earn their place here.

          "ends at last" is the one that catches the off-by-one: r is the final
          index, so the cancel goes in diff[n] — the slot that exists only to be
          written and never read. Size the array n instead of n+1 and this throws.

          "adjacent, no gap" catches the opposite mistake. a[0..1] and a[2..3]
          must not bleed into each other: the first update's cancel at diff[2] and
          the second's start at diff[2] land in the SAME slot and sum to zero
          there, which is exactly right.
          """);
        Console.WriteLine();
    }
}


// =============================================================================
// SIDE BY SIDE — the same work, both implementations, counted
// =============================================================================

void ShowCompare()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  SIDE BY SIDE");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  n updates over an array of n, each spanning the whole array,");
    Console.WriteLine("  then read the finished array once.");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   diff steps   of which integrate        ratio");
    Console.WriteLine("    -----   -----------------   ----------   ------------------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        var updates = new (int, int, int)[n];
        for (int i = 0; i < n; i++) updates[i] = (0, n - 1, i % 7);

        Ranges.Steps = 0;
        Ranges.ApplyBrute(n, updates);
        long brute = Ranges.Steps;

        Ranges.Steps = 0;
        Ranges.ApplyDifference(n, updates);
        long diff = Ranges.Steps;

        // The integrate pass is exactly n of those steps; the rest is 2 per update.
        long integrate = n;

        Console.WriteLine($"    {n,5}   {brute,17:N0}   {diff,10:N0}   {integrate,18:N0}   {(double)brute / diff,9:N0}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The brute force is n updates x n cells = n^2. The difference array is
      2 per update plus one n-long pass at the end, so 3n total — linear.

      Notice the shape of the saving is the same as PrefixSums but pointed the
      other way. There, one build made every later READ free. Here, cheap writes
      defer all the work to a single pass that happens ONCE no matter how many
      updates there were. Either way the win is "do not repeat a loop you have
      already run", and either way you pay for it with an extra O(n) array.

      Break-even is two updates, for the same reason: with one update the
      integrate pass costs the same walk you were trying to avoid.
      """);
    Console.WriteLine();
}


// =============================================================================
// BENCHMARK — steps are deterministic, milliseconds make it real
// =============================================================================

void ShowBench()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  BENCHMARK");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  n updates spanning the whole array of n, then one read pass.");
    Console.WriteLine();
    Console.WriteLine("            brute force                difference array");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        var updates = new (int, int, int)[n];
        for (int i = 0; i < n; i++) updates[i] = (0, n - 1, i % 7);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Ranges.ApplyBrute(n, updates);
        Ranges.ApplyDifference(n, updates);

        Ranges.Steps = 0;
        var sw = Stopwatch.StartNew();
        Ranges.ApplyBrute(n, updates);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Ranges.Steps;

        Ranges.Steps = 0;
        sw.Restart();
        Ranges.ApplyDifference(n, updates);
        double diffMs = sw.Elapsed.TotalMilliseconds;
        long diffSteps = Ranges.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {diffSteps,12:N0}   {diffMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE TWO IMPLEMENTATIONS
//
// Both take an array of n zeros and a list of (l, r, v) updates meaning "add v
// to every element in a[l..r]", inclusive at both ends, and return the finished
// array. Both reset or add to Ranges.Steps so the report methods can read it.
//
// Everything is long, not int. A cell accumulates every update covering it, so
// it overflows an int long before any single v does.
// =============================================================================

static class Ranges
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n) per update, O(1) extra space.
    //
    // Walk the range and add. Correct, obvious, and it re-walks the same cells
    // on every single update — which is the work the other version deletes.
    // -------------------------------------------------------------------------
    public static long[] ApplyBrute(int n, (int l, int r, int v)[] updates)
    {
        long[] a = new long[n];

        foreach (var (l, r, v) in updates)
        {
            if (l < 0 || r >= n || l > r)
                throw new ArgumentOutOfRangeException(nameof(updates), $"bad range {l}..{r} for length {n}");

            for (int i = l; i <= r; i++)
            {
                Steps++;                 // one cell written
                a[i] += v;
            }
        }

        return a;
    }

    // -------------------------------------------------------------------------
    // DIFFERENCE ARRAY — O(1) per update, one O(n) pass at the end, O(n) space.
    //
    // diff[i] holds "the change that begins at index i". Two writes record a
    // range: start the change at l, stop it after r. Nothing stores a value
    // until the integrate pass turns the changes back into values.
    //
    // The array is n+1 long so that diff[r+1] exists when r == n-1. That last
    // slot is written and never read.
    // -------------------------------------------------------------------------
    public static long[] ApplyDifference(int n, (int l, int r, int v)[] updates)
    {
        long[] diff = new long[n + 1];

        foreach (var (l, r, v) in updates)
        {
            if (l < 0 || r >= n || l > r)
                throw new ArgumentOutOfRangeException(nameof(updates), $"bad range {l}..{r} for length {n}");

            Steps += 2;                  // two cell writes, whatever the width
            diff[l] += v;                // from l onward, everything is v higher
            diff[r + 1] -= v;            // from r+1 onward, cancel it. NOT r.
        }

        // Integrate: a running total over diff. This loop is the PrefixSums
        // build with a different name — which is the whole point.
        long[] a = new long[n];
        long running = 0;
        for (int i = 0; i < n; i++)
        {
            Steps++;                     // one cell written
            running += diff[i];
            a[i] = running;
        }

        return a;
    }
}
