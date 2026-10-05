#!/usr/bin/env dotnet
// =============================================================================
// CONVERGINGPOINTERS — start at both ends, walk inward, eliminate a whole row
// of the search space on every step.
//
// Node 04 · Two Pointers.  Needs: array, sorting.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The job used here: given a SORTED array and a target, find two elements that
// sum to it. The obvious approach tries every pair — O(n^2).
//
// Instead, put one pointer at each end and let the comparison tell you which one
// to move:
//
//   sum < target   ->  left++     you need a BIGGER sum
//   sum > target   ->  right--    you need a SMALLER sum
//   sum == target  ->  found
//
//   brute force   every pair                    O(n^2) time, O(1) space
//   this          one pass from both ends       O(n) time,   O(1) space
//
// WHY MOVING ONE POINTER IS SAFE — the only thing that matters here
//
// This is not "try fewer pairs and hope". Each step PROVES a whole set of pairs
// impossible, and that proof is what the sortedness buys you.
//
// Say sum = a[l] + a[r] < target. a[r] is the LARGEST remaining element, so
// a[l] + a[r] is the largest sum a[l] can ever participate in. If even that is
// too small, then a[l] paired with anything else is also too small. a[l] is
// eliminated — not skipped, eliminated — and l++ is sound.
//
// The mirror argument holds for right--. So each step removes one full row or
// column from the n x n grid of pairs, and n steps cover a grid of n^2.
//
//   target = 10, a = [1, 3, 4, 6, 8, 11]
//
//        1+11 = 12 > 10   ->  11 can never work with ANYTHING (1 is the
//                             smallest, so 11's smallest sum is already too
//                             big). Drop the whole 11 column.
//
// THE PRECONDITION IS SORTEDNESS, AND IT IS NOT OPTIONAL
//
// On unsorted input the comparison tells you nothing — a[r] is not the largest
// remaining, so "even the biggest sum is too small" is not a statement you can
// make. The technique silently returns wrong answers rather than failing loudly,
// which makes it a nasty bug.
//
// If the input is not sorted you must sort it first, which costs O(n log n) and
// means the whole thing is O(n log n) — still better than O(n^2), but no longer
// O(n), and sorting destroys the original indices. If the problem wants indices
// into the ORIGINAL array and the array is unsorted, use a hash table instead
// (HashMapCounting, node 01): O(n) and no sorting.
//
// THE SAME SHAPE, OTHER JOBS
//
// Converging pointers is a shape, not one algorithm. Also in this file:
//
//   IsPalindrome      compare from both ends, walk in. No sortedness needed —
//                     here the pointers encode symmetry rather than ordering.
//   ReverseInPlace    swap the ends, walk in. O(1) space.
//
// What they share: two pointers starting apart and meeting in the middle, with
// total movement of exactly n, so the loop is O(n).
//
// WHEN NOT TO USE IT
//
// When the data is not sorted and you cannot afford to sort it, or you need
// original indices. When you need every pair rather than one (then it is still
// useful, but as the inner loop of ThreeSum rather than on its own). And when
// the pointers should move in the SAME direction rather than toward each other
// — that is a sliding window (node 07) or a write pointer (node 01), not this.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run ConvergingPointers.cs                everything
//   dotnet run ConvergingPointers.cs -- trace       one small input, step by step
//   dotnet run ConvergingPointers.cs -- test        the edge cases
//   dotnet run ConvergingPointers.cs -- compare     brute force vs this, in steps
//   dotnet run ConvergingPointers.cs -- bench       steps and milliseconds
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

    int[] a = [1, 3, 4, 6, 8, 11];
    int target = 10;

    Console.WriteLine();
    Console.WriteLine($"  input:  [{string.Join(", ", a)}]   (SORTED — this is required)");
    Console.WriteLine($"  target: {target}");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. Every pair, until one hits.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — try every pair");
    Console.WriteLine();
    Console.WriteLine("    pairs tried");
    Console.WriteLine("    -----------------------------------------------------------------");

    long bruteSteps = 0;
    var tried = new List<string>();
    bool found = false;
    for (int i = 0; i < a.Length && !found; i++)
        for (int j = i + 1; j < a.Length && !found; j++)
        {
            bruteSteps++;
            tried.Add($"{a[i]}+{a[j]}");
            if (a[i] + a[j] == target) found = true;
        }

    // Wrap the list so it stays readable.
    for (int k = 0; k < tried.Count; k += 10)
        Console.WriteLine($"    {string.Join("  ", tried.Skip(k).Take(10))}");

    Console.WriteLine();
    Console.WriteLine($"    {bruteSteps} pairs tried before the hit.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Converging pointers, narrated. One row per step, and what it eliminates.
    // -------------------------------------------------------------------------
    Console.WriteLine("  CONVERGING POINTERS — one step per elimination");
    Console.WriteLine();
    Console.WriteLine("    step    l    r   a[l]+a[r]    vs target   move      what it PROVES impossible");
    Console.WriteLine("    ----   --   --   ---------   ----------   -------   -------------------------------");

    int l = 0, r = a.Length - 1, step = 1;
    while (l < r)
    {
        int sum = a[l] + a[r];
        string vs, move, proves;

        if (sum < target)
        {
            vs = "too small"; move = "l++";
            proves = $"every pair using {a[l]}";
        }
        else if (sum > target)
        {
            vs = "too big"; move = "r--";
            proves = $"every pair using {a[r]}";
        }
        else
        {
            vs = "FOUND"; move = "stop"; proves = "-";
        }

        Console.WriteLine($"    {step,4}   {l,2}   {r,2}   {$"{a[l]}+{a[r]} = {sum}",9}   {vs,-10}   {move,-7}   {proves}");

        if (sum == target) break;
        if (sum < target) l++; else r--;
        step++;
    }

    Console.WriteLine();
    Console.WriteLine($"    found a[{l}] + a[{r}] = {a[l]} + {a[r]} = {target} in {step} steps.");
    Console.WriteLine();

    // Draw the window closing, so the convergence is visible.
    Console.WriteLine("    the window closing:");
    Console.WriteLine();
    int l2 = 0, r2 = a.Length - 1;
    while (true)
    {
        Console.Write("      ");
        for (int i = 0; i < a.Length; i++)
            Console.Write(i == l2 ? $"[{a[i],2}]" : i == r2 ? $"[{a[i],2}]" : $" {a[i],2} ");
        Console.WriteLine($"    {a[l2]}+{a[r2]} = {a[l2] + a[r2]}");

        if (a[l2] + a[r2] == target || l2 >= r2) break;
        if (a[l2] + a[r2] < target) l2++; else r2--;
    }

    Console.WriteLine();
    Console.WriteLine("""
      THE WORK THAT DISAPPEARED

      Read the last column of the step table. Every step does not merely skip a
      pair — it rules out a whole ROW of the n x n grid of pairs, with a proof.

      Step 1: 1 + 11 = 12, too big. 1 is the smallest element in the array, so
      12 is the SMALLEST sum that 11 can possibly be part of. If the smallest is
      already too big, 11 is impossible with every partner. Drop it entirely.

      That proof is the whole technique, and it exists only because the array is
      sorted. On unsorted input a[r] is not the largest remaining, the proof
      collapses, and the method returns a wrong answer without complaining.

      The pointers move toward each other and never back, so total movement is
      exactly n - 1. One pass, O(n), covering a space of n^2 pairs.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The same shape, a different job: symmetry rather than ordering.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE SAME SHAPE, A DIFFERENT JOB — palindrome");
    Console.WriteLine();

    string word = "racecar";
    Console.WriteLine($"    input: \"{word}\"");
    Console.WriteLine();
    Console.WriteLine("    step    l    r   a[l]   a[r]   match?");
    Console.WriteLine("    ----   --   --   ----   ----   ------");

    int pl = 0, pr = word.Length - 1, ps = 1;
    while (pl < pr)
    {
        bool match = word[pl] == word[pr];
        Console.WriteLine($"    {ps,4}   {pl,2}   {pr,2}   {word[pl],4}   {word[pr],4}   {(match ? "yes" : "NO"),6}");
        if (!match) break;
        pl++; pr--; ps++;
    }

    Console.WriteLine();
    Console.WriteLine($"    palindrome: {Pairs.IsPalindrome(word)}  —  {word.Length / 2} comparisons for {word.Length} characters");
    Console.WriteLine();
    Console.WriteLine("  Note what is NOT needed here: sortedness. The two-sum version uses the");
    Console.WriteLine("  pointers to exploit ORDERING; this one uses them to exploit SYMMETRY.");
    Console.WriteLine("  Same shape, different reason it works. That is why it is worth learning");
    Console.WriteLine("  as a shape rather than as one algorithm.");
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
    Console.WriteLine("  TwoSum on a sorted array. (-1,-1) means no pair exists.");
    Console.WriteLine();
    Console.WriteLine("    case                      input                     target   expect     brute      converge");
    Console.WriteLine("    -----------------------   -----------------------   ------   --------   --------   --------");

    int passed = 0, total = 0;

    void Check(string name, int[] a, int target, (int, int) expect)
    {
        total++;
        var brute = Pairs.TwoSumBrute(a, target);
        var conv = Pairs.TwoSumConverging(a, target);
        bool ok = brute == expect && conv == expect;
        if (ok) passed++;

        string shown = a.Length == 0 ? "[]" : $"[{string.Join(",", a)}]";
        if (shown.Length > 23) shown = shown[..20] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-23}   {target,6}   {$"({expect.Item1},{expect.Item2})",-8}   {$"({brute.Item1},{brute.Item2})",-8}   {$"({conv.Item1},{conv.Item2})",-8}");
    }

    Check("empty", [], 5, (-1, -1));
    Check("single", [5], 5, (-1, -1));
    Check("two, hit", [2, 3], 5, (0, 1));
    Check("two, miss", [2, 3], 9, (-1, -1));
    Check("at both ends", [1, 5, 9, 11], 12, (0, 3));
    Check("adjacent middle", [1, 3, 4, 6, 8, 11], 10, (2, 3));
    Check("first two", [1, 2, 90, 99], 3, (0, 1));
    Check("last two", [1, 2, 90, 99], 189, (2, 3));
    Check("no pair", [1, 3, 4, 6, 8, 11], 100, (-1, -1));
    Check("target too small", [1, 3, 4], 2, (-1, -1));
    Check("duplicates", [3, 3, 3, 3], 6, (0, 3));
    Check("negatives", [-8, -3, 0, 2, 7], -1, (0, 4));
    Check("sums to zero", [-5, -2, 2, 5], 0, (0, 3));
    Check("all identical, miss", [4, 4, 4], 9, (-1, -1));

    // Overflow: two values near int.MaxValue. a[l] + a[r] as ints would wrap
    // negative and silently report "too small". The implementations add in long.
    Check("overflow pair", [int.MaxValue - 1, int.MaxValue], -1, (-1, -1));

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Two of these earn their place.

          "duplicates" — [3,3,3,3] with target 6 returns (0,3), not (0,1). The
          converging version finds the OUTERMOST pair because that is where its
          pointers start, and the brute force is written to agree. Neither is
          more correct; it is a choice the problem statement has to pin down, and
          a test is how you notice the choice exists.

          "overflow pair" — a[l] + a[r] on two values near int.MaxValue wraps to
          a negative number, which reads as "too small", which moves the WRONG
          pointer. Both implementations accumulate in long so the comparison
          stays honest. This is the kind of bug that never shows up on small
          test data.
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
    Console.WriteLine("  Worst case for both: NO pair sums to the target, so neither can exit");
    Console.WriteLine("  early. Steps are pair evaluations.");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   converging steps        ratio");
    Console.WriteLine("    -----   -----------------   ----------------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        // Sorted evens, odd target -> no pair can ever sum to it.
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i * 2;
        int target = 1;

        Pairs.Steps = 0;
        Pairs.TwoSumBrute(data, target);
        long brute = Pairs.Steps;

        Pairs.Steps = 0;
        Pairs.TwoSumConverging(data, target);
        long conv = Pairs.Steps;

        Console.WriteLine($"    {n,5}   {brute,17:N0}   {conv,16:N0}   {(double)brute / conv,9:N0}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The brute-force column is n(n-1)/2 and the converging column is n-1, so
      the ratio is n/2 and grows without limit.

      Both are O(1) space, like Kadane and unlike the rest of node 01 — the win
      is bought with the sortedness precondition rather than with memory. That
      precondition is the real price, and it is worth stating as a cost:

        input already sorted    O(n), free
        input unsorted          O(n log n) to sort first, and the original
                                indices are destroyed
        need original indices   use a hash table instead, O(n)

      So "two pointers beats the nested loop" is only true once you have said
      what you paid for the ordering.
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
    Console.WriteLine("  No pair exists, so neither implementation exits early.");
    Console.WriteLine();
    Console.WriteLine("            brute force                   converging");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int n in (int[])[1_000, 10_000, 50_000, 100_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i * 2;
        int target = 1;

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Pairs.TwoSumBrute(data, target);
        Pairs.TwoSumConverging(data, target);

        Pairs.Steps = 0;
        var sw = Stopwatch.StartNew();
        Pairs.TwoSumBrute(data, target);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Pairs.Steps;

        Pairs.Steps = 0;
        sw.Restart();
        Pairs.TwoSumConverging(data, target);
        double convMs = sw.Elapsed.TotalMilliseconds;
        long convSteps = Pairs.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {convSteps,12:N0}   {convMs,6:N2}");
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
// THE IMPLEMENTATIONS
//
// TwoSumBrute and TwoSumConverging both return the indices of a pair summing to
// target, or (-1, -1) if none exists. Both require a SORTED input and both
// return the OUTERMOST such pair, so they agree on inputs with duplicates.
//
// Sums accumulate in long — see the overflow test.
// =============================================================================

static class Pairs
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n^2) time, O(1) space.
    //
    // Every pair. Does not need the array sorted, which is worth noticing: the
    // brute force is MORE general than the fast version. That is usually the
    // shape of these trades — the optimisation buys speed with a precondition.
    //
    // The loops are ordered so the first hit found is the outermost pair, to
    // match TwoSumConverging on duplicate input.
    // -------------------------------------------------------------------------
    public static (int, int) TwoSumBrute(int[] a, int target)
    {
        for (int i = 0; i < a.Length; i++)
            for (int j = a.Length - 1; j > i; j--)      // from the far end inward
            {
                Steps++;                                 // one pair evaluated
                if ((long)a[i] + a[j] == target) return (i, j);
            }

        return (-1, -1);
    }

    // -------------------------------------------------------------------------
    // CONVERGING POINTERS — O(n) time, O(1) space. REQUIRES SORTED INPUT.
    //
    // One pointer at each end. The comparison names which one to move, and
    // moving it eliminates every remaining pair involving the element left
    // behind — see the trace for the proof.
    //
    // Total movement is n-1, so the loop runs at most n-1 times.
    // -------------------------------------------------------------------------
    public static (int, int) TwoSumConverging(int[] a, int target)
    {
        int l = 0, r = a.Length - 1;

        while (l < r)
        {
            Steps++;                                     // one pair evaluated

            // long, not int: two values near int.MaxValue would wrap negative
            // and read as "too small", moving the wrong pointer.
            long sum = (long)a[l] + a[r];

            if (sum == target) return (l, r);
            if (sum < target) l++;                       // a[l] is impossible with anything
            else r--;                                    // a[r] is impossible with anything
        }

        return (-1, -1);
    }

    // -------------------------------------------------------------------------
    // THE SAME SHAPE, SYMMETRY INSTEAD OF ORDERING — O(n) time, O(1) space.
    //
    // No sortedness required. The pointers encode "these two positions must
    // agree", which is a different reason for the same movement pattern.
    // -------------------------------------------------------------------------
    public static bool IsPalindrome(string s)
    {
        int l = 0, r = s.Length - 1;

        while (l < r)
        {
            if (s[l] != s[r]) return false;
            l++; r--;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // AND AGAIN — reverse in place by swapping the ends inward. O(1) space.
    // -------------------------------------------------------------------------
    public static void ReverseInPlace(int[] a)
    {
        int l = 0, r = a.Length - 1;

        while (l < r)
        {
            (a[l], a[r]) = (a[r], a[l]);
            l++; r--;
        }
    }
}
