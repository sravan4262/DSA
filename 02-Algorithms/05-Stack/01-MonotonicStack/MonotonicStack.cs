#!/usr/bin/env dotnet
// =============================================================================
// MONOTONICSTACK — keep a stack whose values only ever go one way, and the
// nested loop collapses to one pass.
//
// Node 05 · Stack.  Needs: array, stack.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The job used here: for every element, find the first element to its RIGHT
// that is larger. (-1 if there is none.) "Next greater element."
//
//   a    = [2, 1, 2, 4, 3]
//   next = [4, 2, 4, -1, -1]
//
// The obvious approach: for each i, scan right until something bigger turns up.
// O(n^2), and it re-scans the same stretch of array over and over.
//
// Instead keep a stack of indices whose VALUES are decreasing from bottom to
// top. Walk left to right, and before pushing i, pop everything smaller:
//
//   while (stack is not empty && a[i] > a[stack.Peek()])
//       answer[stack.Pop()] = a[i];      // a[i] is that element's next greater
//   stack.Push(i);
//
//   brute force   scan right from every i     O(n^2) time, O(1) space
//   this          one pass with a stack        O(n) time,   O(n) space
//
// WHAT THE STACK ACTUALLY HOLDS — say this and the code writes itself
//
// The stack holds the indices of elements that are still WAITING for an answer.
//
// And they are necessarily in decreasing order of value. Why? Suppose a smaller
// value sat below a larger one. The larger one arrived later, and when it
// arrived it would have popped the smaller one — that is exactly what the while
// loop does. So that arrangement cannot exist. The monotonicity is not
// maintained by extra bookkeeping; it is a consequence of the popping rule.
//
// That gives the two facts that matter:
//
//   the top is the element most recently left unanswered
//   anything a[i] can answer is a contiguous run from the top downward
//
// WHY A NESTED WHILE LOOP IS STILL O(n) — the amortised argument
//
// The code LOOKS like O(n^2): a for loop with a while loop inside it. It is not,
// and the reason is the thing to be able to say out loud:
//
//   every index is pushed exactly ONCE
//   every index is popped at most ONCE
//
// So across the whole run there are at most n pushes and n pops — 2n stack
// operations total, no matter how the inner loop is distributed. One iteration
// might pop five elements and the next might pop none; what is bounded is the
// TOTAL, not the per-iteration cost.
//
// That is an amortised argument, and it is the same shape as a dynamic array's
// doubling: an individual step can be expensive, the sequence cannot be.
// -- compare counts the pushes and pops so you can check 2n yourself.
//
// THE FOUR VARIANTS, AND HOW TO PICK
//
// Changing one comparison and one direction gives you all four questions:
//
//   next GREATER to the right    left to right,  pop while a[i] >  a[top]
//   next SMALLER to the right    left to right,  pop while a[i] <  a[top]
//   prev GREATER to the left     right to left,  pop while a[i] >  a[top]
//   prev SMALLER to the left     right to left,  pop while a[i] <  a[top]
//
// Use >= instead of > and ties answer each other, which changes the result on
// duplicate values — a choice the problem statement has to make. This file uses
// strict >, so equal values do NOT answer each other. See the tests.
//
// WHEN NOT TO USE IT
//
// It answers "the nearest element in one direction satisfying a comparison". It
// does not help with "the largest element to the right" (that is a suffix
// maximum — one backward pass, no stack), or with anything needing a count
// rather than a nearest. And it needs O(n) space, which the brute force does
// not.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run MonotonicStack.cs                everything
//   dotnet run MonotonicStack.cs -- trace       one small input, step by step
//   dotnet run MonotonicStack.cs -- test        the edge cases
//   dotnet run MonotonicStack.cs -- compare     brute force vs this, in steps
//   dotnet run MonotonicStack.cs -- bench       steps and milliseconds
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

    int[] a = [2, 1, 2, 4, 3];

    Console.WriteLine();
    Console.WriteLine($"  input: [{string.Join(", ", a)}]");
    Console.WriteLine("  job:   for each element, the FIRST larger element to its right");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The brute force, narrated. Scan right from every index.
    // -------------------------------------------------------------------------
    Console.WriteLine("  BRUTE FORCE — scan right from every index");
    Console.WriteLine();
    Console.WriteLine("     i   a[i]   looked at         answer");
    Console.WriteLine("     -   ----   ---------------   ------");

    long bruteSteps = 0;
    for (int i = 0; i < a.Length; i++)
    {
        var looked = new List<string>();
        int ans = -1;
        for (int j = i + 1; j < a.Length; j++)
        {
            bruteSteps++;
            looked.Add(a[j].ToString());
            if (a[j] > a[i]) { ans = a[j]; break; }
        }
        Console.WriteLine($"     {i}   {a[i],4}   {string.Join(" ", looked),-15}   {ans,6}");
    }

    Console.WriteLine();
    Console.WriteLine($"    {bruteSteps} comparisons. Notice index 1 re-reads what index 0 already read.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The monotonic stack, narrated. Stack contents shown every step.
    // -------------------------------------------------------------------------
    Console.WriteLine("  MONOTONIC STACK — one pass, stack holds who is still WAITING");
    Console.WriteLine();
    Console.WriteLine("     i   a[i]   pops (answered by a[i])   stack after (values)   pushes  pops");
    Console.WriteLine("     -   ----   -----------------------   --------------------   ------  ----");

    int[] answer = new int[a.Length];
    Array.Fill(answer, -1);
    var stack = new Stack<int>();
    long pushes = 0, pops = 0;

    for (int i = 0; i < a.Length; i++)
    {
        var popped = new List<string>();
        while (stack.Count > 0 && a[i] > a[stack.Peek()])
        {
            int idx = stack.Pop();
            pops++;
            answer[idx] = a[i];
            popped.Add($"a[{idx}]={a[idx]}");
        }

        stack.Push(i);
        pushes++;

        string stackShown = "[" + string.Join(",", stack.Reverse().Select(ix => a[ix])) + "]";
        Console.WriteLine($"     {i}   {a[i],4}   {(popped.Count == 0 ? "-" : string.Join(" ", popped)),-23}   {stackShown,-20}   {pushes,6}  {pops,4}");
    }

    Console.WriteLine();
    Console.Write("    left on the stack at the end: ");
    Console.WriteLine(string.Join(", ", stack.Reverse().Select(ix => $"a[{ix}]={a[ix]}")) + "  ->  answer -1, nothing larger exists");
    Console.WriteLine();
    Console.WriteLine($"    answer: [{string.Join(", ", answer)}]");
    Console.WriteLine($"    {pushes} pushes + {pops} pops = {pushes + pops} stack operations for n = {a.Length}.");
    Console.WriteLine();

    Console.WriteLine("""
      WHY THE STACK IS ALWAYS DECREASING, WITHOUT ANYONE ENFORCING IT

      Look at the stack column: 2 | 2,1 | 2,2 | 4 | 4,3. Never increasing from
      bottom to top. Nothing in the code sorts it or checks it.

      Suppose a smaller value sat BELOW a larger one. The larger one must have
      arrived later — and on arrival, the while loop would have popped the
      smaller one first. So that arrangement is unreachable. The monotonicity
      falls out of the popping rule rather than being maintained.

      Which gives the reading that makes the code obvious: the stack holds the
      elements still WAITING for an answer, newest on top, and whatever a[i] can
      answer is a run starting at the top.

      WHY THE NESTED WHILE IS STILL O(n)

      The shape is a for loop around a while loop, which looks quadratic. Count
      differently: every index is pushed exactly once and popped at most once,
      so the run does at most n pushes and n pops — 2n operations, here 5 + 3.

      One iteration popped three elements (i = 3) and two popped none. What is
      bounded is the TOTAL, not the per-step cost. That is an amortised
      argument, the same shape as a dynamic array's doubling.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The four variants, since one comparison changes the question.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE FOUR VARIANTS — one comparison and one direction apart");
    Console.WriteLine();

    int[] b = [2, 1, 2, 4, 3];
    Console.WriteLine($"    input: [{string.Join(", ", b)}]");
    Console.WriteLine();
    Console.WriteLine($"    next greater to the right   [{string.Join(", ", Mono.NextGreaterStack(b))}]");
    Console.WriteLine($"    next smaller to the right   [{string.Join(", ", Mono.NextSmallerRight(b))}]");
    Console.WriteLine($"    prev greater to the left    [{string.Join(", ", Mono.PrevGreaterLeft(b))}]");
    Console.WriteLine($"    prev smaller to the left    [{string.Join(", ", Mono.PrevSmallerLeft(b))}]");
    Console.WriteLine();
    Console.WriteLine("  Direction of the walk picks left or right. Direction of the comparison");
    Console.WriteLine("  picks greater or smaller. Everything else is identical, which is why it");
    Console.WriteLine("  is worth learning as one technique with a switch rather than as four");
    Console.WriteLine("  algorithms.");
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
    Console.WriteLine("  NextGreaterElement. -1 means nothing larger to the right.");
    Console.WriteLine();
    Console.WriteLine("    case                      input                     expect                      stack");
    Console.WriteLine("    -----------------------   -----------------------   -------------------------   -------------------------");

    int passed = 0, total = 0;

    void Check(string name, int[] a, int[] expect)
    {
        total++;
        int[] brute = Mono.NextGreaterBrute(a);
        int[] st = Mono.NextGreaterStack(a);
        bool ok = brute.SequenceEqual(expect) && st.SequenceEqual(expect);
        if (ok) passed++;

        string shown = a.Length == 0 ? "[]" : $"[{string.Join(",", a)}]";
        if (shown.Length > 23) shown = shown[..20] + "...";
        string exp = expect.Length == 0 ? "[]" : $"[{string.Join(",", expect)}]";
        if (exp.Length > 25) exp = exp[..22] + "...";
        string got = st.Length == 0 ? "[]" : $"[{string.Join(",", st)}]";
        if (got.Length > 25) got = got[..22] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-23}   {exp,-25}   {got,-25}");
    }

    Check("empty", [], []);
    Check("single", [5], [-1]);
    Check("two, increasing", [1, 2], [2, -1]);
    Check("two, decreasing", [2, 1], [-1, -1]);
    Check("strictly increasing", [1, 2, 3, 4], [2, 3, 4, -1]);
    Check("strictly decreasing", [4, 3, 2, 1], [-1, -1, -1, -1]);
    Check("the classic", [2, 1, 2, 4, 3], [4, 2, 4, -1, -1]);
    Check("all identical", [3, 3, 3], [-1, -1, -1]);
    Check("ties then bigger", [3, 3, 5], [5, 5, -1]);
    Check("one peak", [1, 5, 1], [5, -1, -1]);
    Check("one valley", [5, 1, 5], [-1, 5, -1]);
    Check("max at front", [9, 1, 2, 3], [-1, 2, 3, -1]);
    Check("max at back", [1, 2, 3, 9], [2, 3, 9, -1]);
    Check("negatives", [-3, -5, -1], [-1, -1, -1]);
    Check("mixed signs", [-2, 4, -6, 8], [4, 8, 8, -1]);
    Check("long pop run", [5, 4, 3, 2, 9], [9, 9, 9, 9, -1]);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Three cases earn their place.

          "all identical" and "ties then bigger" pin down the comparison. This
          file uses STRICT >, so equal values do not answer each other:
          [3,3,3] is all -1, and in [3,3,5] both 3s are answered by the 5 rather
          than the first 3 being answered by the second. Switch > to >= and both
          of those change. Neither is more correct — it is a question for the
          problem statement, and these two tests are how you notice the question
          exists.

          "long pop run" is the case where one iteration pops four elements at
          once. It is the amortised argument in miniature: that single step is
          O(n), and the whole run is still 2n operations.
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
    Console.WriteLine("  Worst case for the brute force: strictly DECREASING input, so every");
    Console.WriteLine("  element scans to the end and finds nothing.");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   stack ops (push+pop)   2n?        ratio");
    Console.WriteLine("    -----   -----------------   --------------------   ----   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = n - i;        // strictly decreasing

        Mono.Steps = 0;
        Mono.NextGreaterBrute(data);
        long brute = Mono.Steps;

        Mono.Steps = 0;
        Mono.NextGreaterStack(data);
        long st = Mono.Steps;

        Console.WriteLine($"    {n,5}   {brute,17:N0}   {st,20:N0}   {(st <= 2L * n ? "yes" : "NO"),4}   {(double)brute / st,9:N0}x");
    }

    Console.WriteLine();
    Console.WriteLine("  And now the brute force's BEST case: strictly increasing, so every");
    Console.WriteLine("  element finds its answer on the very first look.");
    Console.WriteLine();
    Console.WriteLine("        n   brute force steps   stack ops (push+pop)        ratio");
    Console.WriteLine("    -----   -----------------   --------------------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = i;            // strictly increasing

        Mono.Steps = 0;
        Mono.NextGreaterBrute(data);
        long brute = Mono.Steps;

        Mono.Steps = 0;
        Mono.NextGreaterStack(data);
        long st = Mono.Steps;

        string ratio = brute >= st ? $"{(double)brute / st,9:N2}x" : $"1/{(double)st / brute,-7:N2}";
        Console.WriteLine($"    {n,5}   {brute,17:N0}   {st,20:N0}   {ratio,10}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The first table is the headline: n(n-1)/2 against 2n, so the ratio is
      about n/4 and grows without limit. Check the "2n?" column — the stack
      count never exceeds 2n on any input, which IS the amortised claim, verified
      rather than asserted.

      The second table is the honest counterweight. On increasing input the
      brute force does n-1 comparisons and wins, because every element's answer
      is its immediate neighbour. Same lesson as insertion sort against
      mergesort in node 02: the O(n^2) method has a real best case, and the
      O(n) method's value is that it has no bad one.

      Space is the price. The brute force is O(1); the stack is O(n), and worst
      case it holds every element at once — which is exactly the decreasing
      input in the first table.
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
    Console.WriteLine("  Strictly decreasing input — the brute force's worst case.");
    Console.WriteLine();
    Console.WriteLine("            brute force                monotonic stack");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int n in (int[])[1_000, 10_000, 50_000, 100_000])
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++) data[i] = n - i;

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Mono.NextGreaterBrute(data);
        Mono.NextGreaterStack(data);

        Mono.Steps = 0;
        var sw = Stopwatch.StartNew();
        Mono.NextGreaterBrute(data);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Mono.Steps;

        Mono.Steps = 0;
        sw.Restart();
        Mono.NextGreaterStack(data);
        double stMs = sw.Elapsed.TotalMilliseconds;
        long stSteps = Mono.Steps;

        Console.WriteLine($"    {n,5}   {bruteSteps,12:N0}   {bruteMs,6:N2}     {stSteps,12:N0}   {stMs,6:N2}");
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
// NextGreaterBrute and NextGreaterStack both return, for each index, the first
// strictly larger value to its right, or -1. The other three methods are the
// same technique with the comparison or the direction flipped.
//
// Steps counts COMPARISONS for the brute force and STACK OPERATIONS (pushes +
// pops) for the stack version, which is the unit the amortised argument is
// stated in.
// =============================================================================

static class Mono
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n^2) worst, O(n) BEST. O(1) space.
    //
    // Scan right from every index. Its best case is real: on increasing input
    // every answer is the immediate neighbour, so it stops after one look.
    // -------------------------------------------------------------------------
    public static int[] NextGreaterBrute(int[] a)
    {
        int[] answer = new int[a.Length];

        for (int i = 0; i < a.Length; i++)
        {
            answer[i] = -1;
            for (int j = i + 1; j < a.Length; j++)
            {
                Steps++;                                 // one comparison
                if (a[j] > a[i]) { answer[i] = a[j]; break; }
            }
        }

        return answer;
    }

    // -------------------------------------------------------------------------
    // MONOTONIC STACK — O(n) time, O(n) space.
    //
    // The stack holds the indices of elements still WAITING for an answer, and
    // is therefore decreasing in value from bottom to top — a consequence of
    // the popping rule, not something maintained separately.
    //
    // Every index is pushed once and popped at most once, so the total is at
    // most 2n stack operations however the inner loop is distributed.
    // -------------------------------------------------------------------------
    public static int[] NextGreaterStack(int[] a)
    {
        int[] answer = new int[a.Length];
        Array.Fill(answer, -1);

        var stack = new Stack<int>();                    // indices, not values

        for (int i = 0; i < a.Length; i++)
        {
            // Strict >. With >= equal values would answer each other — a
            // different question. See the tests.
            while (stack.Count > 0 && a[i] > a[stack.Peek()])
            {
                Steps++;                                 // one pop
                answer[stack.Pop()] = a[i];
            }

            Steps++;                                     // one push
            stack.Push(i);
        }

        // Whatever is left never found anything larger, and was initialised -1.
        return answer;
    }

    // -------------------------------------------------------------------------
    // THE OTHER THREE VARIANTS — identical shape, one flip each.
    // -------------------------------------------------------------------------

    // Walk left to right, pop while SMALLER.
    public static int[] NextSmallerRight(int[] a)
    {
        int[] answer = new int[a.Length];
        Array.Fill(answer, -1);
        var stack = new Stack<int>();

        for (int i = 0; i < a.Length; i++)
        {
            while (stack.Count > 0 && a[i] < a[stack.Peek()]) answer[stack.Pop()] = a[i];
            stack.Push(i);
        }

        return answer;
    }

    // Walk RIGHT TO LEFT, pop while greater.
    public static int[] PrevGreaterLeft(int[] a)
    {
        int[] answer = new int[a.Length];
        Array.Fill(answer, -1);
        var stack = new Stack<int>();

        for (int i = a.Length - 1; i >= 0; i--)
        {
            while (stack.Count > 0 && a[i] > a[stack.Peek()]) answer[stack.Pop()] = a[i];
            stack.Push(i);
        }

        return answer;
    }

    // Walk RIGHT TO LEFT, pop while smaller.
    public static int[] PrevSmallerLeft(int[] a)
    {
        int[] answer = new int[a.Length];
        Array.Fill(answer, -1);
        var stack = new Stack<int>();

        for (int i = a.Length - 1; i >= 0; i--)
        {
            while (stack.Count > 0 && a[i] < a[stack.Peek()]) answer[stack.Pop()] = a[i];
            stack.Push(i);
        }

        return answer;
    }
}
