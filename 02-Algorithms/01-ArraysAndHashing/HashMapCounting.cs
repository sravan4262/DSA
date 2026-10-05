#!/usr/bin/env dotnet
// =============================================================================
// HASHMAPCOUNTING — trade memory for time by remembering what you have seen.
//
// Node 01 · Arrays & Hashing.  Needs: array, hash table.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// Not really one algorithm — a pattern you will reach for more than any other.
// Whenever a nested loop exists only to ask "have I seen this before?", you can
// delete the inner loop by writing the answer down as you go.
//
//   brute force   for each element, re-scan the rest          O(n^2) time, O(1) space
//   this          one pass, a hash table carries the memory   O(n) time,   O(n) space
//
// You are buying time with memory. That is the entire trade, and it is the most
// common optimisation in the whole roadmap.
//
// THE THREE JOBS, and how to pick
//
//   frequency      how many times does each value appear?   Dictionary<T,int>
//   membership     is it in the set at all?                 HashSet<T>
//   seen-before    have I passed this already?              HashSet<T>, filled
//                                                           DURING the pass
//
// One question decides it: do you need a COUNT, or just YES/NO? A count needs
// Dictionary<T,int>. Yes/no needs HashSet<T> — the same machinery minus the
// value field, so it is smaller and reads clearer.
//
// Membership and seen-before use the identical type and differ only in WHEN the
// set is filled. Membership builds it up front from the whole input. Seen-before
// starts empty and grows as you walk, which is what makes "before" mean anything.
//
// THE CONCRETE PROBLEM USED HERE
//
// Find the first value whose second occurrence you reach, or -1 if every value
// is distinct. It is the smallest problem that needs seen-before, and it gives
// the brute force a fair fight — both walk left to right, so both agree on what
// "first" means.
//
// WHEN NOT TO USE IT
//
// The O(n) memory is not free, and neither is a hash probe — it computes a hash,
// takes a modulus and follows two dependent memory reads, so call it 5-20x the
// cost of one comparison. For a handful of elements the brute force wins on wall
// clock. If the input is already sorted, two pointers beat both and cost nothing.
// And if the keys are a small bounded range, a plain int[] counter array beats a
// Dictionary outright — no hashing, no collisions, perfect cache behaviour.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run HashMapCounting.cs                everything
//   dotnet run HashMapCounting.cs -- trace       one small input, step by step
//   dotnet run HashMapCounting.cs -- test        the edge cases
//   dotnet run HashMapCounting.cs -- compare     brute force vs this, in steps
//   dotnet run HashMapCounting.cs -- bench       steps and milliseconds
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

    int[] input = [4, 7, 2, 7, 9];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", input)}]");
    Console.WriteLine("  job:   find the first value that appears twice");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. For each element, re-scan everything after it.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — for each element, scan the rest");
    Console.WriteLine();
    Console.WriteLine("    i  a[i]  compares against        found?");
    Console.WriteLine("    -  ----  ---------------------   ------");

    long bruteCompares = 0;
    int bruteAnswer = -1;
    for (int i = 0; i < input.Length && bruteAnswer == -1; i++)
    {
        var looked = new List<string>();
        for (int j = i + 1; j < input.Length; j++)
        {
            bruteCompares++;
            looked.Add(input[j].ToString());
            if (input[i] == input[j]) { bruteAnswer = input[i]; break; }
        }
        string hit = bruteAnswer == -1 ? "no" : $"YES -> {bruteAnswer}";
        Console.WriteLine($"    {i}  {input[i],4}  {string.Join(" ", looked),-21}   {hit}");
    }
    Console.WriteLine();
    Console.WriteLine($"    {bruteCompares} comparisons.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The hash version, narrated. One pass. The set carries the memory.
    // -------------------------------------------------------------------------
    Console.WriteLine("  WITH A HASH SET — one pass, remember as you go");
    Console.WriteLine();
    Console.WriteLine("    i  a[i]  seen before?   set after this step");
    Console.WriteLine("    -  ----  ------------   --------------------");

    var seen = new HashSet<int>();
    int hashAnswer = -1;
    for (int i = 0; i < input.Length; i++)
    {
        bool isNew = seen.Add(input[i]);
        string verdict = isNew ? "no" : $"YES -> {input[i]}";
        Console.WriteLine($"    {i}  {input[i],4}  {verdict,-12}   {{{string.Join(", ", seen)}}}");
        if (!isNew) { hashAnswer = input[i]; break; }
    }
    Console.WriteLine();
    Console.WriteLine($"    4 elements touched. Answer: {hashAnswer}");
    Console.WriteLine();

    Console.WriteLine("""
      THE WORK THAT DISAPPEARED

      The brute force asks "is a[i] anywhere later in the array?" and answers it
      by looking. Every question starts from scratch, so element 0 is compared
      against the tail, then element 1 against the tail, and the same cells get
      re-read over and over.

      The hash set answers the same question by having already written the answer
      down. One pass, O(1) average per lookup, and the array is never re-read.

      That is the whole trade: O(n) extra memory buys you O(n) time instead of
      O(n^2). It is the most common optimisation there is.
  """);

    // -------------------------------------------------------------------------
    // The same machinery, the other two jobs.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE THREE JOBS THIS PATTERN DOES");
    Console.WriteLine();

    int[] words = [4, 7, 2, 7, 9, 7];
    Console.WriteLine($"  input: [{string.Join(", ", words)}]");
    Console.WriteLine();

    // 1. FREQUENCY — Dictionary<T,int>, because you need a count per key.
    var freq = new Dictionary<int, int>();
    foreach (int x in words) freq[x] = freq.GetValueOrDefault(x) + 1;

    Console.WriteLine("    1. FREQUENCY   Dictionary<int,int>   value -> how many times");
    foreach (var kv in freq.OrderBy(k => k.Key))
        Console.WriteLine($"                      {kv.Key} appears {kv.Value}x");
    Console.WriteLine();

    // 2. MEMBERSHIP — HashSet<T>, because you only need yes/no.
    var members = new HashSet<int>(words);
    Console.WriteLine("    2. MEMBERSHIP  HashSet<int>          is it there at all?");
    Console.WriteLine($"                      contains 7 : {members.Contains(7)}");
    Console.WriteLine($"                      contains 5 : {members.Contains(5)}");
    Console.WriteLine();

    // 3. SEEN-BEFORE — HashSet<T> built up DURING the pass, not before it.
    Console.WriteLine("    3. SEEN-BEFORE HashSet<int>          built DURING the pass");
    Console.WriteLine("                      the set is empty at the start and grows as");
    Console.WriteLine("                      you walk — that is what makes it 'before'");
    Console.WriteLine();

    Console.WriteLine("""
      Picking between them is one question: do you need a COUNT, or just YES/NO?
      A count needs Dictionary<T,int>. Yes/no needs HashSet<T>, which is the same
      machinery without the value field — so it is smaller and reads clearer.
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
    Console.WriteLine("    case                      input                        expect        brute         hash");
    Console.WriteLine("    -----------------------   --------------------------   -----------   -----------   -----------");

    int passed = 0, total = 0;

    void Check(string name, int[] input, int expect)
    {
        total++;
        int brute = Counting.FirstDuplicateBrute(input);
        int hash = Counting.FirstDuplicateHash(input);
        bool ok = brute == expect && hash == expect;
        if (ok) passed++;

        string shown = input.Length == 0 ? "[]" : $"[{string.Join(",", input)}]";
        if (shown.Length > 26) shown = shown[..23] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-19}   {shown,-26}   {expect,11}   {brute,11}   {hash,11}");
    }

    // The eight cases the template demands. -1 means "no duplicate".
    Check("empty", [], -1);
    Check("single element", [5], -1);
    Check("two, no duplicate", [5, 9], -1);
    Check("two, duplicate", [5, 5], 5);
    Check("already the answer", [7, 7, 1, 2], 7);
    Check("duplicate at end", [1, 2, 3, 1], 1);
    Check("reverse order", [9, 7, 5, 3, 1], -1);
    Check("all identical", [4, 4, 4, 4], 4);
    Check("negative numbers", [-3, 5, -3], -3);
    Check("zero is a value", [0, 1, 0], 0);
    Check("int.MinValue", [int.MinValue, 1, int.MinValue], int.MinValue);

    // The one case that matters most and is easiest to get wrong: FIRST means
    // first by SECOND occurrence, not by smallest value.
    Check("first, not smallest", [9, 3, 9, 3], 9);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Note what 'first duplicate' means: the first value whose SECOND
          occurrence you reach. For [9,3,9,3] that is 9, not 3 — even though 3
          is smaller and its pair is closer together. Both implementations agree
          because both walk left to right.
      """);
        Console.WriteLine();
    }
}


