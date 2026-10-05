#!/usr/bin/env dotnet
// =============================================================================
// KADANE — the largest sum of any contiguous subarray, in one pass.
//
// Node 01 · Arrays & Hashing.  Needs: array.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// Given [-2, 1, -3, 4, -1, 2, 1, -5, 4], which run of neighbouring elements
// adds up to the most? (It is a[3..6] = [4, -1, 2, 1], summing to 6.)
//
// The obvious approach tries every start and extends it: O(n^2). Kadane does it
// in ONE pass by never looking backwards, and the whole thing is one question
// asked at every index:
//
//   "Does the best run ending HERE extend the previous run, or start fresh at me?"
//
//   current = max(a[i], current + a[i])
//   best    = max(best, current)
//
// Two variables. No array, no nested loop, no going back.
//
//   brute force   every start, extend to every end    O(n^2) time, O(1) space
//   this          one pass, two variables             O(n) time,   O(1) space
//
// WHY ONE PASS IS ENOUGH — the insight worth owning
//
// Read the max() again. If current is NEGATIVE, then current + a[i] is worse
// than a[i] by itself, so the answer is "start fresh" — and that is always the
// right call. A negative running sum can never help any future subarray. It is
// dead weight, so you drop it the instant it goes negative and you never need
// to reconsider.
//
// That is why the algorithm can commit at every index and never revisit. An
// equivalent way to write the same line makes it obvious:
//
//   current = max(current, 0) + a[i]        "carry the prefix only if it helps"
//
// THIS IS DYNAMIC PROGRAMMING, AT ITS SMALLEST
//
//   dp[i] = max(a[i], dp[i-1] + a[i])       the best subarray ENDING at i
//
// That is a textbook recurrence, and the answer is max(dp). But dp[i] only ever
// reads dp[i-1], so you do not need the array at all — one variable does it, and
// O(n) space collapses to O(1). Node 17 calls that trick "rolling the state",
// and this is the smallest place to meet it. Which is why Kadane sits in node 01
// rather than with the rest of DP.
//
// THE TRAP: ALL-NEGATIVE INPUT
//
// Initialise best = 0 and [-3, -1, -2] returns 0 — the sum of the empty
// subarray. The real answer is -1. Whether that is a bug depends on a question
// the problem statement has to answer: is the empty subarray allowed?
//
//   "non-empty subarray"  ->  best = a[0], and this file does that
//   "possibly empty"      ->  best = 0 is correct
//
// Nearly every version of this problem says non-empty, so start from a[0] and
// loop from index 1. Starting from 0 is the single most common wrong answer.
//
// WHEN NOT TO USE IT
//
// It answers exactly one question — the maximum sum. It does not generalise to
// maximum PRODUCT without changes (negatives flip the ordering, so you must
// track min and max together), and it says nothing about non-contiguous
// subsequences, which is a different problem entirely. If you need the sum of an
// arbitrary range rather than the best one, that is PrefixSums.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run Kadane.cs                everything
//   dotnet run Kadane.cs -- trace       one small input, step by step
//   dotnet run Kadane.cs -- test        the edge cases
//   dotnet run Kadane.cs -- compare     brute force vs this, in steps
//   dotnet run Kadane.cs -- bench       steps and milliseconds
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

    int[] a = [-2, 1, -3, 4, -1, 2, 1, -5, 4];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", a)}]");
    Console.WriteLine("  job:   the largest sum of any CONTIGUOUS run");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. Every start, extended to every end.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — fix a start, extend it to every end");
    Console.WriteLine();
    Console.WriteLine("    start   best sum from this start   subarray");
    Console.WriteLine("    -----   ------------------------   -------------------------");

    long bruteSteps = 0;
    long bruteBest = long.MinValue;
    int bb = 0, be = 0;
    for (int i = 0; i < a.Length; i++)
    {
        long running = 0, bestHere = long.MinValue;
        int endHere = i;
        for (int j = i; j < a.Length; j++)
        {
            bruteSteps++;
            running += a[j];
            if (running > bestHere) { bestHere = running; endHere = j; }
            if (running > bruteBest) { bruteBest = running; bb = i; be = j; }
        }
        Console.WriteLine($"    {i,5}   {bestHere,24}   a[{i}..{endHere}] = [{string.Join(",", a[i..(endHere + 1)])}]");
    }

    Console.WriteLine();
    Console.WriteLine($"    best = {bruteBest} at a[{bb}..{be}], after {bruteSteps} steps");
    Console.WriteLine($"    (that is n(n+1)/2 = {a.Length * (a.Length + 1) / 2} for n = {a.Length})");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Kadane, narrated. One pass, two variables, one decision per index.
    // -------------------------------------------------------------------------
    Console.WriteLine("  KADANE — one pass, one decision per index");
    Console.WriteLine();
    Console.WriteLine("     i   a[i]   extend: cur+a[i]   fresh: a[i]   decision   current   best");
    Console.WriteLine("     -   ----   ---------------   -----------   --------   -------   ----");

    long current = a[0], best = a[0];
    int start = 0, bestStart = 0, bestEnd = 0;
    Console.WriteLine($"     0   {a[0],4}   {"-",15}   {"-",11}   {"(seed)",-8}   {current,7}   {best,4}");

    for (int i = 1; i < a.Length; i++)
    {
        long extend = current + a[i];
        long fresh = a[i];

        string decision;
        if (fresh > extend) { current = fresh; start = i; decision = "FRESH"; }
        else { current = extend; decision = "extend"; }

        if (current > best) { best = current; bestStart = start; bestEnd = i; }

        Console.WriteLine($"    {i,2}   {a[i],4}   {extend,15}   {fresh,11}   {decision,-8}   {current,7}   {best,4}");
    }

    Console.WriteLine();
    Console.WriteLine($"    best = {best} at a[{bestStart}..{bestEnd}] = [{string.Join(",", a[bestStart..(bestEnd + 1)])}]");
    Console.WriteLine($"    after {a.Length} steps — one per element.");
    Console.WriteLine();

    // Draw the winning run under the input so it is visible.
    Console.Write("    [");
    for (int i = 0; i < a.Length; i++) Console.Write($"{a[i],3}{(i < a.Length - 1 ? "," : "")}");
    Console.WriteLine("]");
    Console.Write("     ");
    for (int i = 0; i < a.Length; i++) Console.Write(i >= bestStart && i <= bestEnd ? "^^^ " : "    ");
    Console.WriteLine();
    Console.WriteLine($"     {new string(' ', bestStart * 4)}the winning run, sum {best}");
    Console.WriteLine();

    Console.WriteLine("""
      THE WORK THAT DISAPPEARED

      Look at the FRESH rows — i = 1 and i = 3. At i = 3 the running sum was -2,
      so extending would have given 2 while starting fresh gives 4. Kadane drops
      the -2 and never looks at it again.

      That is the one idea. A negative running sum cannot help any future
      subarray, because any run that includes it would be strictly better without
      it. So the moment the running total goes negative it is dead weight, you
      throw it away, and you never have to reconsider the decision.

      The brute force re-derives that same fact 45 times. It starts at index 0 and
      walks to the end, then starts at index 1 and walks to the end, re-adding
      cells it has already added. Kadane asks one question per element and
      commits.

      Equivalent one-liner for the same decision, which some people find clearer:

          current = max(current, 0) + a[i]        carry the prefix only if it helps
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The all-negative trap, shown rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE ALL-NEGATIVE TRAP, SHOWN");
    Console.WriteLine();

    int[] neg = [-3, -1, -2];
    Console.WriteLine($"  input: [{string.Join(", ", neg)}]  — every element is negative");
    Console.WriteLine();

    // Correct: seed from a[0], so the answer is forced to be a real element.
    long c1 = neg[0], b1 = neg[0];
    for (int i = 1; i < neg.Length; i++) { c1 = Math.Max(neg[i], c1 + neg[i]); b1 = Math.Max(b1, c1); }

    // Bug: seed best at 0, which silently allows the empty subarray.
    long c2 = 0, b2 = 0;
    for (int i = 0; i < neg.Length; i++) { c2 = Math.Max(neg[i], c2 + neg[i]); b2 = Math.Max(b2, c2); }

    Console.WriteLine($"    best = a[0]  (correct) : {b1}    <- a[1] alone, the least bad run");
    Console.WriteLine($"    best = 0     (bug)     : {b2}     <- the EMPTY subarray");
    Console.WriteLine();
    Console.WriteLine("  Seeding best at 0 claims an answer of 0, which requires picking no");
    Console.WriteLine("  elements at all. If the problem says \"non-empty subarray\" — and it");
    Console.WriteLine("  almost always does — that is wrong. Seed from a[0] and loop from 1.");
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
    Console.WriteLine("    case                      input                             expect           brute          kadane");
    Console.WriteLine("    -----------------------   ----------------------------   --------------   -------------   -------------");

    int passed = 0, total = 0;

    void Check(string name, int[] a, long expect)
    {
        total++;
        long brute = Subarrays.MaxSumBrute(a);
        long kadane = Subarrays.MaxSumKadane(a);
        bool ok = brute == expect && kadane == expect;
        if (ok) passed++;

        string shown = $"[{string.Join(",", a)}]";
        if (shown.Length > 28) shown = shown[..25] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-28}   {expect,14}   {brute,13}   {kadane,13}");
    }

    Check("single positive", [5], 5);
    Check("single negative", [-5], -5);
    Check("single zero", [0], 0);
    Check("two elements", [-1, 4], 4);
    Check("all positive", [1, 2, 3, 4], 10);
    Check("all negative", [-3, -1, -2], -1);
    Check("all zeros", [0, 0, 0], 0);
    Check("the classic", [-2, 1, -3, 4, -1, 2, 1, -5, 4], 6);
    Check("best at start", [5, -1, -1, -1], 5);
    Check("best at end", [-1, -1, -1, 5], 5);
    Check("whole array wins", [2, 3, 4], 9);
    Check("big dip in middle", [10, -100, 10], 10);
    Check("dip worth crossing", [10, -1, 10], 19);
    Check("alternating", [1, -1, 1, -1, 1], 1);
    Check("negatives then pos", [-5, -4, 3], 3);

    // Overflow: every element fits in an int, their sum does not. This is why
    // both implementations accumulate in long.
    int[] big = new int[1000];
    Array.Fill(big, int.MaxValue);
    Check("overflow (int.Max)", big, 1000L * int.MaxValue);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Three of these earn their place.

          "all negative" is the one that catches a best = 0 seed. It must return
          -1, not 0 — see the bottom of -- trace for what the bug looks like.

          "big dip in middle" and "dip worth crossing" are the same shape with
          one number changed, and they must disagree. [10,-100,10] -> 10: the dip
          is not worth crossing, so start fresh. [10,-1,10] -> 19: it is, so
          extend. An implementation that always does one or the other passes one
          of these and fails the other.
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
    Console.WriteLine("  Neither implementation can exit early on any input — both always");
    Console.WriteLine("  look at everything — so every input is the worst case and these");
    Console.WriteLine("  numbers are exact, not typical.");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   kadane steps        ratio");
    Console.WriteLine("    -----   -----------------   ------------   ----------");

    var rng = new Random(42);
    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(-100, 101);

        Subarrays.Steps = 0;
        Subarrays.MaxSumBrute(data);
        long brute = Subarrays.Steps;

        Subarrays.Steps = 0;
        Subarrays.MaxSumKadane(data);
        long kadane = Subarrays.Steps;

        Console.WriteLine($"    {n,5}   {brute,17:N0}   {kadane,12:N0}   {(double)brute / kadane,9:N0}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The brute-force column is n(n+1)/2 and the kadane column is exactly n, so
      the ratio is (n+1)/2 and grows without limit.

      What is unusual here compared with the other algorithms in this node: there
      is no extra memory and no precomputed table. PrefixSums and DifferenceArray
      both bought their speed with an O(n) array. Kadane buys it with an IDEA —
      the observation that a negative running sum is always worth discarding — and
      pays nothing. Both implementations are O(1) space.

      That makes it the cheapest win in the roadmap, and also the one you cannot
      derive mechanically. You either know the discard rule or you do not.
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
    Console.WriteLine("            brute force                     kadane");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    var rng = new Random(42);
    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(-100, 101);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Subarrays.MaxSumBrute(data);
        Subarrays.MaxSumKadane(data);

        Subarrays.Steps = 0;
        var sw = Stopwatch.StartNew();
        Subarrays.MaxSumBrute(data);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Subarrays.Steps;

        Subarrays.Steps = 0;
        sw.Restart();
        Subarrays.MaxSumKadane(data);
        double kadaneMs = sw.Elapsed.TotalMilliseconds;
        long kadaneSteps = Subarrays.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {kadaneSteps,12:N0}   {kadaneMs,6:N2}");
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
// Both return the largest sum of any NON-EMPTY contiguous subarray. Both throw
// on an empty input, because "the largest sum of no elements" has no answer
// under that rule. Both accumulate in long — see the overflow test.
// =============================================================================

static class Subarrays
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n^2) time, O(1) space.
    //
    // Fix a start, then extend the end one cell at a time, keeping the running
    // sum. This is already the SMART brute force: the naive-naive version
    // re-sums each range from scratch and is O(n^3). Carrying `running` across
    // the inner loop is itself a small act of not-repeating-work.
    //
    // What it still repeats: the walk from each start to the end of the array,
    // n times over, re-adding cells it has already added under a different
    // starting point.
    // -------------------------------------------------------------------------
    public static long MaxSumBrute(int[] a)
    {
        if (a.Length == 0)
            throw new ArgumentException("no non-empty subarray exists", nameof(a));

        long best = long.MinValue;

        for (int i = 0; i < a.Length; i++)
        {
            long running = 0;
            for (int j = i; j < a.Length; j++)
            {
                Steps++;                 // one cell added
                running += a[j];
                if (running > best) best = running;
            }
        }

        return best;
    }

    // -------------------------------------------------------------------------
    // KADANE — O(n) time, O(1) space.
    //
    // One pass. At each index, ask whether the best run ending here extends the
    // previous run or starts fresh — and that decision is final, because a
    // negative running sum can never help a later subarray.
    //
    // best is seeded from a[0], NOT from 0. Seeding at 0 would allow the empty
    // subarray and return 0 for all-negative input. See -- trace.
    // -------------------------------------------------------------------------
    public static long MaxSumKadane(int[] a)
    {
        if (a.Length == 0)
            throw new ArgumentException("no non-empty subarray exists", nameof(a));

        long current = a[0];             // best run ENDING at index 0
        long best = a[0];                // best run seen anywhere so far
        Steps++;                         // the seed reads one cell

        for (int i = 1; i < a.Length; i++)
        {
            Steps++;                     // one cell read

            // Extend the previous run, or abandon it and start at a[i]?
            // Equivalently: current = Math.Max(current, 0) + a[i];
            current = Math.Max(a[i], current + a[i]);

            if (current > best) best = current;
        }

        return best;
    }
}
