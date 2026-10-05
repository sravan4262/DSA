#!/usr/bin/env dotnet
// =============================================================================
// PREFIXSUMS — pay O(n) once so every range query afterwards costs O(1).
//
// Node 01 · Arrays & Hashing.  Needs: array.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// You are asked the same SHAPE of question many times: "what is the sum of
// a[l..r]?" Answering it directly means walking the range, so q queries over a
// range of length n cost O(n) each.
//
// Instead, precompute one array where slot i holds the sum of everything before
// i. Then any range is the difference of two slots:
//
//   sum(l..r) = prefix[r + 1] - prefix[l]
//
// One subtraction. The range length stops mattering entirely — summing 10 cells
// and summing 10,000,000 cells cost exactly the same.
//
//   brute force   walk the range, every query        O(n) per query
//   this          O(n) build once, then subtract     O(1) per query
//
// So the trade is not "faster" — it is MOVING the cost. You pay a single O(n)
// pass up front and O(n) extra memory, and in exchange every query afterwards
// is free. That only wins if there is more than one query.
//
// WHY THE ARRAY IS n+1 LONG, AND WHY prefix[0] = 0
//
// This is the part everyone gets wrong first. prefix[i] means "the sum of the
// first i elements" — NOT "the sum up to index i". So:
//
//   prefix[0] = 0                  the sum of no elements
//   prefix[1] = a[0]
//   prefix[2] = a[0] + a[1]
//   prefix[i] = a[0] + ... + a[i-1]
//
// That leading zero is what makes the formula work with no special case for
// l = 0. Without it you would need "if (l == 0) return prefix[r];" and that
// branch is where the bugs live. One wasted slot buys a formula with no
// exceptions in it.
//
// WHEN NOT TO USE IT
//
// The array must not change. A single write to a[i] invalidates every prefix
// slot from i+1 onward, so rebuilding is O(n) — which destroys the whole point.
// If the data is updated between queries you want a Fenwick tree (binary
// indexed tree) or a segment tree instead: O(log n) for both update and query.
// And with exactly one query, the build costs more than just walking the range.
//
// The same trick generalises: prefix products, prefix XOR, prefix min over a
// fixed window, and 2-D prefix sums for rectangle queries on a grid.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run PrefixSums.cs                everything
//   dotnet run PrefixSums.cs -- trace       one small input, step by step
//   dotnet run PrefixSums.cs -- test        the edge cases
//   dotnet run PrefixSums.cs -- compare     brute force vs this, in steps
//   dotnet run PrefixSums.cs -- bench       steps and milliseconds
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

    int[] a = [3, 1, 4, 1, 5, 9];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", a)}]");
    Console.WriteLine("  job:   answer 'sum of a[l..r]' many times");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Build the prefix array, narrated one slot at a time.
    // -------------------------------------------------------------------------
    Console.WriteLine("  STEP 1 — build the prefix array, ONE pass, n+1 slots");
    Console.WriteLine();
    Console.WriteLine("    i   a[i-1]   prefix[i] = prefix[i-1] + a[i-1]   meaning");
    Console.WriteLine("    -   ------   -------------------------------   --------------------");

    int[] prefix = new int[a.Length + 1];
    Console.WriteLine($"    0        -   prefix[0] = 0                     sum of NO elements");
    for (int i = 1; i <= a.Length; i++)
    {
        prefix[i] = prefix[i - 1] + a[i - 1];
        Console.WriteLine($"    {i}   {a[i - 1],6}   prefix[{i}] = {prefix[i - 1],2} + {a[i - 1],-2} = {prefix[i],-13}   sum of first {i} element{(i == 1 ? "" : "s")}");
    }

    Console.WriteLine();
    Console.WriteLine($"    prefix = [{string.Join(", ", prefix.Select(x => x.ToString().PadLeft(2)))}]");
    Console.WriteLine($"    a      =     [{string.Join(", ", a.Select(x => x.ToString().PadLeft(2)))}]");
    Console.WriteLine("               ^");
    Console.WriteLine("                the extra leading 0 — this is the slot that removes");
    Console.WriteLine("                the l == 0 special case from the formula");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Answer queries both ways, side by side, so the saving is visible.
    // -------------------------------------------------------------------------
    Console.WriteLine("  STEP 2 — answer queries. Both ways, same answers.");
    Console.WriteLine();
    Console.WriteLine("    query     brute force walks        steps     prefix formula              steps");
    Console.WriteLine("    -------   ----------------------   -----     -------------------------   -----");

    (int l, int r)[] queries = [(0, 2), (1, 4), (3, 5), (0, 5), (2, 2)];

    foreach (var (l, r) in queries)
    {
        // Brute force: walk every cell in the range.
        int walked = 0, bruteSum = 0;
        var terms = new List<string>();
        for (int i = l; i <= r; i++) { bruteSum += a[i]; walked++; terms.Add(a[i].ToString()); }

        // Prefix: one subtraction, regardless of how wide the range is.
        int fast = prefix[r + 1] - prefix[l];

        string expr = $"{string.Join("+", terms)} = {bruteSum}";
        string pf = $"prefix[{r + 1}]-prefix[{l}] = {fast}";
        Console.WriteLine($"    a[{l}..{r}]   {expr,-22}   {walked,5}     {pf,-25}   {1,5}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      THE WORK THAT DISAPPEARED

      Look at the two step columns. The brute force pays for the WIDTH of the
      range — a[0..5] costs six reads, a[2..2] costs one. The prefix version
      pays 1 either way, because the width never enters the arithmetic.

      Why is subtracting safe? prefix[6] is the sum of everything up to index 5
      and prefix[3] is the sum of everything up to index 2. Everything the two
      have in common cancels, and what survives is exactly a[3] + a[4] + a[5].
      You are not re-summing the range; you are deleting a shared head.

          prefix[6] = 3 + 1 + 4 + 1 + 5 + 9
          prefix[3] = 3 + 1 + 4
                      ---------
          difference =           1 + 5 + 9   <- exactly a[3..5]

      That cancellation is the whole algorithm.
      """);
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
    Console.WriteLine("    case                      input                   query          expect            brute           prefix");
    Console.WriteLine("    -----------------------   ---------------------   ------   --------------   --------------   --------------");

    int passed = 0, total = 0;

    void Check(string name, int[] a, int l, int r, long expect)
    {
        total++;
        long brute = Ranges.SumBrute(a, l, r);
        long fast = Ranges.SumPrefix(Ranges.Build(a), l, r);
        bool ok = brute == expect && fast == expect;
        if (ok) passed++;

        string shown = a.Length == 0 ? "[]" : $"[{string.Join(",", a)}]";
        if (shown.Length > 21) shown = shown[..18] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-21}   {$"{l}..{r}",6}   {expect,14}   {brute,14}   {fast,14}");
    }

    Check("single element", [7], 0, 0, 7);
    Check("whole array", [3, 1, 4], 0, 2, 8);
    Check("l == 0", [3, 1, 4, 1, 5], 0, 2, 8);
    Check("r == last", [3, 1, 4, 1, 5], 2, 4, 10);
    Check("l == r, middle", [3, 1, 4, 1, 5], 2, 2, 4);
    Check("two elements", [8, 9], 0, 1, 17);
    Check("all negative", [-3, -1, -4], 0, 2, -8);
    Check("mixed signs", [5, -3, 2, -8], 1, 3, -9);
    Check("all zeros", [0, 0, 0, 0], 0, 3, 0);
    Check("sums to zero", [5, -5, 7], 0, 1, 0);
    Check("reverse sorted", [9, 7, 5, 3, 1], 1, 3, 15);

    // Overflow: 1,000 copies of int.MaxValue. Each one fits in an int; the sum
    // does not. This is why Build returns long[] and not int[].
    int[] big = new int[1000];
    Array.Fill(big, int.MaxValue);
    Check("overflow (int.Max)", big, 0, 999, 1000L * int.MaxValue);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          The last case is the one that bites in real code. Every element fits in
          an int, but their sum does not. 1,000 x int.MaxValue is about 2.1
          trillion, which needs a long. A prefix array accumulates EVERYTHING, so it
          overflows far earlier than any single element suggests. That is why
          Build returns long[] rather than int[].

          Note also there is no empty-array case in this table. "Sum of a[l..r]"
          has no meaning when there is no valid l — an empty input is a caller
          error, not an edge case, so both implementations throw.
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
    Console.WriteLine("  n queries over an array of n, each query spanning the whole array");
    Console.WriteLine("  (the worst case for the brute force, and a fair one — it is the");
    Console.WriteLine("  repeated-query situation that makes anyone reach for this).");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   prefix steps   of which build        ratio");
    Console.WriteLine("    -----   -----------------   ------------   --------------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i % 97;

        // Brute force: n queries, each walking all n cells.
        Ranges.Steps = 0;
        for (int q = 0; q < n; q++) Ranges.SumBrute(data, 0, n - 1);
        long brute = Ranges.Steps;

        // Prefix: one build, then n subtractions.
        Ranges.Steps = 0;
        long[] prefix = Ranges.Build(data);
        long build = Ranges.Steps;
        for (int q = 0; q < n; q++) Ranges.SumPrefix(prefix, 0, n - 1);
        long fast = Ranges.Steps;

        Console.WriteLine($"    {n,5}   {brute,17:N0}   {fast,12:N0}   {build,14:N0}   {(double)brute / fast,9:N0}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Read the 'of which build' column: almost all of the prefix cost IS the
      build. The n queries afterwards are nearly free, which is the point.

      The ratio is n/2 and grows without limit, but notice WHY it is not simply
      "this algorithm is faster". Both do one O(n) pass over the data. The brute
      force then does that pass again for every single query; the prefix version
      never touches the array again. The saving is not a cleverer loop — it is
      not repeating a loop you already ran.

      The break-even is one query. With exactly one query the brute force wins,
      because the build costs the same walk and then you still have to subtract.
      With two, the prefix version is already ahead and never looks back.
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
    Console.WriteLine("  n queries spanning the whole array of n.");
    Console.WriteLine();
    Console.WriteLine("            brute force                   prefix sums");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i % 97;

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Ranges.SumBrute(data, 0, n - 1);
        Ranges.SumPrefix(Ranges.Build(data), 0, n - 1);

        Ranges.Steps = 0;
        var sw = Stopwatch.StartNew();
        for (int q = 0; q < n; q++) Ranges.SumBrute(data, 0, n - 1);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Ranges.Steps;

        Ranges.Steps = 0;
        sw.Restart();
        long[] prefix = Ranges.Build(data);
        for (int q = 0; q < n; q++) Ranges.SumPrefix(prefix, 0, n - 1);
        double fastMs = sw.Elapsed.TotalMilliseconds;
        long fastSteps = Ranges.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {fastSteps,12:N0}   {fastMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.

      The brute-force column is one of the clearest O(n^2) curves you will see:
      n doubles, the work quadruples. The prefix column is linear.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE TWO IMPLEMENTATIONS
//
// Both answer "the sum of a[l..r]", inclusive at both ends. Both reset or add
// to Ranges.Steps so the report methods above can read it.
//
// Note the return type is long, not int. A sum overflows an int long before any
// single element does — see the overflow test case.
// =============================================================================

static class Ranges
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n) per query, O(1) space.
    //
    // Walk the range and add. No precomputation, no extra memory, and no memory
    // of the last query either — which is the whole problem.
    // -------------------------------------------------------------------------
    public static long SumBrute(int[] a, int l, int r)
    {
        if (l < 0 || r >= a.Length || l > r)
            throw new ArgumentOutOfRangeException(nameof(l), $"bad range {l}..{r} for length {a.Length}");

        long sum = 0;
        for (int i = l; i <= r; i++)
        {
            Steps++;                     // one cell read
            sum += a[i];
        }

        return sum;
    }

    // -------------------------------------------------------------------------
    // BUILD — O(n) time, O(n) space. Run once.
    //
    // prefix[i] is the sum of the FIRST i elements, so prefix[0] = 0 and the
    // array is n+1 long. That leading zero is load-bearing: it is what lets
    // SumPrefix handle l = 0 with no special case.
    // -------------------------------------------------------------------------
    public static long[] Build(int[] a)
    {
        long[] prefix = new long[a.Length + 1];   // n+1, and prefix[0] stays 0

        for (int i = 1; i <= a.Length; i++)
        {
            Steps++;                     // one cell written
            prefix[i] = prefix[i - 1] + a[i - 1];
        }

        return prefix;
    }

    // -------------------------------------------------------------------------
    // QUERY — O(1), always.
    //
    // One subtraction. The range width never appears, so a 10-cell range and a
    // 10-million-cell range cost the same.
    // -------------------------------------------------------------------------
    public static long SumPrefix(long[] prefix, int l, int r)
    {
        if (l < 0 || r >= prefix.Length - 1 || l > r)
            throw new ArgumentOutOfRangeException(nameof(l), $"bad range {l}..{r} for length {prefix.Length - 1}");

        Steps++;                         // one subtraction

        // prefix[r+1] includes a[r]; prefix[l] stops just before a[l].
        // The shared head cancels and the range is what is left.
        return prefix[r + 1] - prefix[l];
    }
}
