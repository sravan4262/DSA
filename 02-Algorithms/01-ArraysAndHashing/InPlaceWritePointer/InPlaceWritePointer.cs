#!/usr/bin/env dotnet
// =============================================================================
// INPLACEWRITEPOINTER — compact an array using two indices and no extra memory.
//
// Node 01 · Arrays & Hashing.  Needs: array.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The job: remove every element matching some rule and close up the gaps, in
// place, returning how many elements survived.
//
// Two indices walk the same array at different speeds:
//
//   read    visits EVERY element, once, left to right
//   write   the next slot a keeper should go into — advances ONLY on a keep
//
//   for (read = 0; read < n; read++)
//       if (keep(a[read]))
//           a[write++] = a[read];
//   return write;                    <- write IS the new length
//
// Because write never gets ahead of read, you only ever overwrite a slot you
// have already read. That is the invariant that makes it safe, and it is the
// whole trick.
//
//   brute force   build a new array, copy back    O(n) time, O(n) SPACE
//   this          two indices, one pass           O(n) time, O(1) space
//
// Note the time complexity is the SAME. This is not a speed optimisation — both
// are one pass. The win is entirely in SPACE, and that makes it the odd one out
// in this node. "Do it in place, O(1) extra space" is a constraint interviewers
// add specifically to force this pattern.
//
// WHY write NEVER OVERTAKES read
//
// write starts equal to read and only advances when read does, so write <= read
// always. On the first element they are both 0, and a[0] = a[0] is harmless.
// After the first discard, write falls permanently behind — and the gap is
// exactly the number of elements discarded so far.
//
//   a      [ 0,  1,  0,  3,  12 ]      remove zeros
//            ^
//            read=0, write=0            keep -> a[0]=0, write=1
//
//   a      [ 1,  1,  0,  3,  12 ]
//                 ^
//            read=1, write=0 -> wrote 1 at slot 0, write=1
//
// THE TAIL IS GARBAGE, NOT CLEARED
//
// The array's Length does not change — you cannot shrink an array in C#. After
// the call, a[0..write-1] holds the answer and everything from write onward is
// STALE DATA left over from before the shift. It is not zeroed and not removed.
//
//   [0, 1, 0, 3, 12]  remove zeros  ->  [1, 3, 12, 3, 12]
//                                        ^^^^^^^^^  ^^^^^
//                                        the answer  garbage
//
// This is why the return value matters: it is the only thing that tells the
// caller where the real data stops. Ignoring it and printing the whole array is
// the classic bug. If the caller needs a clean array, they slice: a[..written].
//
// WHEN ORDER DOES NOT MATTER, THERE IS SOMETHING FASTER
//
// This pattern preserves the relative order of the keepers, and pays for that
// by writing every single keeper. If order is irrelevant, swap-with-last is
// cheaper — it writes only once per DISCARD, not once per keep:
//
//   if (!keep(a[i])) a[i--] = a[--n];    overwrite the hole with the last element
//
// Mostly-keepers favours swap-with-last; mostly-discards favours this. Both are
// O(n) and O(1), so it is a constant-factor choice, and both are in the file
// below so -- compare can show the difference.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run InPlaceWritePointer.cs                everything
//   dotnet run InPlaceWritePointer.cs -- trace       one small input, step by step
//   dotnet run InPlaceWritePointer.cs -- test        the edge cases
//   dotnet run InPlaceWritePointer.cs -- compare     brute force vs this
//   dotnet run InPlaceWritePointer.cs -- bench       steps and milliseconds
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

    int[] original = [0, 1, 0, 3, 0, 12];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", original)}]");
    Console.WriteLine("  job:   remove every 0, keep the order, return the new length");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. Build a second array, then copy it back.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — build a new list, then copy it back");
    Console.WriteLine();

    var kept = new List<int>();
    foreach (int x in original) if (x != 0) kept.Add(x);

    Console.WriteLine($"    pass 1: collect keepers into a NEW list   -> [{string.Join(", ", kept)}]");
    Console.WriteLine($"    pass 2: copy {kept.Count} of them back into the array");
    Console.WriteLine();
    Console.WriteLine($"    correct answer, but it allocated a second array of {kept.Count} ints.");
    Console.WriteLine("    O(n) extra space — which is exactly what the constraint forbids.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The write pointer, narrated. One row per read, both indices shown.
    // -------------------------------------------------------------------------
    Console.WriteLine("  WRITE POINTER — two indices, one array, no allocation");
    Console.WriteLine();
    Console.WriteLine("    read   a[read]   keep?   action                 write   array after");
    Console.WriteLine("    ----   -------   -----   --------------------   -----   ---------------------");

    int[] a = (int[])original.Clone();
    int write = 0;
    for (int read = 0; read < a.Length; read++)
    {
        bool keep = a[read] != 0;
        string action;

        if (keep)
        {
            action = $"a[{write}] = {a[read],-2}  (keep)";
            a[write] = a[read];
            write++;
        }
        else
        {
            action = "skip, write stays";
        }

        Console.WriteLine($"    {read,4}   {a[read],7}   {(keep ? "yes" : "no"),5}   {action,-20}   {write,5}   [{string.Join(", ", a.Select(x => x.ToString().PadLeft(2)))}]");
    }

    Console.WriteLine();
    Console.WriteLine($"    returned length = {write}");
    Console.WriteLine();

    // Draw the answer/garbage split, which is the thing people get wrong.
    Console.Write("    final array  [");
    for (int i = 0; i < a.Length; i++) Console.Write($"{a[i],3}{(i < a.Length - 1 ? "," : "")}");
    Console.WriteLine("]");
    Console.Write("                  ");
    for (int i = 0; i < a.Length; i++) Console.Write(i < write ? "^^^ " : "ggg ");
    Console.WriteLine();
    Console.WriteLine($"                  {new string(' ', 0)}^ = the answer (first {write})      g = stale garbage");
    Console.WriteLine();
    Console.WriteLine($"    a[..{write}] = [{string.Join(", ", a[..write])}]   <- what the caller actually wants");
    Console.WriteLine();

    Console.WriteLine("""
      THE WORK THAT DISAPPEARED

      Nothing, in time terms — both versions touch every element once, and both
      are O(n). Compare that with the rest of this node: HashMapCounting,
      PrefixSums, DifferenceArray and Kadane all cut the TIME complexity. This
      one does not. It cuts the SPACE, from O(n) to O(1), and that is the only
      thing it does.

      Which makes it worth being precise about why it works at all. The brute
      force needs a second array because it is afraid of clobbering data it has
      not read yet. The write pointer proves that fear is unfounded:

          write <= read, always

      write starts level with read and advances only when read advances, so it
      can never run ahead. Every slot you overwrite is a slot you already read.
      The second array was never necessary — it was insurance against a danger
      that does not exist.

      The last two elements of the final array are leftovers from before the
      shift. The array's Length cannot change in C#, so the return value is the
      ONLY thing marking where the answer stops. Printing the whole array and
      ignoring the return value is the standard bug.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The variant: when order does not matter, swap-with-last writes less.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE VARIANT — swap-with-last, when order does NOT matter");
    Console.WriteLine();

    int[] b = (int[])original.Clone();
    int n = b.Length, i2 = 0, swapWrites = 0;
    Console.WriteLine("    i   a[i]   action                          array after");
    Console.WriteLine("    -   ----   -----------------------------   ---------------------");
    while (i2 < n)
    {
        if (b[i2] == 0)
        {
            n--;
            string act = $"hole at {i2}, pull a[{n}] = {b[n],-2} in";
            b[i2] = b[n];
            swapWrites++;
            Console.WriteLine($"    {i2}   {0,4}   {act,-29}   [{string.Join(", ", b.Take(n).Select(x => x.ToString().PadLeft(2)))}]");
            // do NOT advance i2 — the element just pulled in is unexamined
        }
        else
        {
            Console.WriteLine($"    {i2}   {b[i2],4}   {"keep, no write at all",-29}   [{string.Join(", ", b.Take(n).Select(x => x.ToString().PadLeft(2)))}]");
            i2++;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"    returned length = {n}, and only {swapWrites} writes happened");
    Console.WriteLine($"    (the write-pointer version did {write} writes — one per KEEPER)");
    Console.WriteLine();
    Console.WriteLine($"    result [{string.Join(", ", b[..n])}]  — same elements, DIFFERENT order");
    Console.WriteLine();
    Console.WriteLine("  Two things to notice. It writes once per DISCARD rather than once per");
    Console.WriteLine("  keeper, so mostly-keepers data favours this one. And `i` must not");
    Console.WriteLine("  advance after a swap — the element you just pulled in from the end has");
    Console.WriteLine("  not been examined yet, and may itself need discarding.");
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
    Console.WriteLine("    case                      input                     remove   expected kept       len");
    Console.WriteLine("    -----------------------   -----------------------   ------   -----------------   ---");

    int passed = 0, total = 0;

    void Check(string name, int[] input, int remove, int[] expect)
    {
        total++;

        // Brute force and write pointer must both preserve order exactly.
        int[] a1 = (int[])input.Clone();
        int len1 = Compact.RemoveBrute(a1, remove);

        int[] a2 = (int[])input.Clone();
        int len2 = Compact.RemoveWritePointer(a2, remove);

        // Swap-with-last is allowed to reorder, so compare it as a multiset.
        int[] a3 = (int[])input.Clone();
        int len3 = Compact.RemoveSwapWithLast(a3, remove);

        bool ok = len1 == expect.Length && a1[..len1].SequenceEqual(expect)
               && len2 == expect.Length && a2[..len2].SequenceEqual(expect)
               && len3 == expect.Length && a3[..len3].OrderBy(x => x).SequenceEqual(expect.OrderBy(x => x));
        if (ok) passed++;

        string shown = input.Length == 0 ? "[]" : $"[{string.Join(",", input)}]";
        if (shown.Length > 23) shown = shown[..20] + "...";
        string exp = expect.Length == 0 ? "[]" : $"[{string.Join(",", expect)}]";
        if (exp.Length > 17) exp = exp[..14] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-23}   {remove,6}   {exp,-17}   {expect.Length,3}");
    }

    Check("empty", [], 0, []);
    Check("single, keep", [5], 0, [5]);
    Check("single, remove", [0], 0, []);
    Check("nothing to remove", [1, 2, 3], 0, [1, 2, 3]);
    Check("remove everything", [0, 0, 0], 0, []);
    Check("remove first", [0, 1, 2], 0, [1, 2]);
    Check("remove last", [1, 2, 0], 0, [1, 2]);
    Check("the classic", [0, 1, 0, 3, 0, 12], 0, [1, 3, 12]);
    Check("alternating", [0, 1, 0, 1, 0], 0, [1, 1]);
    Check("adjacent removals", [1, 0, 0, 0, 2], 0, [1, 2]);
    Check("negatives kept", [-1, 0, -2], 0, [-1, -2]);
    Check("remove a negative", [-1, 5, -1], -1, [5]);
    Check("duplicates kept", [3, 3, 0, 3], 0, [3, 3, 3]);
    Check("two elements", [0, 7], 0, [7]);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Note how the three implementations are checked differently. The brute
          force and the write pointer must produce the kept elements in their
          ORIGINAL ORDER, so they are compared element by element. Swap-with-last
          is allowed to reorder, so it is compared as a multiset — same elements,
          any order. Holding it to the stricter test would fail a correct
          implementation.

          "remove everything" and "adjacent removals" are the two that matter.
          The first must return 0 and leave write at 0. The second has three
          discards in a row, which is where an implementation that advances the
          wrong index on a discard falls over.
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
    Console.WriteLine("  Steps counted as ELEMENT WRITES, because that is what differs —");
    Console.WriteLine("  all three read every element exactly once.");
    Console.WriteLine();
    Console.WriteLine("  Varying how much of the array survives:");
    Console.WriteLine();
    Console.WriteLine("    n = 10,000       brute writes   writeptr writes   swap writes   brute EXTRA SPACE");
    Console.WriteLine("    --------------   ------------   ---------------   -----------   -----------------");

    var rng = new Random(42);
    foreach (int keepPercent in (int[])[10, 50, 90, 100])
    {
        int n = 10_000;
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(100) < keepPercent ? 1 : 0;

        int[] a1 = (int[])data.Clone();
        Compact.Steps = 0;
        Compact.RemoveBrute(a1, 0);
        long brute = Compact.Steps;

        int[] a2 = (int[])data.Clone();
        Compact.Steps = 0;
        int kept = Compact.RemoveWritePointer(a2, 0);
        long wp = Compact.Steps;

        int[] a3 = (int[])data.Clone();
        Compact.Steps = 0;
        Compact.RemoveSwapWithLast(a3, 0);
        long swap = Compact.Steps;

        Console.WriteLine($"    {keepPercent,3}% survive     {brute,12:N0}   {wp,15:N0}   {swap,11:N0}   {kept,10:N0} ints");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Read the last column first. That is the number this pattern exists to make
      zero, and it is the only column where the brute force loses. Every other
      column is a constant factor.

      Now read the two in-place columns against each other. They cross over:

        10% survive  ->  writeptr ~1,000    swap ~9,000     write pointer wins
        90% survive  ->  writeptr ~9,000    swap ~1,000     swap wins
       100% survive  ->  writeptr 10,000    swap 0          swap writes NOTHING

      The rule, and it is just the two definitions read back:

        write pointer   one write per KEEPER    -> cheap when FEW survive
        swap-with-last  one write per DISCARD   -> cheap when MOST survive

      The 100% row is the clearest case. Nothing is being removed, so
      swap-with-last has no holes to fill and does zero writes, while the write
      pointer dutifully copies all 10,000 elements onto themselves.

      Pick whichever you expect fewer of — and if order matters you have no
      choice, it is the write pointer.

      The brute force writes twice per keeper: once into the new list, once back
      into the array. Which is the smaller of its two problems.
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
    Console.WriteLine("  Half the elements survive. Steps are element writes.");
    Console.WriteLine();
    Console.WriteLine("            brute force                 write pointer");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    var rng = new Random(42);
    foreach (int n in (int[])[1_000, 10_000, 100_000, 1_000_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = rng.Next(2);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Compact.RemoveBrute((int[])data.Clone(), 0);
        Compact.RemoveWritePointer((int[])data.Clone(), 0);

        int[] a1 = (int[])data.Clone();
        Compact.Steps = 0;
        var sw = Stopwatch.StartNew();
        Compact.RemoveBrute(a1, 0);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Compact.Steps;

        int[] a2 = (int[])data.Clone();
        Compact.Steps = 0;
        sw.Restart();
        Compact.RemoveWritePointer(a2, 0);
        double wpMs = sw.Elapsed.TotalMilliseconds;
        long wpSteps = Compact.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {wpSteps,12:N0}   {wpMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Both curves are linear — this was never a time optimisation. The gap you
      see is a constant factor plus the cost of allocating and garbage-collecting
      a second array, which at n = 1,000,000 is 4 MB the other version never
      asks for.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE THREE IMPLEMENTATIONS
//
// All three remove every element equal to `remove`, modify the array in place,
// and return the number of elements that survived. Everything from that index
// onward is stale and must be ignored by the caller.
//
// RemoveBrute and RemoveWritePointer preserve the original order of the
// keepers. RemoveSwapWithLast does NOT — see the tests.
//
// Steps counts ELEMENT WRITES, because reads are identical across all three.
// =============================================================================

static class Compact
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n) time, O(n) SPACE. Order preserved.
    //
    // Collect the keepers somewhere else, then copy them back. Correct and
    // obvious, and the second array is the entire problem: it doubles the
    // memory and it is pure insurance against clobbering data you have not read
    // yet — a danger the write pointer proves cannot happen.
    // -------------------------------------------------------------------------
    public static int RemoveBrute(int[] a, int remove)
    {
        var kept = new List<int>();

        foreach (int x in a)
        {
            if (x != remove)
            {
                Steps++;                 // one write, into the new list
                kept.Add(x);
            }
        }

        for (int i = 0; i < kept.Count; i++)
        {
            Steps++;                     // one write, copying back
            a[i] = kept[i];
        }

        return kept.Count;
    }

    // -------------------------------------------------------------------------
    // WRITE POINTER — O(n) time, O(1) space. Order preserved.
    //
    // read visits every element; write marks where the next keeper belongs and
    // advances only on a keep. write <= read always holds, so every slot being
    // overwritten has already been read.
    //
    // One write per KEEPER.
    // -------------------------------------------------------------------------
    public static int RemoveWritePointer(int[] a, int remove)
    {
        int write = 0;

        for (int read = 0; read < a.Length; read++)
        {
            if (a[read] != remove)
            {
                Steps++;                 // one write
                a[write] = a[read];
                write++;
            }
        }

        return write;                    // write IS the new length
    }

    // -------------------------------------------------------------------------
    // SWAP WITH LAST — O(n) time, O(1) space. ORDER NOT PRESERVED.
    //
    // On a discard, pull the last live element into the hole and shrink the live
    // region. Do NOT advance i afterwards: the element just pulled in has not
    // been examined and may itself need discarding.
    //
    // One write per DISCARD — so this wins when most elements survive.
    // -------------------------------------------------------------------------
    public static int RemoveSwapWithLast(int[] a, int remove)
    {
        int n = a.Length, i = 0;

        while (i < n)
        {
            if (a[i] == remove)
            {
                Steps++;                 // one write
                n--;                     // shrink the live region
                a[i] = a[n];             // pull the last live element into the hole
                                         // i does NOT advance — a[i] is unexamined
            }
            else
            {
                i++;
            }
        }

        return n;
    }
}
