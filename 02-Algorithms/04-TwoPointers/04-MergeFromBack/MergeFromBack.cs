#!/usr/bin/env dotnet
// =============================================================================
// MERGEFROMBACK — merge two sorted arrays in place by filling from the END,
// where the free space already is.
//
// Node 04 · Two Pointers.  Needs: array, merge sort.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The setup: array a has m real values at the front and n empty slots after
// them. Array b has n values. Both are sorted. Merge b into a, in place.
//
//   a = [1, 3, 5, _, _, _]      m = 3
//   b = [2, 4, 6]               n = 3
//   want a = [1, 2, 3, 4, 5, 6]
//
// Merging FORWARD is the obvious move and it is wrong. Write the 1, then you
// need to write 2 into a[1] — but a[1] still holds the 3 you have not merged
// yet. You would clobber your own input.
//
// So people reach for a temp array: merge forward into scratch, copy back. That
// works and costs O(m+n) extra memory for a problem that handed you the free
// space already.
//
// Fill from the BACK instead. Start all three pointers at the end and take the
// LARGER of the two tails each time:
//
//   i = m-1    last real value in a
//   j = n-1    last value in b
//   k = m+n-1  last slot in a — the free space
//
//   brute force   merge forward into a temp, copy back   O(m+n) EXTRA SPACE
//   this          fill backwards in place                O(1) extra space
//
// WHY BACKWARDS IS SAFE — the invariant that makes it work
//
// k is the write position and i is the read position in the same array, so the
// only danger is k catching up to i and overwriting an unread value. It cannot:
//
//   k - i = j + 1
//
// True on entry (k-i = n and j+1 = n) and preserved by every step: each write
// decrements k and exactly one of i or j, so either both sides drop by 1 or
// neither does.
//
// The loop runs only while j >= 0, so the gap is always at least 1 — the write
// slot is always STRICTLY right of the read slot. It closes to exactly 1 as b is
// exhausted, using every free slot and not one more. -- trace prints the column.
//
// Compare with InPlaceWritePointer (node 01), which had the mirror invariant —
// there write <= read because it moved forward. Same proof obligation, opposite
// direction. The rule underneath both: you may overwrite a slot you have
// already read, and the pointer order is what guarantees you have.
//
// WHY THE FREE SPACE'S LOCATION DECIDES THE DIRECTION
//
// This is the transferable idea. The free space is at the END, so writing
// backwards consumes it first and never touches live data. If the empty slots
// were at the FRONT, forward would be the safe direction and backward would
// clobber.
//
//   [1, 3, 5, _, _, _]   free at the back   ->  fill BACKWARD
//   [_, _, _, 1, 3, 5]   free at the front  ->  fill FORWARD
//
// So "merge from the back" is not a fact to memorise. Ask where the hole is,
// and write into the hole.
//
// THE TAIL CASE THAT CATCHES PEOPLE
//
// When the main loop ends, one array may still have values left:
//
//   b exhausted first   ->  a's remaining values are ALREADY in place. Nothing
//                           to do. A loop here is harmless but pointless.
//   a exhausted first   ->  b's remaining values MUST still be copied, because
//                           they were never in a to begin with.
//
// So exactly one drain loop is required, and it is b's. Writing both is
// harmless; writing only a's is a silent bug on input like a=[4,5,_,_],
// b=[1,2].
//
// WHEN NOT TO USE IT
//
// Only when the destination actually has the trailing space. If both arrays are
// exactly full you have nowhere to write and you need a third array — then it
// is just the merge step of MergeSort (node 02). And if the inputs are not
// sorted this produces nonsense, like every merge.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run MergeFromBack.cs                everything
//   dotnet run MergeFromBack.cs -- trace       one small input, step by step
//   dotnet run MergeFromBack.cs -- test        the edge cases
//   dotnet run MergeFromBack.cs -- compare     brute force vs this
//   dotnet run MergeFromBack.cs -- bench       steps and milliseconds
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

    int[] aInit = [1, 3, 5, 0, 0, 0];
    int[] b = [2, 4, 6];
    int m = 3, n = 3;

    Console.WriteLine();
    Console.WriteLine($"  a = [{string.Join(", ", aInit.Select((x, idx) => idx < m ? x.ToString() : "_"))}]   m = {m} real values, {n} free slots at the END");
    Console.WriteLine($"  b = [{string.Join(", ", b)}]");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Why forward fails, shown rather than asserted.
    // -------------------------------------------------------------------------
    Console.WriteLine("  WHY FORWARD FAILS — shown, not asserted");
    Console.WriteLine();

    int[] fwd = (int[])aInit.Clone();
    Console.WriteLine($"    start          a = [{string.Join(",", fwd)}]   (0 marks a free slot)");
    Console.WriteLine($"    write a[0]=1   a = [{string.Join(",", fwd)}]   fine, 1 was already there");
    fwd[1] = 2;
    Console.WriteLine($"    write a[1]=2   a = [{string.Join(",", fwd)}]   DESTROYED the 3 — it was not merged yet");
    Console.WriteLine();
    Console.WriteLine("    Forward writing collides with its own unread input. That is the whole");
    Console.WriteLine("    reason the temp array exists in the brute force — it is insurance");
    Console.WriteLine("    against this collision.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force: forward into scratch, then copy back.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — merge forward into a temp, then copy back");
    Console.WriteLine();

    int[] temp = new int[m + n];
    int ti = 0, tj = 0;
    for (int k = 0; k < m + n; k++)
    {
        if (ti >= m) temp[k] = b[tj++];
        else if (tj >= n) temp[k] = aInit[ti++];
        else if (aInit[ti] <= b[tj]) temp[k] = aInit[ti++];
        else temp[k] = b[tj++];
    }

    Console.WriteLine($"    merged into temp   [{string.Join(", ", temp)}]");
    Console.WriteLine($"    copy {m + n} back into a");
    Console.WriteLine();
    Console.WriteLine($"    Correct. Allocated {m + n} extra ints — for a problem that already");
    Console.WriteLine($"    handed you {n} free slots to work in.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Backwards, narrated, with the collision-safety gap shown every step.
    // -------------------------------------------------------------------------
    Console.WriteLine("  FROM THE BACK — in place, three pointers");
    Console.WriteLine();
    Console.WriteLine("    step    i    j    k   compare             write        k-i   a");
    Console.WriteLine("    ----   --   --   --   -----------------   ----------   ---   -------------");

    int[] a = (int[])aInit.Clone();
    int i = m - 1, j = n - 1, kk = m + n - 1, step = 1;

    while (j >= 0)
    {
        // Capture the pointers BEFORE acting, so the columns and the compare
        // text describe the same moment.
        int pi = i, pj = j, pk = kk;
        string cmp, wrote;

        if (i >= 0 && a[i] > b[j])
        {
            cmp = $"a[{i}]={a[i]} > b[{j}]={b[j]}";
            wrote = $"a[{kk}] = {a[i]}";
            a[kk] = a[i];
            i--;
        }
        else
        {
            cmp = i < 0 ? "a exhausted" : $"a[{i}]={a[i]} <= b[{j}]={b[j]}";
            wrote = $"a[{kk}] = {b[j]}";
            a[kk] = b[j];
            j--;
        }

        kk--;
        Console.WriteLine($"    {step,4}   {pi,2}   {pj,2}   {pk,2}   {cmp,-17}   {wrote,-10}   {pk - pi,3}   [{string.Join(",", a)}]");
        step++;
    }

    Console.WriteLine();
    Console.WriteLine($"    result: [{string.Join(", ", a)}]  in {step - 1} steps, zero extra memory.");
    Console.WriteLine($"    b is exhausted (j = {j}), so a's remaining values are already in place.");
    Console.WriteLine();

    Console.WriteLine("""
      WHY THE WRITE POINTER NEVER CLOBBERS THE READ POINTER

      k is the slot being written and i is the slot being read, both in the SAME
      array, so k overtaking i is the only thing that could go wrong. Read the
      k-i column: it is 3, 2, 2, 1, 1 — and the j column is 2, 1, 1, 0, 0.

      That is the invariant, and it is exact:

          k - i = j + 1

      Proof in three lines:

        START    k = m+n-1 and i = m-1, so k-i = n. And j = n-1, so j+1 = n.
                 They agree.

        STEP     every write decrements k and exactly one of i or j.
                   decrement i  ->  k-i unchanged, j unchanged.    still equal.
                   decrement j  ->  k-i drops by 1, j+1 drops by 1. still equal.

        HOLDS    so it is true on entry and preserved by every step.

      Now finish it. The loop runs only while j >= 0, so k - i >= 1 throughout:
      the write slot is always STRICTLY to the right of the read slot. a[k] is
      never the cell a[i] you are about to read.

      The collision is not avoided by luck or by a bounds check. It is
      arithmetically impossible, and the gap closing to exactly 1 as j hits 0 is
      the algorithm using every last free slot and not one more.

      Compare InPlaceWritePointer in node 01, where the invariant was
      write <= read because it moved forward. Same obligation, mirrored. The
      rule under both: you may only overwrite a slot you have already read.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Which drain loop is required, shown rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  WHICH LEFTOVERS NEED COPYING — shown");
    Console.WriteLine();

    Console.WriteLine("    case 1: b runs out first");
    int[] c1a = [1, 2, 9, 0, 0];
    int[] c1b = [3, 4];
    int[] got1 = (int[])c1a.Clone();
    Merge.FromBack(got1, 3, c1b, 2);
    Console.WriteLine($"      a = [1,2,9,_,_]  b = [3,4]   ->   [{string.Join(",", got1)}]");
    Console.WriteLine("      the 1 and 2 never moved. They were ALREADY in the right slots, so");
    Console.WriteLine("      no drain loop for a is needed — writing one is harmless but useless.");
    Console.WriteLine();

    Console.WriteLine("    case 2: a runs out first");
    int[] c2a = [4, 5, 0, 0];
    int[] c2b = [1, 2];
    int[] got2 = (int[])c2a.Clone();
    Merge.FromBack(got2, 2, c2b, 2);
    Console.WriteLine($"      a = [4,5,_,_]    b = [1,2]   ->   [{string.Join(",", got2)}]");
    Console.WriteLine("      here b's 1 and 2 had to be copied in after a was exhausted. Skip");
    Console.WriteLine("      that drain and you get [_,_,4,5] with garbage at the front.");
    Console.WriteLine();
    Console.WriteLine("  That asymmetry is why the loop condition is `while (j >= 0)` and not");
    Console.WriteLine("  `while (i >= 0 || j >= 0)` — b MUST be drained, a never needs to be.");
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
    Console.WriteLine("    case                      a (real part)         b                 result");
    Console.WriteLine("    -----------------------   -------------------   ---------------   --------------------------");

    int passed = 0, total = 0;

    void Check(string name, int[] aReal, int[] b)
    {
        total++;

        int m = aReal.Length, n = b.Length;

        // Build a with the trailing free slots, as the problem hands it to you.
        int[] a1 = new int[m + n];
        Array.Copy(aReal, a1, m);
        int[] a2 = (int[])a1.Clone();

        int[] expect = aReal.Concat(b).ToArray();
        Array.Sort(expect);                              // the oracle

        Merge.ForwardWithTemp(a1, m, b, n);
        Merge.FromBack(a2, m, b, n);

        bool ok = a1.SequenceEqual(expect) && a2.SequenceEqual(expect);
        if (ok) passed++;

        string sa = aReal.Length == 0 ? "[]" : $"[{string.Join(",", aReal)}]";
        if (sa.Length > 19) sa = sa[..16] + "...";
        string sb = b.Length == 0 ? "[]" : $"[{string.Join(",", b)}]";
        if (sb.Length > 15) sb = sb[..12] + "...";
        string got = a2.Length == 0 ? "[]" : $"[{string.Join(",", a2)}]";
        if (got.Length > 26) got = got[..23] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {sa,-19}   {sb,-15}   {got,-26}");
    }

    Check("both empty", [], []);
    Check("b empty", [1, 2, 3], []);
    Check("a empty", [], [1, 2, 3]);
    Check("interleaved", [1, 3, 5], [2, 4, 6]);
    Check("a all smaller", [1, 2, 3], [7, 8, 9]);
    Check("b all smaller", [7, 8, 9], [1, 2, 3]);
    Check("single each", [2], [1]);
    Check("duplicates across", [1, 2, 3], [1, 2, 3]);
    Check("all identical", [5, 5], [5, 5]);
    Check("uneven, a longer", [1, 2, 3, 4, 5], [6]);
    Check("uneven, b longer", [6], [1, 2, 3, 4, 5]);
    Check("negatives", [-5, -1], [-3, 0]);
    Check("touching at end", [1, 2, 9], [3, 4]);
    Check("a exhausts first", [4, 5], [1, 2]);
    Check("int extremes", [int.MinValue, 0], [int.MaxValue]);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Three cases earn their place.

          "b all smaller" and "a exhausts first" are the ones that catch a
          missing drain loop for b. On a=[4,5,_,_] with b=[1,2], the main loop
          exhausts a immediately and everything left in b still has to be
          written. An implementation that only drains a returns garbage at the
          front and throws no exception.

          "b empty" is the mirror: nothing to merge, and a must be returned
          untouched. If your loop is driven by k rather than by j it will
          happily rewrite a's values over themselves — correct by accident, but
          it hides the fact that the j-driven loop is the right one.

          "int extremes" checks that nothing in here adds two values together.
          It does not — merging only compares — which is why this file needs no
          long arithmetic, unlike ConvergingPointers.
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
    Console.WriteLine("  Interleaved input, m = n. Steps are ELEMENT WRITES.");
    Console.WriteLine();
    Console.WriteLine("      m+n   forward+temp writes   from-back writes   temp EXTRA SPACE");
    Console.WriteLine("    -----   -------------------   ----------------   ----------------");

    foreach (int total in (int[])[100, 1_000, 10_000, 1_000_000])
    {
        int m = total / 2, n = total - m;

        int[] aReal = new int[m];
        for (int i = 0; i < m; i++) aReal[i] = i * 2;        // evens
        int[] b = new int[n];
        for (int i = 0; i < n; i++) b[i] = i * 2 + 1;        // odds

        int[] a1 = new int[m + n];
        Array.Copy(aReal, a1, m);
        Merge.Steps = 0;
        Merge.ForwardWithTemp(a1, m, b, n);
        long fwd = Merge.Steps;

        int[] a2 = new int[m + n];
        Array.Copy(aReal, a2, m);
        Merge.Steps = 0;
        Merge.FromBack(a2, m, b, n);
        long back = Merge.Steps;

        Console.WriteLine($"    {total,5}   {fwd,19:N0}   {back,16:N0}   {$"{m + n} ints",16}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The forward version writes every element TWICE — once into the temp, once
      back into a — so its write count is 2(m+n) against the in-place version's
      (m+n). A flat 2x, not a complexity win: both are O(m+n).

      The column that matters is the last one. At m+n = 1,000,000 the forward
      version allocates 4 MB for a problem whose own input already contained
      the free space needed to solve it. The point of this technique is noticing
      that the free space is already there and that its LOCATION tells you which
      direction to write.

      Same shape as InPlaceWritePointer in node 01: identical time complexity,
      and the entire argument is O(n) space versus O(1).
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
    Console.WriteLine("  Interleaved input, m = n. Steps are element writes.");
    Console.WriteLine();
    Console.WriteLine("           forward + temp                 from the back");
    Console.WriteLine("      m+n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int total in (int[])[10_000, 100_000, 1_000_000, 10_000_000])
    {
        int m = total / 2, n = total - m;

        int[] aReal = new int[m];
        for (int i = 0; i < m; i++) aReal[i] = i * 2;
        int[] b = new int[n];
        for (int i = 0; i < n; i++) b[i] = i * 2 + 1;

        int[] seed = new int[m + n];
        Array.Copy(aReal, seed, m);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Merge.ForwardWithTemp((int[])seed.Clone(), m, b, n);
        Merge.FromBack((int[])seed.Clone(), m, b, n);

        int[] a1 = (int[])seed.Clone();
        Merge.Steps = 0;
        var sw = Stopwatch.StartNew();
        Merge.ForwardWithTemp(a1, m, b, n);
        double fwdMs = sw.Elapsed.TotalMilliseconds;
        long fwdSteps = Merge.Steps;

        int[] a2 = (int[])seed.Clone();
        Merge.Steps = 0;
        sw.Restart();
        Merge.FromBack(a2, m, b, n);
        double backMs = sw.Elapsed.TotalMilliseconds;
        long backSteps = Merge.Steps;

        Console.WriteLine($"    {total,5}   {fwdSteps,12:N0}   {fwdMs,6:N2}     {backSteps,12:N0}   {backMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Both curves are linear. The gap is the second write pass plus the cost of
      allocating and later collecting the temp array — at m+n = 10,000,000 that
      is 40 MB the in-place version never asks for, and 40 MB the GC never has
      to walk.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE TWO IMPLEMENTATIONS
//
// Both merge b (n values) into a, which holds m real values at the front and at
// least n free slots after them. Both leave a holding m+n sorted values.
//
// Steps counts ELEMENT WRITES so the two are measured in the same unit.
// =============================================================================

static class Merge
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // FORWARD INTO A TEMP — O(m+n) time, O(m+n) SPACE.
    //
    // Merge left to right into scratch, then copy the scratch back. The temp
    // array exists purely to avoid the forward collision: writing a[1] while
    // a[1] still holds unmerged input. See -- trace.
    //
    // Every element is written twice, which is the visible cost. The invisible
    // cost is the allocation.
    // -------------------------------------------------------------------------
    public static void ForwardWithTemp(int[] a, int m, int[] b, int n)
    {
        if (m + n == 0) return;

        int[] temp = new int[m + n];
        int i = 0, j = 0;

        for (int k = 0; k < m + n; k++)
        {
            Steps++;                                     // one write, into temp

            if (i >= m) temp[k] = b[j++];                // a exhausted
            else if (j >= n) temp[k] = a[i++];           // b exhausted
            else if (a[i] <= b[j]) temp[k] = a[i++];     // <= keeps it stable
            else temp[k] = b[j++];
        }

        for (int k = 0; k < m + n; k++)
        {
            Steps++;                                     // one write, copying back
            a[k] = temp[k];
        }
    }

    // -------------------------------------------------------------------------
    // FROM THE BACK — O(m+n) time, O(1) space.
    //
    // Three pointers, all starting at the end. Each step writes the LARGER of
    // the two tails into the rightmost unfilled slot.
    //
    //   i  last real value in a
    //   j  last value in b
    //   k  last free slot in a
    //
    // The loop is driven by j, not by k. When j hits -1, every remaining value
    // in a is already sitting in its final slot, so there is nothing to drain.
    // The reverse is not true, which is why j is the one that drives it.
    //
    // Safety: k - i = j + 1, and j >= 0 in the loop, so k is always strictly
    // right of i and can never clobber it. See -- trace for the proof.
    // -------------------------------------------------------------------------
    public static void FromBack(int[] a, int m, int[] b, int n)
    {
        int i = m - 1;                                   // last real value in a
        int j = n - 1;                                   // last value in b
        int k = m + n - 1;                               // last free slot in a

        while (j >= 0)
        {
            Steps++;                                     // one write

            // Take from a only while a still has values AND its tail is bigger.
            // The i >= 0 check has to come first or a[-1] throws.
            if (i >= 0 && a[i] > b[j]) a[k--] = a[i--];
            else a[k--] = b[j--];
        }

        // No drain loop for a. If j ran out first, a[0..i] are untouched and
        // already in their final positions.
    }
}
