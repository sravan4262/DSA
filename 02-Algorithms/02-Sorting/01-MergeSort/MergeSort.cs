#!/usr/bin/env dotnet
// =============================================================================
// MERGESORT — split until trivial, then merge sorted halves back together.
//
// Node 02 · Sorting.  Needs: array, recursion.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The only sorting algorithm in this roadmap, picked on purpose. Three steps:
//
//   1. SPLIT   cut the range in half
//   2. SORT    sort each half (by doing this same thing to it)
//   3. MERGE   walk both sorted halves at once, taking the smaller front element
//
// A range of one element is already sorted, so the recursion stops there. All
// of the actual work is in step 3 — splitting is just arithmetic.
//
//   brute force   insertion sort   O(n^2) time,      O(1) space
//   this          merge sort       O(n log n) ALWAYS, O(n) space
//
// WHY O(n log n) — derive it, do not memorise it
//
// Two independent facts multiplied together:
//
//   halving n down to 1 takes log2(n) steps        -> log n LEVELS
//   each level merges every element exactly once   -> n work PER LEVEL
//
//   level 0:        1 range  of n      -> n elements merged
//   level 1:        2 ranges of n/2    -> n elements merged
//   level 2:        4 ranges of n/4    -> n elements merged
//   ...
//   level log n:    n ranges of 1      -> n elements merged
//                                         ---
//                                         n x log n
//
// The subarrays get smaller but there are proportionally more of them, so every
// level costs the same n. Count the levels, multiply by n. That is the whole
// derivation, and it is the template for every divide-and-conquer analysis.
//
// WHY THE MERGE WORKS IN ONE PASS
//
// Both halves are already sorted, so the smallest unplaced element is at the
// front of one half or the other — never buried in the middle. One comparison
// identifies it. So merging two sorted halves of total length n costs n
// comparisons, not n^2. Sortedness of the inputs is what buys that, which is
// why the recursion has to finish before the merge starts.
//
// STABLE, AND IT HANGS ON ONE CHARACTER
//
// Stable = elements that compare equal keep their original relative order. In
// the merge, when the two fronts tie, you must take from the LEFT half, because
// the left half held the earlier elements:
//
//   if (temp[l] <= temp[r])   take left     STABLE
//   if (temp[l] <  temp[r])   take right    NOT stable
//
// One character. It matters whenever you sort records by one field and expect
// a previous sort on another field to survive — sort by date, then by name, and
// stability is what keeps each name's dates in order. -- trace shows both.
//
// WHAT IT COSTS: O(n) SPACE, AND IT IS NOT OPTIONAL
//
// The merge cannot be done in place without destroying elements it has not read
// yet, so it needs a scratch array. Allocate it ONCE in the public entry point
// and reuse it at every level — allocating inside the recursion means O(n log n)
// allocations and is a classic performance bug. Plus O(log n) stack frames for
// the recursion itself, which counts toward space.
//
// WHY THIS ONE AND NOT QUICKSORT
//
//   O(n log n) WORST case      quicksort degrades to O(n^2) on bad pivots
//   stable                     quicksort is not
//   works on linked lists      quicksort needs random access
//   merge step is reusable     MergeFromBack (04), KWayMerge (12)
//
// Quicksort is usually faster in practice — in place, better cache behaviour,
// smaller constants. In production you call Array.Sort, which is introsort:
// quicksort, falling back to heapsort on bad pivots and insertion sort on small
// spans. MergeSort is the one to KNOW; the library is the one to CALL.
//
// WHEN NOT TO USE IT
//
// When O(n) extra memory is unavailable, or the data is already nearly sorted —
// insertion sort is O(n) on sorted input and mergesort is O(n log n) no matter
// what. -- compare measures exactly that, and insertion sort wins the sorted
// column outright.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run MergeSort.cs                everything
//   dotnet run MergeSort.cs -- trace       one small input, step by step
//   dotnet run MergeSort.cs -- test        the edge cases
//   dotnet run MergeSort.cs -- compare     insertion sort vs this, in steps
//   dotnet run MergeSort.cs -- bench       steps and milliseconds
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

    int[] input = [38, 27, 43, 3, 9, 82, 10];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", input)}]");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The recursion, narrated. Indentation IS the depth, so the shape of the
    // call tree is visible without drawing a separate diagram.
    // -------------------------------------------------------------------------
    Console.WriteLine("  THE RECURSION — indentation is the call depth");
    Console.WriteLine();

    int[] a = (int[])input.Clone();
    int[] temp = new int[a.Length];
    int maxDepth = 0;

    void Recurse(int lo, int hi, int depth)
    {
        if (depth > maxDepth) maxDepth = depth;
        string pad = new string(' ', 4 + depth * 2);

        if (lo >= hi)
        {
            Console.WriteLine($"{pad}leaf  {$"[{a[lo]}]",-22}  already sorted, return");
            return;
        }

        int mid = lo + (hi - lo) / 2;
        Console.WriteLine($"{pad}split [{string.Join(",", a[lo..(hi + 1)])}]  ->  [{string.Join(",", a[lo..(mid + 1)])}] and [{string.Join(",", a[(mid + 1)..(hi + 1)])}]");

        Recurse(lo, mid, depth + 1);
        Recurse(mid + 1, hi, depth + 1);

        // Capture both halves before merging so the line can show the inputs.
        string left = string.Join(",", a[lo..(mid + 1)]);
        string right = string.Join(",", a[(mid + 1)..(hi + 1)]);

        for (int i = lo; i <= hi; i++) temp[i] = a[i];
        int l = lo, r = mid + 1;
        for (int k = lo; k <= hi; k++)
        {
            if (l > mid) a[k] = temp[r++];
            else if (r > hi) a[k] = temp[l++];
            else if (temp[l] <= temp[r]) a[k] = temp[l++];
            else a[k] = temp[r++];
        }

        Console.WriteLine($"{pad}MERGE [{left}] + [{right}]  ->  [{string.Join(",", a[lo..(hi + 1)])}]");
    }

    Recurse(0, a.Length - 1, 0);

    Console.WriteLine();
    Console.WriteLine($"    result: [{string.Join(", ", a)}]");
    Console.WriteLine($"    deepest recursion: {maxDepth} levels, and log2({a.Length}) = {Math.Ceiling(Math.Log2(a.Length))}");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // One merge, pointer by pointer. This is where all the work happens.
    // -------------------------------------------------------------------------
    Console.WriteLine("  ONE MERGE, POINTER BY POINTER — all the work is here");
    Console.WriteLine();

    int[] L = [3, 27, 38, 43];
    int[] R = [9, 10, 82];
    Console.WriteLine($"    left  = [{string.Join(", ", L)}]   (already sorted)");
    Console.WriteLine($"    right = [{string.Join(", ", R)}]      (already sorted)");
    Console.WriteLine();
    Console.WriteLine("    step   compare        take      output so far");
    Console.WriteLine("    ----   ------------   -------   -------------------------");

    var outp = new List<int>();
    int li = 0, ri = 0, step = 1;
    while (li < L.Length || ri < R.Length)
    {
        string cmp, took;
        if (li >= L.Length) { cmp = "left empty"; took = $"R {R[ri]}"; outp.Add(R[ri++]); }
        else if (ri >= R.Length) { cmp = "right empty"; took = $"L {L[li]}"; outp.Add(L[li++]); }
        else if (L[li] <= R[ri]) { cmp = $"{L[li]} <= {R[ri]}"; took = $"L {L[li]}"; outp.Add(L[li++]); }
        else { cmp = $"{L[li]} >  {R[ri]}"; took = $"R {R[ri]}"; outp.Add(R[ri++]); }

        Console.WriteLine($"    {step,4}   {cmp,-12}   {took,-7}   [{string.Join(", ", outp)}]");
        step++;
    }

    Console.WriteLine();
    Console.WriteLine($"    {L.Length + R.Length} elements merged in {step - 1} steps — one per element.");
    Console.WriteLine();

    Console.WriteLine("""
      WHY ONE PASS IS ENOUGH

      Both halves are already sorted, so the smallest element not yet placed is
      sitting at the FRONT of one half or the other. It can never be buried in
      the middle. One comparison finds it, so n elements take n comparisons
      rather than n^2.

      That is why the recursion has to finish before the merge begins: the merge
      is cheap only because its inputs are sorted, and the recursion is what
      makes them sorted.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Why n log n, as a table rather than a claim.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  WHY n log n — the two facts, multiplied");
    Console.WriteLine();
    Console.WriteLine("    level   ranges   size each   elements merged");
    Console.WriteLine("    -----   ------   ---------   ---------------");

    int n = 64;
    for (int lvl = 0, ranges = 1, size = n; size >= 1; lvl++, ranges *= 2, size /= 2)
        Console.WriteLine($"    {lvl,5}   {ranges,6}   {size,9}   {ranges * size,15}");

    Console.WriteLine();
    Console.WriteLine($"    The bottom row is the leaves — single elements, nothing to merge.");
    Console.WriteLine($"    So for n = {n} there are log2({n}) = {(int)Math.Log2(n)} MERGE levels, each moving all");
    Console.WriteLine($"    {n} elements:   {n} x {(int)Math.Log2(n)} = {n * (int)Math.Log2(n)} element moves.");
    Console.WriteLine();
    Console.WriteLine("    The ranges shrink but there are proportionally more of them, so");
    Console.WriteLine("    each level costs the same n. Count levels, multiply by n. Done.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Stability, shown. One character of difference.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  STABILITY, AND THE ONE CHARACTER IT HANGS ON");
    Console.WriteLine();

    (int key, char tag)[] items = [(2, 'a'), (1, 'x'), (2, 'b'), (1, 'y')];
    Console.WriteLine($"    input (sort by the NUMBER, watch the letters):");
    Console.WriteLine($"      {string.Join("  ", items.Select(t => $"{t.key}{t.tag}"))}");
    Console.WriteLine();
    Console.WriteLine("      a comes before b in the input, and x comes before y.");
    Console.WriteLine();

    var stable = Stability.Sort((( int, char)[])items.Clone(), useLessOrEqual: true);
    var unstable = Stability.Sort((( int, char)[])items.Clone(), useLessOrEqual: false);

    Console.WriteLine($"    temp[l] <= temp[r]  (STABLE)      {string.Join("  ", stable.Select(t => $"{t.Item1}{t.Item2}"))}   <- x before y, a before b");
    Console.WriteLine($"    temp[l] <  temp[r]  (not stable)  {string.Join("  ", unstable.Select(t => $"{t.Item1}{t.Item2}"))}   <- both pairs FLIPPED");
    Console.WriteLine();
    Console.WriteLine("""
      Both outputs are correctly sorted by the number. Only one preserved the
      input order of the ties, and the difference is a single '=' character.

      When the two fronts are equal you must take from the LEFT half, because
      the left half is the one that held the earlier elements. Taking from the
      right reverses every tie.

      Why care: sort by date, then sort by name. If the second sort is stable,
      each name's dates stay in order and you have grouped-and-sorted data from
      two simple passes. If it is not, the second sort scrambles the first.
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
    Console.WriteLine("    case                      input                        result");
    Console.WriteLine("    -----------------------   --------------------------   --------------------------");

    int passed = 0, total = 0;

    void Check(string name, int[] input)
    {
        total++;

        int[] expect = (int[])input.Clone();
        Array.Sort(expect);                      // the oracle

        int[] a1 = (int[])input.Clone();
        Sorts.InsertionSort(a1);

        int[] a2 = (int[])input.Clone();
        Sorts.MergeSort(a2);

        bool ok = a1.SequenceEqual(expect) && a2.SequenceEqual(expect);
        if (ok) passed++;

        string shown = input.Length == 0 ? "[]" : $"[{string.Join(",", input)}]";
        if (shown.Length > 26) shown = shown[..23] + "...";
        string got = a2.Length == 0 ? "[]" : $"[{string.Join(",", a2)}]";
        if (got.Length > 26) got = got[..23] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-26}   {got,-26}");
    }

    Check("empty", []);
    Check("single", [5]);
    Check("two, sorted", [1, 2]);
    Check("two, reversed", [2, 1]);
    Check("three, reversed", [3, 2, 1]);
    Check("already sorted", [1, 2, 3, 4, 5]);
    Check("reverse sorted", [5, 4, 3, 2, 1]);
    Check("all identical", [7, 7, 7, 7]);
    Check("duplicates", [3, 1, 3, 1, 2]);
    Check("negatives", [-3, 5, -1, 0, -9]);
    Check("the classic", [38, 27, 43, 3, 9, 82, 10]);
    Check("odd length", [5, 1, 4, 2, 8]);
    Check("even length", [5, 1, 4, 2]);
    Check("int extremes", [int.MaxValue, int.MinValue, 0]);
    Check("one out of place", [1, 2, 3, 0, 4]);

    // A big random array, to catch anything that only breaks at depth.
    var rng = new Random(42);
    int[] big = new int[1000];
    for (int i = 0; i < big.Length; i++) big[i] = rng.Next(-1000, 1000);
    Check("1,000 random", big);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    // Stability gets its own check — SequenceEqual on ints cannot detect it,
    // because two equal ints are indistinguishable once sorted.
    (int, char)[] tagged = [(2, 'a'), (1, 'x'), (2, 'b'), (1, 'y'), (1, 'z')];
    var st = Stability.Sort(((int, char)[])tagged.Clone(), useLessOrEqual: true);
    bool stable = st.Select(t => t.Item2).SequenceEqual(new[] { 'x', 'y', 'z', 'a', 'b' });
    Console.WriteLine($"    {(stable ? "PASS" : "FAIL")}  stability           ties keep input order -> {string.Join("", st.Select(t => t.Item2))}");
    Console.WriteLine();

    if (passed == total && stable)
    {
        Console.WriteLine("""
          Three notes on this table.

          Array.Sort is used as the ORACLE, not as the implementation — the
          algorithms under test are hand-written and Array.Sort only says what
          the answer should be.

          "all identical" and "duplicates" check correctness but CANNOT check
          stability: once sorted, two equal ints are indistinguishable. Stability
          needs tagged elements, which is why it is a separate check below the
          table.

          "already sorted" matters for a reason the table does not show —
          insertion sort finishes it in O(n) while mergesort still does the full
          O(n log n). See -- compare.
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
    Console.WriteLine("  Steps are ELEMENT COMPARISONS. Three input shapes, because this is");
    Console.WriteLine("  the one place in the roadmap where the brute force sometimes WINS.");
    Console.WriteLine();

    foreach (string shape in (string[])["random", "already sorted", "reverse sorted"])
    {
        Console.WriteLine($"  {shape}:");
        Console.WriteLine();
        Console.WriteLine("        n   insertion sort   merge sort        ratio");
        Console.WriteLine("    -----   --------------   ----------   ----------");

        var rng = new Random(42);
        foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
        {
            int[] data = new int[n];
            for (int i = 0; i < n; i++)
                data[i] = shape switch
                {
                    "random" => rng.Next(1_000_000),
                    "already sorted" => i,
                    _ => n - i
                };

            int[] a1 = (int[])data.Clone();
            Sorts.Steps = 0;
            Sorts.InsertionSort(a1);
            long ins = Sorts.Steps;

            int[] a2 = (int[])data.Clone();
            Sorts.Steps = 0;
            Sorts.MergeSort(a2);
            long mrg = Sorts.Steps;

            string ratio = ins >= mrg ? $"{(double)ins / mrg,9:N0}x" : $"1/{(double)mrg / ins,-7:N1}";
            Console.WriteLine($"    {n,5}   {ins,14:N0}   {mrg,10:N0}   {ratio,10}");
        }
        Console.WriteLine();
    }

    Console.WriteLine("""
      Read the middle block. On already-sorted input insertion sort does n-1
      comparisons and stops — it is O(n), and it BEATS mergesort by a growing
      margin. Mergesort cannot exploit existing order to change its SHAPE: it
      splits and merges identically no matter what, so it is O(n log n) on its
      best input and its worst input alike.

      One honest wrinkle in the numbers. Mergesort's comparison count is not
      literally constant across the three blocks — at n = 50,000 it is 718,047
      random, 401,952 sorted, 382,512 reversed, roughly a 2x spread. That is
      because when one half is entirely below the other, the merge exhausts that
      half early and the leftovers are copied without any comparison. The
      element MOVES are exactly n log n in every case; only the comparisons
      wobble, and only by a constant factor. The asymptotic claim stands.

      That is not a flaw, it is the trade. Mergesort gives up the easy wins in
      exchange for a GUARANTEE: no input makes it slow. Insertion sort's O(n)
      best case comes with an O(n^2) worst case, and you do not get to choose
      which one your data is.

      It is also why Array.Sort is introsort rather than any single algorithm:
      it uses insertion sort on small or nearly-sorted spans precisely to collect
      the wins in the middle block, then switches for everything else.
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
    Console.WriteLine("  Random input. Steps are element comparisons.");
    Console.WriteLine();
    Console.WriteLine("           insertion sort                 merge sort");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    var rng = new Random(42);
    foreach (int n in (int[])[1_000, 10_000, 50_000, 100_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(1_000_000);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Sorts.InsertionSort((int[])data.Clone());
        Sorts.MergeSort((int[])data.Clone());

        int[] a1 = (int[])data.Clone();
        Sorts.Steps = 0;
        var sw = Stopwatch.StartNew();
        Sorts.InsertionSort(a1);
        double insMs = sw.Elapsed.TotalMilliseconds;
        long insSteps = Sorts.Steps;

        int[] a2 = (int[])data.Clone();
        Sorts.Steps = 0;
        sw.Restart();
        Sorts.MergeSort(a2);
        double mrgMs = sw.Elapsed.TotalMilliseconds;
        long mrgSteps = Sorts.Steps;

        Console.WriteLine($"    {n,5}   {insSteps,12:N0}   {insMs,6:N2}     {mrgSteps,12:N0}   {mrgMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.

      The recursion depth is log2(n), so n = 100,000 nests about 17 frames deep.
      That is why mergesort never overflows the 1 MB thread stack, unlike a
      naive quicksort which can recurse n deep on sorted input.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE TWO IMPLEMENTATIONS
//
// Both sort an int[] ascending, in place from the caller's point of view. Both
// add to Sorts.Steps, counting ELEMENT COMPARISONS so the two are measured in
// the same unit.
// =============================================================================

static class Sorts
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // INSERTION SORT — O(n^2) worst and average, O(n) BEST. O(1) space.
    //
    // Walk left to right, and slide each element back into its place among the
    // already-sorted prefix. The prefix a[0..i-1] is always sorted, which is the
    // invariant that makes it correct.
    //
    // Its best case is real and worth knowing: on already-sorted input the inner
    // loop never runs, so it finishes in n-1 comparisons. That is the one thing
    // it does better than mergesort, and it is why introsort keeps it around for
    // small and nearly-sorted spans.
    // -------------------------------------------------------------------------
    public static void InsertionSort(int[] a)
    {
        for (int i = 1; i < a.Length; i++)
        {
            int x = a[i];
            int j = i - 1;

            // Slide everything greater than x one slot right.
            while (j >= 0)
            {
                Steps++;                     // one element comparison
                if (a[j] <= x) break;        // found x's place
                a[j + 1] = a[j];
                j--;
            }

            a[j + 1] = x;
        }
    }

    // -------------------------------------------------------------------------
    // MERGE SORT — O(n log n) worst, average AND best. O(n) space.
    //
    // The scratch array is allocated ONCE here and threaded through the
    // recursion. Allocating inside the recursive method would mean O(n log n)
    // allocations instead of one — correct, but a well-known performance bug.
    // -------------------------------------------------------------------------
    public static void MergeSort(int[] a)
    {
        if (a.Length < 2) return;            // 0 or 1 elements: already sorted

        int[] temp = new int[a.Length];      // ONE allocation, reused at every level
        SortRange(a, temp, 0, a.Length - 1);
    }

    // Sort a[lo..hi] inclusive.
    private static void SortRange(int[] a, int[] temp, int lo, int hi)
    {
        if (lo >= hi) return;                // a range of one is sorted

        int mid = lo + (hi - lo) / 2;        // not (lo+hi)/2 — that can overflow

        SortRange(a, temp, lo, mid);         // sort the left half
        SortRange(a, temp, mid + 1, hi);     // sort the right half
        Merge(a, temp, lo, mid, hi);         // then combine them
    }

    // Merge the two sorted halves a[lo..mid] and a[mid+1..hi].
    private static void Merge(int[] a, int[] temp, int lo, int mid, int hi)
    {
        // Copy the range out, then write the merged result back over a. Copying
        // first is what makes it safe to overwrite a[k] while still needing the
        // original values.
        for (int i = lo; i <= hi; i++) temp[i] = a[i];

        int l = lo, r = mid + 1;

        for (int k = lo; k <= hi; k++)
        {
            if (l > mid) a[k] = temp[r++];                 // left exhausted
            else if (r > hi) a[k] = temp[l++];             // right exhausted
            else
            {
                Steps++;                                   // one element comparison

                // <= and not < : on a tie take from the LEFT half, which held
                // the earlier elements. This single character is what makes the
                // whole sort STABLE.
                if (temp[l] <= temp[r]) a[k] = temp[l++];
                else a[k] = temp[r++];
            }
        }
    }
}


// =============================================================================
// THE STABILITY DEMONSTRATION
//
// Same merge sort, over (key, tag) pairs so ties are distinguishable, with the
// tie-breaking comparison switchable. Sorts by key only; the tag just rides
// along so you can see where it ended up.
// =============================================================================

static class Stability
{
    public static (int, char)[] Sort((int, char)[] a, bool useLessOrEqual)
    {
        if (a.Length < 2) return a;

        var temp = new (int, char)[a.Length];
        SortRange(a, temp, 0, a.Length - 1, useLessOrEqual);
        return a;
    }

    private static void SortRange((int, char)[] a, (int, char)[] temp, int lo, int hi, bool le)
    {
        if (lo >= hi) return;

        int mid = lo + (hi - lo) / 2;
        SortRange(a, temp, lo, mid, le);
        SortRange(a, temp, mid + 1, hi, le);

        for (int i = lo; i <= hi; i++) temp[i] = a[i];

        int l = lo, r = mid + 1;
        for (int k = lo; k <= hi; k++)
        {
            if (l > mid) a[k] = temp[r++];
            else if (r > hi) a[k] = temp[l++];
            else
            {
                // The only difference between a stable and an unstable mergesort.
                bool takeLeft = le
                    ? temp[l].Item1 <= temp[r].Item1     // stable
                    : temp[l].Item1 < temp[r].Item1;     // not stable

                if (takeLeft) a[k] = temp[l++];
                else a[k] = temp[r++];
            }
        }
    }
}
