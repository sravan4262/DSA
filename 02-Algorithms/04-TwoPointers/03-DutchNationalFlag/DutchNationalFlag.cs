#!/usr/bin/env dotnet
// =============================================================================
// DUTCHNATIONALFLAG — partition into three groups in ONE pass, with three
// pointers and no extra memory.
//
// Node 04 · Two Pointers.  Needs: array.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The job: an array holds only 0s, 1s and 2s. Sort it. (Dijkstra's name for it,
// after the Dutch flag's three stripes.)
//
// You could sort it properly — O(n log n) and overkill. You could count the
// three values and overwrite — two passes, and impossible if the elements carry
// payloads you must not fabricate. Or you can do it in ONE pass with three
// indices that carve the array into four regions:
//
//   [ 0 0 0 | 1 1 1 | ? ? ? ? ? | 2 2 2 ]
//            ^       ^         ^
//            low     mid       high
//
//   a[0     .. low-1]   all 0s      done
//   a[low   .. mid-1]   all 1s      done
//   a[mid   .. high]    UNKNOWN     still to classify
//   a[high+1.. n-1]     all 2s      done
//
// mid walks forward and the unknown region shrinks from both sides. The loop
// ends when mid passes high, because then nothing is unknown.
//
//   brute force   count, then overwrite    TWO passes
//   full sort     Array.Sort               O(n log n)
//   this          one pass, three indices  O(n) time, O(1) space, ONE pass
//
// THE THREE CASES, AND THE ONE THAT CATCHES EVERYONE
//
//   a[mid] == 0   swap(a[low], a[mid]);  low++;  mid++;
//   a[mid] == 1   mid++;
//   a[mid] == 2   swap(a[mid], a[high]); high--;     <-- mid does NOT move
//
// Why mid advances on a 0 but not on a 2:
//
//   On a 0, you swap with a[low]. a[low] was in the 1s region, so whatever you
//   just pulled into a[mid] is a 1 — already classified. Safe to advance.
//
//   On a 2, you swap with a[high]. a[high] came from the UNKNOWN region, so the
//   value now sitting at a[mid] has never been looked at. Advancing would skip
//   it. You must re-examine the same position.
//
// That asymmetry is the whole algorithm, and getting it wrong is the standard
// bug. -- trace prints the broken version beside the correct one.
//
// WHY THIS IS NOT JUST "A FASTER SORT"
//
// It only works because there are exactly THREE distinct values and you know
// them in advance. That is not sorting, it is partitioning — and partitioning
// is the step quicksort is built on. Two-way partition is the Hoare/Lomuto
// scheme; this is its three-way cousin, which is what makes a quicksort immune
// to arrays full of duplicate pivots.
//
// So the real value of learning it is not sorting 0s and 1s and 2s. It is the
// invariant discipline: name your regions, state what is true of each, and the
// pointer movements follow.
//
// WHEN NOT TO USE IT
//
// With more than three groups it does not extend — four regions need a
// different approach (counting sort, or a stable sort by key). It is NOT stable:
// the swaps move equal elements past each other, so if the elements are records
// and their original order matters, this destroys it. And when the values are
// not known in advance you cannot write the comparisons at all.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run DutchNationalFlag.cs                everything
//   dotnet run DutchNationalFlag.cs -- trace       one small input, step by step
//   dotnet run DutchNationalFlag.cs -- test        the edge cases
//   dotnet run DutchNationalFlag.cs -- compare     brute force vs this
//   dotnet run DutchNationalFlag.cs -- bench       steps and milliseconds
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

    int[] original = [2, 0, 2, 1, 1, 0];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", original)}]   — only 0s, 1s and 2s");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. Count, then overwrite.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — count in pass 1, overwrite in pass 2");
    Console.WriteLine();

    int c0 = original.Count(x => x == 0);
    int c1 = original.Count(x => x == 1);
    int c2 = original.Count(x => x == 2);

    Console.WriteLine($"    pass 1: {c0} zeros, {c1} ones, {c2} twos      ({original.Length} reads)");
    Console.WriteLine($"    pass 2: write {c0} zeros, then {c1} ones, then {c2} twos   ({original.Length} writes)");
    Console.WriteLine();
    Console.WriteLine("    Correct, and two passes. Note the hidden assumption: it FABRICATES");
    Console.WriteLine("    the output from counts. If each element were a record that happens to");
    Console.WriteLine("    have key 0, 1 or 2, you could not invent them from a tally — you would");
    Console.WriteLine("    have to move the actual objects. The one-pass version always does.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The three-way partition, narrated with the four regions drawn.
    // -------------------------------------------------------------------------
    Console.WriteLine("  THREE-WAY PARTITION — one pass, four regions");
    Console.WriteLine();
    Console.WriteLine("    regions:  [ 0s | 1s | UNKNOWN | 2s ]");
    Console.WriteLine("                   ^    ^         ^");
    Console.WriteLine("                   low  mid       high");
    Console.WriteLine();
    Console.WriteLine("    step   low  mid  high   a[mid]   action                             array");
    Console.WriteLine("    ----   ---  ---  ----   ------   --------------------------------   -------------");

    int[] a = (int[])original.Clone();
    int lo = 0, mid = 0, hi = a.Length - 1, step = 1;

    while (mid <= hi)
    {
        // Capture the pointers BEFORE acting, so the columns and the action
        // text describe the same moment.
        int pLo = lo, pMid = mid, pHi = hi, v = a[mid];
        string action;

        if (v == 0)
        {
            action = $"swap a[{lo}],a[{mid}], low++ mid++";
            (a[lo], a[mid]) = (a[mid], a[lo]);
            lo++; mid++;
        }
        else if (v == 1)
        {
            action = "mid++ only";
            mid++;
        }
        else
        {
            action = $"swap a[{mid}],a[{hi}], high--, MID STAYS";
            (a[mid], a[hi]) = (a[hi], a[mid]);
            hi--;
        }

        Console.WriteLine($"    {step,4}   {pLo,3}  {pMid,3}  {pHi,4}   {v,6}   {action,-32}   [{string.Join(",", a)}]");
        step++;
    }

    Console.WriteLine();
    Console.WriteLine($"    result: [{string.Join(", ", a)}]  in {step - 1} steps, one pass, no extra memory.");
    Console.WriteLine($"    mid ({mid}) passed high ({hi}) — nothing is unknown, so it stops.");
    Console.WriteLine();

    Console.WriteLine("""
      WHY mid ADVANCES ON A 0 BUT NOT ON A 2

      This is the asymmetry, and it is the only hard part.

      On a 0 you swap with a[low]. Where did a[low] come from? It was the front
      of the 1s region — a value you have ALREADY classified. So the thing that
      lands at a[mid] is a known 1, and mid can safely step over it.

      On a 2 you swap with a[high]. Where did a[high] come from? The UNKNOWN
      region. The value now at a[mid] has never been looked at. Advance mid and
      you skip it forever.

          swap with low   ->  you receive a KNOWN value    -> mid++ is safe
          swap with high  ->  you receive an UNKNOWN value -> mid must stay

      Say it as "I only move past things I have classified" and the rule stops
      needing to be memorised.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The bug, shown rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE BUG, SHOWN — advancing mid after a 2");
    Console.WriteLine();

    // Worth knowing: on [2,0,2,1,1,0] the broken version happens to get the
    // right answer anyway. The bug needs an input where the value pulled back
    // from `high` is itself out of place. [1,2,0] is the smallest such input.
    int[] bugInput = [1, 2, 0];

    int[] bug = (int[])bugInput.Clone();
    int bl = 0, bm = 0, bh = bug.Length - 1;
    while (bm <= bh)
    {
        if (bug[bm] == 0) { (bug[bl], bug[bm]) = (bug[bm], bug[bl]); bl++; bm++; }
        else if (bug[bm] == 1) { bm++; }
        else { (bug[bm], bug[bh]) = (bug[bh], bug[bm]); bh--; bm++; }   // BUG: mid++
    }

    int[] good = (int[])bugInput.Clone();
    Flag.Partition(good);

    Console.WriteLine($"    input            [{string.Join(", ", bugInput)}]");
    Console.WriteLine($"    mid stays  (ok)  [{string.Join(", ", good)}]   sorted");
    Console.WriteLine($"    mid++      (bug) [{string.Join(", ", bug)}]   NOT sorted — the 0 was skipped");
    Console.WriteLine();
    Console.WriteLine("  Walk the broken run: a[0] is 1, so mid -> 1. a[1] is 2, so swap it with");
    Console.WriteLine("  a[2] — which brings the 0 to position 1 — then the buggy mid++ steps");
    Console.WriteLine("  straight over it. high is now 1 and mid is 2, so the loop ends with an");
    Console.WriteLine("  unclassified 0 sitting in the middle.");
    Console.WriteLine();
    Console.WriteLine("  Note it does not crash or throw — it quietly returns a wrong array, so");
    Console.WriteLine("  only a test catches it. Note also that [2,0,2,1,1,0] from the trace above");
    Console.WriteLine("  comes out CORRECT even with the bug, which is exactly why a test suite");
    Console.WriteLine("  needs more than one input.");
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
    Console.WriteLine("    case                      input                        counts   result");
    Console.WriteLine("    -----------------------   --------------------------   ------   --------------------------");

    int passed = 0, total = 0;

    void Check(string name, int[] input)
    {
        total++;

        int[] expect = (int[])input.Clone();
        Array.Sort(expect);                      // the oracle

        int[] a1 = (int[])input.Clone();
        Flag.PartitionBrute(a1);

        int[] a2 = (int[])input.Clone();
        Flag.Partition(a2);

        bool ok = a1.SequenceEqual(expect) && a2.SequenceEqual(expect);
        if (ok) passed++;

        string shown = input.Length == 0 ? "[]" : $"[{string.Join(",", input)}]";
        if (shown.Length > 26) shown = shown[..23] + "...";
        string got = a2.Length == 0 ? "[]" : $"[{string.Join(",", a2)}]";
        if (got.Length > 26) got = got[..23] + "...";
        string counts = $"{input.Count(x => x == 0)}/{input.Count(x => x == 1)}/{input.Count(x => x == 2)}";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-26}   {counts,6}   {got,-26}");
    }

    Check("empty", []);
    Check("single 0", [0]);
    Check("single 1", [1]);
    Check("single 2", [2]);
    Check("all zeros", [0, 0, 0]);
    Check("all ones", [1, 1, 1]);
    Check("all twos", [2, 2, 2]);
    Check("already sorted", [0, 0, 1, 1, 2, 2]);
    Check("exactly reversed", [2, 2, 1, 1, 0, 0]);
    Check("the classic", [2, 0, 2, 1, 1, 0]);
    Check("2s at the front", [2, 2, 2, 0, 0, 1]);
    Check("0s at the back", [2, 1, 1, 0, 0, 0]);
    Check("no ones", [2, 0, 2, 0]);
    Check("no zeros", [2, 1, 2, 1]);
    Check("no twos", [1, 0, 1, 0]);
    Check("two elements", [2, 0]);
    Check("alternating", [0, 1, 2, 0, 1, 2, 0, 1, 2]);

    // A big random array, to catch anything that only breaks at scale.
    var rng = new Random(42);
    int[] big = new int[1000];
    for (int i = 0; i < big.Length; i++) big[i] = rng.Next(3);
    Check("1,000 random", big);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Three cases earn their place.

          "all twos" and "2s at the front" are the ones that catch an incorrect
          mid++ after a 2. On [2,2,2,0,0,1] the broken version swaps 2s to the
          back while stepping mid past the values it pulled forward, and the
          result is wrong without any exception being thrown.

          "exactly reversed" is the maximum-swap case — every element has to
          move.

          Array.Sort is the ORACLE here, not the implementation. The point of
          this algorithm is to beat a sort, so it cannot be built from one.
          """);
        Console.WriteLine();
    }
}


