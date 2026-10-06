#!/usr/bin/env dotnet
// =============================================================================
// DIFFERENCE ARRAY — why storing the GAPS lets you skip writing the middle.
//
//   dotnet run DifferenceArrayPractice.cs
//
// Every number printed below is COMPUTED, not typed in. If the explanation and
// the arithmetic ever disagree, the program is the one telling the truth.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "explain") Explain();
if (mode is "all" or "three") ThreeUpdates();

BruteForce();


void Explain()
{
    // a difference array is similar to prefix sum, we store the differnce between adjacent elements
    // let's take an array of [1,3,5,3]
    // now difference array would be [1,2,2,-2]
    // now add 10 for index of (1 thru 3)
    //
    // the update touches only TWO cells, never the whole range:
    //      diff[l]     += v   ->  diff[1] += 10
    //      diff[r + 1] -= v   ->  diff[4] -= 10
    // so diff needs n+1 slots: [1,2,2,-2,0]
    //
    // now the new difference array would be [1,12,2,-2,-10]
    //                                           ^      ^
    //                                      only these two moved
    // now the prefix sum would be [1,13,15,13]
}
// =============================================================================
// THREE UPDATES ON A FOUR-NUMBER ARRAY
//
// Shows the diff array AND the rebuilt array after EVERY update. You would not
// normally rebuild in the middle — the whole point is to rebuild once at the
// end — but doing it here makes it visible that diff is a correct encoding of
// the array at every step, not just the last one.
//
// A brute-force copy (writing every cell in the range) runs alongside, so each
// rebuild is checked rather than asserted.
// =============================================================================

void ThreeUpdates()
{
    string Cells(IEnumerable<int> xs) => string.Concat(xs.Select(x => x.ToString().PadLeft(7)));
    string Text(IEnumerable<string> xs) => string.Concat(xs.Select(x => x.PadLeft(7)));

    int[] a = [1, 3, 5, 3];
    int n = a.Length;

    Console.WriteLine();
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("  THREE UPDATES, STEP BY STEP");
    Console.WriteLine(new string('=', 70));
    Console.WriteLine();
    Console.WriteLine($"    starting array   [{string.Join(", ", a)}]");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // BUILD. diff[0] = a[0], then each gap. One spare slot so r+1 is legal.
    // -------------------------------------------------------------------------
    int[] diff = new int[n + 1];
    diff[0] = a[0];
    for (int i = 1; i < n; i++) diff[i] = a[i] - a[i - 1];

    Console.WriteLine("  BUILD — diff[0] = a[0],  diff[i] = a[i] - a[i-1]");
    Console.WriteLine();
    Console.WriteLine("     index       " + Text(Enumerable.Range(0, n + 1).Select(i => i.ToString())));
    Console.WriteLine("     ---------   " + Text(Enumerable.Repeat("------", n + 1)));
    Console.WriteLine("     a           " + Cells(a) + "      -");
    Console.WriteLine("     diff        " + Cells(diff));
    Console.WriteLine();
    Console.WriteLine($"     gaps:  {a[0]},  {a[1]}-{a[0]}={diff[1]},  {a[2]}-{a[1]}={diff[2]},  {a[3]}-{a[2]}={diff[3]},  spare 0");
    Console.WriteLine();

    int[] brute = (int[])a.Clone();          // writes every cell, for checking

    int[] Rebuild()                          // prefix sum — the only way to read back
    {
        int[] result = new int[n];
        int running = 0;
        for (int i = 0; i < n; i++) { running += diff[i]; result[i] = running; }
        return result;
    }

    (int L, int R, int V)[] updates = [(1, 3, 10), (0, 1, 4), (2, 3, -2)];

    int step = 0;
    foreach (var (l, r, v) in updates)
    {
        step++;
        int[] diffBefore = (int[])diff.Clone();

        diff[l] += v;                        // THE UPDATE — two cells, always
        diff[r + 1] -= v;

        for (int i = l; i <= r; i++) brute[i] += v;     // the slow way, for comparison

        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"  UPDATE {step} of {updates.Length} — add {v} to indices {l}..{r}");
        Console.WriteLine(new string('-', 70));
        Console.WriteLine();
        Console.WriteLine($"    diff[{l}] += {v}        and        diff[{r + 1}] -= {v}");
        Console.WriteLine();
        Console.WriteLine("     index       " + Text(Enumerable.Range(0, n + 1).Select(i => i.ToString())));
        Console.WriteLine("     ---------   " + Text(Enumerable.Repeat("------", n + 1)));
        Console.WriteLine("     diff before " + Cells(diffBefore));
        Console.WriteLine("     diff after  " + Cells(diff));
        Console.WriteLine("     changed?    " + Text(Enumerable.Range(0, n + 1)
            .Select(i => diffBefore[i] == diff[i] ? "." : "YES")));
        Console.WriteLine();

        int touched = Enumerable.Range(0, n + 1).Count(i => diffBefore[i] != diff[i]);
        Console.WriteLine($"    {touched} cells changed, for a range covering {r - l + 1} elements.");
        Console.WriteLine();

        int[] rebuilt = Rebuild();
        Console.WriteLine("    REBUILD (prefix sum of diff) — normally you would NOT do this yet");
        Console.WriteLine();
        Console.WriteLine("     index       " + Text(Enumerable.Range(0, n).Select(i => i.ToString())));
        Console.WriteLine("     ---------   " + Text(Enumerable.Repeat("------", n)));
        Console.WriteLine("     running sum " + Cells(rebuilt));
        Console.WriteLine("     brute force " + Cells(brute));
        Console.WriteLine($"     match?      {rebuilt.SequenceEqual(brute)}");
        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("  FINAL");
    Console.WriteLine(new string('=', 70));
    Console.WriteLine();

    int[] final = Rebuild();
    Console.WriteLine($"    started with     [{string.Join(", ", a)}]");
    foreach (var (l, r, v) in updates)
        Console.WriteLine($"                     add {v,3} to indices {l}..{r}");
    Console.WriteLine();
    Console.WriteLine($"    final diff       [{string.Join(", ", diff)}]   <- what you actually stored");
    Console.WriteLine($"    final array      [{string.Join(", ", final)}]       <- prefix sum of it");
    Console.WriteLine($"    brute force      [{string.Join(", ", brute)}]       match: {final.SequenceEqual(brute)}");
    Console.WriteLine();
    Console.WriteLine("    CHECKED PER INDEX — original, plus every update covering it");
    Console.WriteLine();

    for (int i = 0; i < n; i++)
    {
        var covering = updates.Where(u => i >= u.L && i <= u.R).ToArray();
        string sum = a[i] + string.Concat(covering.Select(u => u.V >= 0 ? $" + {u.V}" : $" - {-u.V}"));
        Console.WriteLine($"     index {i}   {sum,-22} = {final[i],3}");
    }

    Console.WriteLine();
    Console.WriteLine("    Every update wrote exactly 2 cells. The ranges covered 3, 2 and 2");
    Console.WriteLine("    elements, so 7 writes the brute-force way against 6 here. At this");
    Console.WriteLine("    size it is a wash — the point is that 2 does not grow when the");
    Console.WriteLine("    ranges do.");
    Console.WriteLine();
}


void BruteForce()
{
        // a difference array is similar to prefix sum, we store the differnce between adjacent elements
    // let's take an array of [1,3,5,3]
    // now difference array would be [1,2,2,-2]
    // now add 10 for index of (1 thru 3)
    // now the new difference array would be [1,12,12,10]
    // now the prefix sum would be [1,13,25,35]
    // TODO
}
