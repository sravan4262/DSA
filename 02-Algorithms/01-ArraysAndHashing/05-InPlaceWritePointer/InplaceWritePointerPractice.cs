#!/usr/bin/env dotnet
// =============================================================================
// REMOVE ELEMENT — two ways.
//
//   dotnet run InplaceWritePointerPractice.cs
//
// Both methods do the same job: remove every copy of `remove` from `a`, in
// place, and return how many elements are left. The surviving elements end up
// in a[0 .. returned-1]; whatever is past that is junk and nobody looks at it.
// =============================================================================

// Step counters. In a top-level-statements file these must be declared before
// anything uses them, so they live up here rather than next to their methods.
long bruteSteps = 0;
long writeSteps = 0;

Run([0, 1, 0, 3, 0, 12], 0);
Run([2, 2, 2, 2], 2);
Run([1, 2, 3], 9);
Compare();


void Run(int[] input, int remove)
{
    int[] a = (int[])input.Clone();
    int[] b = (int[])input.Clone();

    bruteSteps = 0;
    int ka = BruteForceShift(a, remove);

    writeSteps = 0;
    int kb = WritePointer(b, remove);

    Console.WriteLine();
    Console.WriteLine($"  input        [{string.Join(", ", input)}]   remove {remove}");
    Console.WriteLine($"  brute force  [{string.Join(", ", a[..ka])}]   length {ka}   {bruteSteps} steps");
    Console.WriteLine($"  write ptr    [{string.Join(", ", b[..kb])}]   length {kb}   {writeSteps} steps");
    Console.WriteLine($"  agree?       {ka == kb && a[..ka].SequenceEqual(b[..kb])}");
}


// =============================================================================
// BRUTE FORCE — O(n^2) time, O(1) space.
//
// When you find one to remove, close the hole by shifting everything after it
// one place left. The array gets logically shorter, so n comes down too.
//
// The waste: every shift re-writes elements that were already in the right
// place relative to each other. Removing k elements from n costs up to k x n
// writes.
// =============================================================================

int BruteForceShift(int[] a, int remove)
{
    int n = a.Length;                     // how many are still "live"
    int i = 0;

    while (i < n)
    {
        if (a[i] == remove)
        {
            for (int k = i; k < n - 1; k++)
            {
                bruteSteps++;             // one write
                a[k] = a[k + 1];          // close the hole
            }
            n--;                          // one fewer live element
            // do NOT advance i — whatever shifted into a[i] has not been checked
        }
        else
        {
            i++;
        }
    }

    return n;
}


// =============================================================================
// WRITE POINTER — O(n) time, O(1) space.
//
// Two indices walking the same array. `read` looks at every element; `write`
// marks where the next KEEPER goes.
//
// The invariant that makes it safe: write <= read, always. Writing at `write`
// can never clobber a cell that `read` has not already passed, because `write`
// only moves forward when `read` does, and it moves at most as often.
// =============================================================================

int WritePointer(int[] a, int remove)
{
    int write = 0;

    for (int read = 0; read < a.Length; read++)
    {
        if (a[read] != remove)
        {
            writeSteps++;                 // one write
            a[write] = a[read];
            write++;
        }
    }

    return write;                         // = how many survived
}


// =============================================================================
// The cost gap, as the array grows. Worst case for the brute force is an array
// that is ALL removals — every one of them shifts the whole remaining tail.
// =============================================================================

void Compare()
{
    Console.WriteLine();
    Console.WriteLine("  worst case — every element is removed");
    Console.WriteLine();
    Console.WriteLine("         n   brute writes   write-ptr writes        ratio");
    Console.WriteLine("    ------   ------------   ----------------   ----------");

    foreach (int n in (int[])[100, 1_000, 5_000, 10_000])
    {
        int[] a = new int[n];             // all zeros, remove 0
        int[] b = new int[n];

        bruteSteps = 0;
        BruteForceShift(a, 0);

        writeSteps = 0;
        WritePointer(b, 0);

        string ratio = writeSteps == 0 ? "-" : $"{(double)bruteSteps / writeSteps,9:N0}x";
        Console.WriteLine($"    {n,6:N0}   {bruteSteps,12:N0}   {writeSteps,16:N0}   {ratio,10}");
    }

    Console.WriteLine();
    Console.WriteLine("  The write pointer does ZERO writes here — nothing survives, so it");
    Console.WriteLine("  never copies anything. One write per KEEPER is its whole cost.");
    Console.WriteLine();
    Console.WriteLine("  worst case for the write pointer — nothing is removed");
    Console.WriteLine();
    Console.WriteLine("         n   brute writes   write-ptr writes");
    Console.WriteLine("    ------   ------------   ----------------");

    foreach (int n in (int[])[100, 1_000, 5_000, 10_000])
    {
        int[] a = new int[n];
        int[] b = new int[n];
        Array.Fill(a, 7);
        Array.Fill(b, 7);

        bruteSteps = 0;
        BruteForceShift(a, 0);            // 0 is not present

        writeSteps = 0;
        WritePointer(b, 0);

        Console.WriteLine($"    {n,6:N0}   {bruteSteps,12:N0}   {writeSteps,16:N0}");
    }

    Console.WriteLine();
    Console.WriteLine("  Here the brute force does nothing and the write pointer copies every");
    Console.WriteLine("  cell onto itself — a[0]=a[0], a[1]=a[1]. Still O(n), still fine, but");
    Console.WriteLine("  worth knowing it is not free. The swap-with-last variant writes once");
    Console.WriteLine("  per DISCARD instead, which wins on exactly the opposite input.");
    Console.WriteLine();
}