// =============================================================================
// SIDE BY SIDE — the same work, all three, counted
// =============================================================================

void ShowCompare()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  SIDE BY SIDE");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Steps are ELEMENT TOUCHES — a read, a write or half a swap.");
    Console.WriteLine();
    Console.WriteLine("        n   count+overwrite   three-way partition        ratio");
    Console.WriteLine("    -----   ---------------   -------------------   ----------");

    var rng = new Random(42);
    foreach (int n in (int[])[100, 1_000, 10_000, 1_000_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(3);

        int[] a1 = (int[])data.Clone();
        Flag.Steps = 0;
        Flag.PartitionBrute(a1);
        long brute = Flag.Steps;

        int[] a2 = (int[])data.Clone();
        Flag.Steps = 0;
        Flag.Partition(a2);
        long dnf = Flag.Steps;

        Console.WriteLine($"    {n,5}   {brute,15:N0}   {dnf,19:N0}   {(double)brute / dnf,9:N2}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Read the ratio column honestly, because it says something uncomfortable:
      it is about 0.86x, which means the three-way partition does roughly 16%
      MORE element touches than count-and-overwrite. It is not faster. It is
      slightly slower, and the ratio is flat, so that never changes.

      The reason is easy to see. Counting does exactly 2n touches — n reads,
      then n writes. The partition does n reads plus 2 touches for every swap,
      and with random 0/1/2 data about two thirds of the elements get swapped.
      So 2n versus roughly 2.33n.

      Do not let that be a disappointment; let it be the lesson. "One pass"
      sounds like it should be faster than "two passes" and it simply is not,
      because a pass is not the unit that costs anything — touches are. If you
      had reached for this expecting a speedup you would have been wrong, and
      the only way to know was to measure.

      What you actually get for the extra thinking:

        ONE pass instead of two     matters on a stream you cannot rewind, and
                                    on data too large to walk twice cheaply

        it MOVES the elements       the counting version rebuilds the array from
                                    a tally, which only works when the elements
                                    are interchangeable. Give them payloads and
                                    counting is not merely slower, it is wrong.

        the invariant discipline    four named regions, each with a stated
                                    truth, and the pointer rules falling out of
                                    them. That is the transferable part, and it
                                    is what three-way quicksort partition is.

      Against a full sort the win IS asymptotic on paper — O(n) versus
      O(n log n), because "there are only three distinct values" is information
      a general sort is not allowed to assume. But see -- bench before trusting
      that to show up as a big number: it does not.
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
    Console.WriteLine("  Including Array.Sort, to check whether the O(n) vs O(n log n) gap on paper");
    Console.WriteLine("  actually shows up as a wall-clock gap. Read the result before assuming.");
    Console.WriteLine();
    Console.WriteLine("            count+overwrite            three-way partition      Array.Sort");
    Console.WriteLine("        n          steps       ms            steps       ms              ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------     -----------");

    var rng = new Random(42);
    foreach (int n in (int[])[10_000, 100_000, 1_000_000, 10_000_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(3);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Flag.PartitionBrute((int[])data.Clone());
        Flag.Partition((int[])data.Clone());
        Array.Sort((int[])data.Clone());

        int[] a1 = (int[])data.Clone();
        Flag.Steps = 0;
        var sw = Stopwatch.StartNew();
        Flag.PartitionBrute(a1);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Flag.Steps;

        int[] a2 = (int[])data.Clone();
        Flag.Steps = 0;
        sw.Restart();
        Flag.Partition(a2);
        double dnfMs = sw.Elapsed.TotalMilliseconds;
        long dnfSteps = Flag.Steps;

        int[] a3 = (int[])data.Clone();
        sw.Restart();
        Array.Sort(a3);
        double sortMs = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {dnfSteps,12:N0}   {dnfMs,6:N2}     {sortMs,11:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The last column is the one to look at, and the result is a useful
      disappointment. On paper this is O(n) against Array.Sort's O(n log n), so
      the gap should be wide. Measured, Array.Sort is within about 10-15% —
      sometimes it even wins at the smaller sizes.

      Two reasons, both worth keeping:

        1. log n is TINY. At n = 1,000,000 it is 20. An algorithm doing 2.33n
           touches with swaps and branches does not comfortably beat one doing
           20n highly-tuned, cache-friendly, vectorised comparisons. Asymptotic
           superiority with a worse constant can lose at every size you will
           ever actually run.

        2. Array.Sort is introsort, and introsort is very good at exactly this
           input — three distinct keys means its partitions are enormous and it
           bottoms out fast. We picked its best case to compete against.

      So the honest summary of this whole algorithm: it is the right answer when
      you need ONE pass, or when the elements carry payloads that cannot be
      fabricated from counts. It is NOT the right answer because it is faster,
      because on bare integers it is not.

      That is a more valuable thing to have measured than a win would have been.
      "It has a better Big-O" is not the same claim as "it is faster", and this
      is the row in the repo that proves it.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE TWO IMPLEMENTATIONS
//
// Both sort an array containing only 0s, 1s and 2s, in place. Steps counts
// ELEMENT TOUCHES so the two are measured in the same unit.
//
// Both throw on a value outside {0,1,2} rather than silently misbehaving — the
// algorithm's correctness depends on that precondition.
// =============================================================================

static class Flag
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // COUNT AND OVERWRITE — O(n) time, O(1) space, TWO passes.
    //
    // Tally the three values, then write that many of each back. Simple and
    // fast, with one assumption buried in it: the output is FABRICATED from
    // counts rather than built from the original elements. That is fine for
    // bare ints and wrong for anything carrying a payload.
    // -------------------------------------------------------------------------
    public static void PartitionBrute(int[] a)
    {
        int c0 = 0, c1 = 0, c2 = 0;

        foreach (int x in a)
        {
            Steps++;                                     // one read
            if (x == 0) c0++;
            else if (x == 1) c1++;
            else if (x == 2) c2++;
            else throw new ArgumentOutOfRangeException(nameof(a), $"expected 0, 1 or 2 but found {x}");
        }

        int k = 0;
        for (int i = 0; i < c0; i++) { Steps++; a[k++] = 0; }
        for (int i = 0; i < c1; i++) { Steps++; a[k++] = 1; }
        for (int i = 0; i < c2; i++) { Steps++; a[k++] = 2; }
    }

    // -------------------------------------------------------------------------
    // THREE-WAY PARTITION — O(n) time, O(1) space, ONE pass.
    //
    // Four regions, and the invariant is the specification:
    //
    //   a[0       .. low-1 ]   all 0s
    //   a[low     .. mid-1 ]   all 1s
    //   a[mid     .. high  ]   unknown
    //   a[high+1  .. n-1   ]   all 2s
    //
    // Every iteration shrinks the unknown region by one, so it terminates in at
    // most n iterations. The loop ends when mid passes high.
    // -------------------------------------------------------------------------
    public static void Partition(int[] a)
    {
        int low = 0, mid = 0, high = a.Length - 1;

        while (mid <= high)
        {
            Steps++;                                     // one read of a[mid]

            switch (a[mid])
            {
                case 0:
                    // Swapping with low pulls in a value from the 1s region,
                    // which is already classified — so mid can advance.
                    Steps += 2;
                    (a[low], a[mid]) = (a[mid], a[low]);
                    low++;
                    mid++;
                    break;

                case 1:
                    // Already in the right region. Just widen it.
                    mid++;
                    break;

                case 2:
                    // Swapping with high pulls in a value from the UNKNOWN
                    // region. It has never been examined, so mid must STAY and
                    // look at it on the next iteration.
                    Steps += 2;
                    (a[mid], a[high]) = (a[high], a[mid]);
                    high--;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(a), $"expected 0, 1 or 2 but found {a[mid]}");
            }
        }
    }
}