// =============================================================================
// SIDE BY SIDE — the same input, both implementations, counted
// =============================================================================

void ShowCompare()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  SIDE BY SIDE");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Worst case for both: every value distinct, so neither can exit early.");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   hash steps        ratio");
    Console.WriteLine("    -----   -----------------   ----------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 100_000])
    {
        // Distinct values -> no duplicate -> both run to completion.
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i;

        Counting.FirstDuplicateBrute(data);
        long brute = Counting.Steps;

        Counting.FirstDuplicateHash(data);
        long hash = Counting.Steps;

        Console.WriteLine($"    {n,5}   {brute,17:N0}   {hash,10:N0}   {(double)brute / hash,9:N0}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The brute-force column is n(n-1)/2 — it quadruples when n doubles. The hash
      column is exactly n. The ratio is therefore n/2 and grows without limit:
      there is no size at which the brute force catches up.

      Honest caveat on the step counts: one 'hash step' is not as cheap as one
      'brute-force step'. A comparison is one instruction; a hash probe computes
      a hash, takes a modulus, and follows two dependent memory reads. Call it
      5-20x more expensive per step. At n = 100 that genuinely matters and the
      brute force can win on wall clock. By n = 1,000 the n/2 factor has buried
      it. That is why -- bench prints milliseconds next to the steps.
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
    Console.WriteLine("            brute force                   hash set");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i;

        // Warm up the JIT first, or the first row measures compilation.
        Counting.FirstDuplicateBrute(data);
        Counting.FirstDuplicateHash(data);

        var sw = Stopwatch.StartNew();
        Counting.FirstDuplicateBrute(data);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Counting.Steps;

        sw.Restart();
        Counting.FirstDuplicateHash(data);
        double hashMs = sw.Elapsed.TotalMilliseconds;
        long hashSteps = Counting.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {hashSteps,12:N0}   {hashMs,6:N2}");
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
// Both return the first value whose second occurrence is reached, or -1 if
// every value is distinct. Both walk left to right so they agree on "first".
// Both reset and populate Counting.Steps so -- compare can read it.
// =============================================================================

static class Counting
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n^2) time, O(1) space.
    //
    // For each element, re-scan everything after it. No memory of what has
    // already been seen, so the tail of the array gets read again and again.
    // -------------------------------------------------------------------------
    public static int FirstDuplicateBrute(int[] a)
    {
        Steps = 0;

        for (int i = 0; i < a.Length; i++)
        {
            for (int j = i + 1; j < a.Length; j++)
            {
                Steps++;                      // one comparison
                if (a[i] == a[j]) return a[j];
            }
        }

        return -1;
    }

    // -------------------------------------------------------------------------
    // WITH A HASH SET — O(n) time average, O(n) space.
    //
    // One pass. The set is the memory the brute force did not have.
    //
    // HashSet<T>.Add returns false when the value was already present, so the
    // insert and the membership test are a single operation. Checking Contains
    // and then calling Add would hash the same value twice.
    // -------------------------------------------------------------------------
    public static int FirstDuplicateHash(int[] a)
    {
        Steps = 0;

        var seen = new HashSet<int>(a.Length);   // pre-sized: no rehashing

        foreach (int x in a)
        {
            Steps++;                             // one element touched
            if (!seen.Add(x)) return x;          // Add returned false -> seen before
        }

        return -1;
    }
}
