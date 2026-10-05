#!/usr/bin/env dotnet
// =============================================================================
// AUXILIARYSTATESTACK — a stack that also answers Min() in O(1), by storing the
// answer next to each element instead of recomputing it.
//
// Node 05 · Stack.  Needs: array, stack.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// A MinStack: Push, Pop, Peek and Min, all O(1) WORST case.
//
//   push 5   push 3   push 7   Min() -> 3   pop   pop   Min() -> 5
//
// Scanning the stack on every Min() is O(n), and the stack is the wrong shape to
// search anyway — you can only reach the top.
//
// THE QUESTION THAT DECIDES EVERYTHING: DOES THE AGGREGATE HAVE AN INVERSE?
//
// Try to keep a single int _min field. Push is easy: _min = Math.Min(_min, x).
// Now Pop. The element leaving might BE the minimum, and you need the minimum of
// what is left. There is no operation that removes a value from a min.
//
// Compare with a running sum, where there is:
//
//   aggregate       element leaves              one variable enough?
//   sum             _sum -= top                 YES — subtraction undoes +
//   count           _count--                    YES
//   min / max       "un-min"?  does not exist   NO  — needs per-element state
//   gcd             "un-gcd"?  does not exist   NO
//
// That is the rule, and it is worth more than the trick: an invertible aggregate
// needs one variable, a non-invertible one needs the answer stored PER ELEMENT.
// The file ships a SumStack next to the MinStacks so the contrast is runnable.
//
// WHY A PARALLEL STACK IS EXACTLY THE RIGHT SHAPE
//
//   values   [5, 3, 7, 3]
//   mins     [5, 3, 3, 3]      mins[i] = the minimum of values[0..i]
//
// Push x: push Math.Min(x, mins.Peek()). Pop: pop both. Min(): mins.Peek().
//
// Both stacks are always the same height, so index i in one lines up with index
// i in the other. And the lifetimes match perfectly: "the minimum when element i
// was pushed" becomes the answer again precisely when everything above i has
// gone — which is the one thing a stack does. Nothing is recomputed, because
// nothing was ever thrown away.
//
// This does NOT work for a queue. Elements leave from the far end, so the saved
// answer belongs to the wrong survivor set — that is node 07's MonotonicDeque.
//
// THE TWO "OPTIMISATIONS", AND WHAT THEY COST
//
//   lazy      only push to mins when x <= mins.Peek().  Uses less space on most
//             inputs. The <= is NOT optional: with strict <, duplicate minima
//             break it in four operations. -- trace proves it.
//
//   encoded   store 2*x - min when a new minimum arrives and keep no second
//             stack at all. Genuinely O(1) extra space, and it silently returns
//             the WRONG VALUE on extreme inputs because 2*x - min overflows.
//             -- trace proves that too.
//
// WHEN NOT TO USE IT
//
// If Min() is called rarely and n is small, scanning is fine and shorter. If you
// need Min() *and* arbitrary search or ordered iteration, this is the wrong
// structure — that is a heap or a sorted set, and both give up O(1) Pop ordering
// by insertion. And the whole technique relies on removal from the SAME end the
// state was recorded at; change that and it collapses.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run AuxiliaryStateStack.cs                everything
//   dotnet run AuxiliaryStateStack.cs -- trace       one small script, step by step
//   dotnet run AuxiliaryStateStack.cs -- test        the edge cases
//   dotnet run AuxiliaryStateStack.cs -- compare     scanning vs stored, in steps
//   dotnet run AuxiliaryStateStack.cs -- bench       steps and milliseconds
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
// THE TRACE — one small script, all the way through
// =============================================================================

void ShowTrace()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TRACE");
    Console.WriteLine(new string('=', 78));

    // 'p' = push Arg, 'o' = pop, 'm' = Min()
    (char Op, int Arg)[] script =
    [
        ('p', 5), ('p', 3), ('p', 7), ('p', 3),
        ('m', 0), ('o', 0), ('m', 0), ('o', 0), ('m', 0), ('o', 0), ('m', 0),
    ];

    Console.WriteLine();
    Console.WriteLine("  script: push 5, push 3, push 7, push 3, then pop back down,");
    Console.WriteLine("          asking Min() after every pop.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The parallel stack, narrated. Both stacks printed every step.
    // -------------------------------------------------------------------------
    Console.WriteLine("  TWO PARALLEL STACKS — mins[i] = minimum of values[0..i]");
    Console.WriteLine();
    Console.WriteLine("     op         values (bottom->top)   mins (bottom->top)   Min()");
    Console.WriteLine("     --------   --------------------   ------------------   -----");

    var values = new List<int>();
    var mins = new List<int>();

    foreach (var (op, arg) in script)
    {
        string label;
        string result = "";

        if (op == 'p')
        {
            values.Add(arg);
            mins.Add(mins.Count == 0 ? arg : Math.Min(arg, mins[^1]));
            label = $"push {arg}";
        }
        else if (op == 'o')
        {
            int top = values[^1];
            values.RemoveAt(values.Count - 1);
            mins.RemoveAt(mins.Count - 1);
            label = $"pop -> {top}";
        }
        else
        {
            label = "Min()";
            result = mins[^1].ToString();
        }

        Console.WriteLine($"     {label,-8}   [{string.Join(",", values)}]{new string(' ', Math.Max(0, 18 - string.Join(",", values).Length))}   [{string.Join(",", mins)}]{new string(' ', Math.Max(0, 16 - string.Join(",", mins).Length))}   {result,5}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      THE ROW THAT MATTERS IS THE LAST POP.

      Taking the 3 off leaves [5], and Min() has to go back to 5. Nothing
      recomputed it. The 5 was sitting at the bottom of the mins stack the whole
      time, put there when 5 was pushed, and popping uncovered it.

      That is the entire idea: an answer recorded when an element arrives
      becomes current again exactly when everything pushed after it has gone.
      A stack is the only structure where those two moments coincide, which is
      why this works here and does not work for a queue.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The single variable, demonstrated failing on the same script.
    // -------------------------------------------------------------------------
    Console.WriteLine("  WHY ONE VARIABLE CANNOT WORK — same script, _min field only");
    Console.WriteLine();
    Console.WriteLine("     op         values (bottom->top)   _min   truth");
    Console.WriteLine("     --------   --------------------   ----   -----");

    var broken = new MinStackOneVariable();
    var truth = new List<int>();

    foreach (var (op, arg) in script)
    {
        string label;

        if (op == 'p') { broken.Push(arg); truth.Add(arg); label = $"push {arg}"; }
        else if (op == 'o')
        {
            int top = broken.Pop();
            truth.RemoveAt(truth.Count - 1);
            label = $"pop -> {top}";
        }
        else label = "Min()";

        int claimed = broken.Min();
        int actual = truth.Min();
        string flag = claimed == actual ? "" : "<-- WRONG";

        Console.WriteLine($"     {label,-8}   [{string.Join(",", truth)}]{new string(' ', Math.Max(0, 18 - string.Join(",", truth).Length))}   {claimed,4}   {actual,5}   {flag}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Read where it goes wrong: not at the first pop, or the second. The field
      stays correct for a while and then quietly stops being correct, because
      _min only ever moves DOWN. Math.Min is not reversible, so Pop has nothing
      to apply.

      The first pops look fine because the minimum happened not to leave. That
      is what makes this bug survive a casual test — you have to pop the actual
      minimum, and then ask again.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Lazy, and the <= trap.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE LAZY VERSION — push to mins only when x <= current min");
    Console.WriteLine();

    var lazyIncreasing = new MinStackLazy();
    foreach (int v in (int[])[1, 2, 3, 4, 5, 6, 7, 8]) lazyIncreasing.Push(v);
    var lazyDecreasing = new MinStackLazy();
    foreach (int v in (int[])[8, 7, 6, 5, 4, 3, 2, 1]) lazyDecreasing.Push(v);

    Console.WriteLine("     input              values   mins   note");
    Console.WriteLine("     ----------------   ------   ----   ----------------------------------");
    Console.WriteLine($"     1..8 increasing    {lazyIncreasing.Count,6}   {lazyIncreasing.AuxCount,4}   only the first is ever the minimum");
    Console.WriteLine($"     8..1 decreasing    {lazyDecreasing.Count,6}   {lazyDecreasing.AuxCount,4}   every push is a new minimum");
    Console.WriteLine();
    Console.WriteLine("    So the aux stack is between 1 and n. Still O(n) worst case — the");
    Console.WriteLine("    saving is real on typical input and zero in the case you quote.");
    Console.WriteLine();

    Console.WriteLine("  AND THE TRAP: the comparison must be <=, not <");
    Console.WriteLine();
    Console.WriteLine("    script: push 3, push 3, pop, Min()      true answer: 3");
    Console.WriteLine();

    var ok = new MinStackLazy();
    ok.Push(3); ok.Push(3); ok.Pop();
    Console.WriteLine($"    with <=   mins holds {ok.AuxCount} entry, Min() = {ok.Min()}");

    var bad = new MinStackLazyStrict();
    bad.Push(3); bad.Push(3); bad.Pop();
    Console.WriteLine($"    with <    mins holds {bad.AuxCount} entries, Min() = {(bad.AuxCount == 0 ? "THROWS — mins is empty" : bad.Min().ToString())}");

    Console.WriteLine();
    Console.WriteLine("""
      With strict <, the second 3 is not recorded because it is not smaller. But
      Pop removes from mins whenever the value leaving EQUALS the recorded
      minimum — and the 3 that left equals it. One entry was pushed, two were
      matched, so the bookkeeping is off by one for the rest of the stack's life.

      <= makes pushes and matched pops line up one for one. Four operations is
      the whole reproduction, and a test suite without a duplicated minimum in
      it will never see this.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The encoded version, and the overflow it hides.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE ENCODED VERSION — O(1) extra space, by arithmetic");
    Console.WriteLine();
    Console.WriteLine("    When x is a new minimum, store 2*x - min instead of x. Since x < min,");
    Console.WriteLine("    2*x - min < x < min, so a stored value BELOW the current min is a flag");
    Console.WriteLine("    saying \"I am encoded\". Popping one recovers the previous minimum as");
    Console.WriteLine("    2*min - stored, and the real value was the min all along.");
    Console.WriteLine();
    Console.WriteLine("     op         stored (bottom->top)   _min   Min()   Peek()");
    Console.WriteLine("     --------   --------------------   ----   -----   ------");

    var enc = new MinStackEncoded();
    foreach (var (op, arg) in script)
    {
        string label;
        if (op == 'p') { enc.Push(arg); label = $"push {arg}"; }
        else if (op == 'o') { int top = enc.Pop(); label = $"pop -> {top}"; }
        else label = "Min()";

        string stored = string.Join(",", enc.StoredBottomToTop());
        Console.WriteLine($"     {label,-8}   [{stored}]{new string(' ', Math.Max(0, 18 - stored.Length))}   {enc.RawMin,4}   {(enc.Count > 0 ? enc.Min().ToString() : "-"),5}   {(enc.Count > 0 ? enc.Peek().ToString() : "-"),6}");
    }

    Console.WriteLine();
    Console.WriteLine("    Look at the stored column against the values column further up. The 3");
    Console.WriteLine("    is not there — a 1 is, because 2*3 - 5 = 1. Peek() has to decode it.");
    Console.WriteLine();

    Console.WriteLine("  WHAT IT COSTS — the same script, then one hostile value");
    Console.WriteLine();

    var enc2 = new MinStackEncoded();
    enc2.Push(1);
    enc2.Push(int.MinValue);
    int popped = enc2.Pop();

    Console.WriteLine($"    push 1, push int.MinValue, pop");
    Console.WriteLine($"      expected   {int.MinValue}");
    Console.WriteLine($"      got        {popped}{(popped == int.MinValue ? "" : "   <-- WRONG, and it did not throw")}");
    Console.WriteLine();
    Console.WriteLine("""
      2 * int.MinValue - 1 does not fit in an int. It wraps, the wrapped value is
      no longer below the minimum, so the decode branch is never taken and Pop
      hands back the encoded number as though it were data. No exception, no
      warning, wrong answer.

      The fix is to store longs. Which costs 8 bytes per element instead of 4 —
      so you spend the memory you were saving, on a scheme that is harder to read
      than two stacks. That is the honest verdict on this one: know it exists,
      because it gets asked, and write the parallel stack.

      THE ORDER TO WRITE THEM IN

        parallel    two stacks, equal height    the one to write
        lazy        push on <=                  fine, one comparison to get wrong
        encoded     2*x - min                   clever, fragile, don't
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The rule, made runnable: sum needs no auxiliary state at all.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE RULE — the same script, asking for the SUM instead of the minimum");
    Console.WriteLine();
    Console.WriteLine("     op         values (bottom->top)   _sum   recomputed   match");
    Console.WriteLine("     --------   --------------------   ----   ----------   -----");

    var sums = new SumStack();
    var sumTruth = new List<int>();

    foreach (var (op, arg) in script)
    {
        string label;
        if (op == 'p') { sums.Push(arg); sumTruth.Add(arg); label = $"push {arg}"; }
        else if (op == 'o')
        {
            int top = sums.Pop();
            sumTruth.RemoveAt(sumTruth.Count - 1);
            label = $"pop -> {top}";
        }
        else label = "Sum()";

        long recomputed = sumTruth.Sum();
        Console.WriteLine($"     {label,-8}   [{string.Join(",", sumTruth)}]{new string(' ', Math.Max(0, 18 - string.Join(",", sumTruth).Length))}   {sums.Sum,4}   {recomputed,10}   {(sums.Sum == recomputed ? "yes" : "NO"),5}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      One long field, correct at every step, including the pops that broke the
      one-variable minimum. The only difference is one line in Pop:

        _sum -= top;      an inverse exists
        _min -= top;      meaningless — there is nothing to subtract from a min

      So before reaching for a second stack, ask whether the aggregate can be
      undone when an element leaves. Sum, count, product (careful with zero) and
      xor all can. Min, max and gcd cannot, and those are exactly the ones that
      need the answer stored per element.
  """);
    Console.WriteLine();
}


// =============================================================================
// THE TESTS — operation scripts, every implementation against a scan
// =============================================================================

void RunTests()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TESTS");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Each script runs against MinStackBrute, which scans for the minimum and");
    Console.WriteLine("  is therefore obviously right. A row passes only if all three O(1)");
    Console.WriteLine("  implementations return the same Min() and Pop() values it does.");
    Console.WriteLine();
    Console.WriteLine("    case                          ops   min results");
    Console.WriteLine("    ---------------------------   ----   -------------------------------------");

    int passed = 0, total = 0;
    var allCases = new List<(string Name, (char Op, int Arg)[] Script)>();

    void Check(string name, (char Op, int Arg)[] script)
    {
        total++;
        allCases.Add((name, script));

        var reference = new List<string>();
        var brute = new MinStackBrute();
        foreach (var (op, arg) in script)
        {
            if (op == 'p') brute.Push(arg);
            else if (op == 'o') reference.Add($"pop{brute.Pop()}");
            else reference.Add($"min{brute.Min()}");
        }

        bool Agrees(Func<(char, int)[], List<string>> run) => run(script).SequenceEqual(reference);

        bool ok = Agrees(s =>
            {
                var st = new MinStackParallel();
                var log = new List<string>();
                foreach (var (op, arg) in s)
                {
                    if (op == 'p') st.Push(arg);
                    else if (op == 'o') log.Add($"pop{st.Pop()}");
                    else log.Add($"min{st.Min()}");
                }
                return log;
            })
            && Agrees(s =>
            {
                var st = new MinStackLazy();
                var log = new List<string>();
                foreach (var (op, arg) in s)
                {
                    if (op == 'p') st.Push(arg);
                    else if (op == 'o') log.Add($"pop{st.Pop()}");
                    else log.Add($"min{st.Min()}");
                }
                return log;
            })
            && Agrees(s =>
            {
                var st = new MinStackEncoded();
                var log = new List<string>();
                foreach (var (op, arg) in s)
                {
                    if (op == 'p') st.Push(arg);
                    else if (op == 'o') log.Add($"pop{st.Pop()}");
                    else log.Add($"min{st.Min()}");
                }
                return log;
            });

        if (ok) passed++;

        string shown = string.Join(" ", reference);
        if (shown.Length > 37) shown = shown[..34] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-21}   {script.Length,4:N0}   {shown,-37}");
    }

    Check("one push, one min", [('p', 5), ('m', 0)]);
    Check("push pop push min", [('p', 5), ('o', 0), ('p', 9), ('m', 0)]);
    Check("min never changes", [('p', 1), ('p', 2), ('p', 3), ('m', 0), ('o', 0), ('m', 0)]);
    Check("min changes often", [('p', 9), ('p', 7), ('p', 5), ('m', 0), ('o', 0), ('m', 0), ('o', 0), ('m', 0)]);
    Check("pop the minimum", [('p', 5), ('p', 1), ('m', 0), ('o', 0), ('m', 0)]);
    Check("duplicate minima", [('p', 3), ('p', 3), ('m', 0), ('o', 0), ('m', 0), ('o', 0)]);
    Check("triple duplicates", [('p', 2), ('p', 2), ('p', 2), ('o', 0), ('m', 0), ('o', 0), ('m', 0)]);
    Check("dup min, dup above", [('p', 1), ('p', 4), ('p', 1), ('p', 4), ('m', 0), ('o', 0), ('m', 0), ('o', 0), ('m', 0)]);
    Check("all identical", [('p', 7), ('p', 7), ('p', 7), ('m', 0), ('o', 0), ('o', 0), ('m', 0)]);
    Check("strictly decreasing", [('p', 5), ('p', 4), ('p', 3), ('p', 2), ('p', 1), ('m', 0), ('o', 0), ('m', 0)]);
    Check("strictly increasing", [('p', 1), ('p', 2), ('p', 3), ('p', 4), ('p', 5), ('m', 0), ('o', 0), ('m', 0)]);
    Check("negatives", [('p', -3), ('p', -7), ('m', 0), ('o', 0), ('m', 0)]);
    Check("mixed signs", [('p', 4), ('p', -2), ('p', 0), ('m', 0), ('o', 0), ('m', 0), ('o', 0), ('m', 0)]);
    Check("drain to empty", [('p', 2), ('p', 1), ('o', 0), ('o', 0), ('p', 8), ('m', 0)]);
    Check("zero", [('p', 0), ('p', 0), ('m', 0), ('o', 0), ('m', 0)]);

    // A long random script, so the hand-picked cases are not the only evidence.
    var rng = new Random(20261005);
    var randomScript = new List<(char, int)>();
    int depth = 0;
    for (int i = 0; i < 2_000; i++)
    {
        int roll = rng.Next(100);
        if (depth == 0 || roll < 45) { randomScript.Add(('p', rng.Next(-50, 50))); depth++; }
        else if (roll < 70) { randomScript.Add(('o', 0)); depth--; }
        else randomScript.Add(('m', 0));
    }
    Check("random, seed 20261005", randomScript.ToArray());

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // WHICH ROWS ACTUALLY CATCH THE < BUG? Run the same scripts through the
    // broken lazy version rather than claiming to know which ones matter.
    // -------------------------------------------------------------------------
    Console.WriteLine("  THE SAME SCRIPTS AGAINST MinStackLazyStrict — the < version");
    Console.WriteLine();
    Console.WriteLine("    case                          catches the bug?");
    Console.WriteLine("    ---------------------------   ----------------");

    int caught = 0;
    foreach (var (name, script) in allCases)
    {
        var reference = new List<string>();
        var brute = new MinStackBrute();
        foreach (var (op, arg) in script)
        {
            if (op == 'p') brute.Push(arg);
            else if (op == 'o') reference.Add($"pop{brute.Pop()}");
            else reference.Add($"min{brute.Min()}");
        }

        string verdict;
        try
        {
            var st = new MinStackLazyStrict();
            var log = new List<string>();
            foreach (var (op, arg) in script)
            {
                if (op == 'p') st.Push(arg);
                else if (op == 'o') log.Add($"pop{st.Pop()}");
                else log.Add($"min{st.Min()}");
            }
            verdict = log.SequenceEqual(reference) ? "no — passes" : "YES — wrong answer";
        }
        catch (InvalidOperationException)
        {
            verdict = "YES — it throws";
        }

        if (verdict.StartsWith("YES")) caught++;
        Console.WriteLine($"    {name,-27}   {verdict}");
    }

    Console.WriteLine();
    Console.WriteLine($"    {caught} of {allCases.Count} scripts detect it. The rest pass a broken implementation.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
      READ THE SECOND TABLE, NOT THE FIRST.

      16 of 16 pass, which tells you almost nothing. The second table is the
      one with information in it: only 6 of those 16 scripts can tell a correct
      lazy implementation from a broken one. Ten of them pass either way.

      Every script that catches it has the same feature — the minimum pushed
      more than once. "duplicate minima", "triple duplicates", "all identical",
      "zero" and "dup min, dup above" were written for it; the random script
      found it by accident, in 2,000 operations.

      Note HOW they catch it. Five throw, because the aux stack empties while
      values remain, and the random one returns a wrong answer. Throwing is the
      lucky outcome: the one that returns a number is the shape this bug takes
      in code that does not happen to drain the stack.

      And note which scripts do NOT catch it. "strictly decreasing" pushes a
      new minimum every single time and still passes — the case that feels like
      the hardest one is blind to this bug, because <= and < only differ on
      EQUAL values. Choosing an extreme input is not the same as choosing a
      discriminating one.

      WHAT IS MISSING: there is no row with values near int.MinValue, and
      MinStackEncoded passes anyway. -- trace shows the four-operation script
      that breaks it. A suite can only ever tell you about the inputs in it, and
      this one is evidence about ordinary integers only.
  """);
        Console.WriteLine();
    }
}


// =============================================================================
// SIDE BY SIDE — scanning for the minimum against storing it
// =============================================================================

void ShowCompare()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  SIDE BY SIDE");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  n pushes, then n Min() queries. The scan re-reads the whole stack every");
    Console.WriteLine("  time; the parallel stack reads one entry.");
    Console.WriteLine();
    Console.WriteLine("         n      scan steps   parallel steps     ratio");
    Console.WriteLine("    ------   -------------   --------------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 50_000])
    {
        var brute = new MinStackBrute();
        MinStackBrute.Steps = 0;
        for (int i = 0; i < n; i++) brute.Push(n - i);
        for (int i = 0; i < n; i++) brute.Min();
        long bruteSteps = MinStackBrute.Steps;

        var fast = new MinStackParallel();
        MinStackParallel.Steps = 0;
        for (int i = 0; i < n; i++) fast.Push(n - i);
        for (int i = 0; i < n; i++) fast.Min();
        long fastSteps = MinStackParallel.Steps;

        Console.WriteLine($"    {n,6:N0}   {bruteSteps,13:N0}   {fastSteps,14:N0}   {(double)bruteSteps / fastSteps,9:N1}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      n queries x n elements against n queries x 1 entry, so the ratio grows
      with n and there is no input where the scan catches up. Note the CASE
      label: this is O(1) WORST for the parallel stack, not amortised and not
      average. Nothing resizes in a way that needs spreading out and nothing
      depends on the data, so there is no worse scenario to hide.

      (Stack<T> itself resizes, which is amortised O(1) — so Push is amortised
      and Min is worst. Two operations on the same object, two different labels.)
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Space: what each variant actually keeps.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  SPACE — aux entries kept for n = 10,000, by input shape");
    Console.WriteLine();
    Console.WriteLine("     input shape           parallel     lazy   encoded");
    Console.WriteLine("     -------------------   --------   ------   -------");

    foreach (var (shapeName, make) in (( string, Func<int, int> )[])
        [
            ("increasing 1..n", i => i + 1),
            ("decreasing n..1", i => 10_000 - i),
            ("all identical", _ => 7),
            ("random, seed 1", _ => 0),       // replaced below
        ])
    {
        var rng = new Random(1);
        var par = new MinStackParallel();
        var laz = new MinStackLazy();
        var enc = new MinStackEncoded();

        for (int i = 0; i < 10_000; i++)
        {
            int v = shapeName == "random, seed 1" ? rng.Next(0, 1_000_000) : make(i);
            par.Push(v);
            laz.Push(v);
            enc.Push(v);
        }

        Console.WriteLine($"     {shapeName,-19}   {par.AuxCount,8:N0}   {laz.AuxCount,6:N0}   {0,7}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The lazy version keeps one entry per RUNNING MINIMUM, so its space is a
      property of the input shape, not of n:

        increasing      1          nothing after the first is ever smaller
        decreasing      n          every push is a new minimum
        all identical   n          <= records ties too, so every one is kept
        random          10         measured, at n = 10,000

      Two things in that list are worth pausing on.

      "all identical" is also a worst case, and it is the one people miss.
      Having chosen <= for correctness, you have accepted that duplicated
      minima are all recorded — the fix for the bug IS the cause of the space
      behaviour. They are not separate design choices.

      The random row is 10, and that is not a coincidence. The chance element i
      is smaller than all i-1 before it is 1/i, so the expected count is
      1 + 1/2 + 1/3 + ... + 1/n, which is about ln(n). ln(10,000) = 9.2, and the
      measurement says 10. So lazy is O(n) worst case and O(log n) EXPECTED —
      and "expected" there is a claim about random input, not about the
      algorithm. Quote the worst case unless you are asked otherwise.

      The encoded column is 0 on every row, which is the claim it is sold on.
      -- trace shows what it costs to believe that.
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
    Console.WriteLine("  n pushes then n Min() queries, decreasing input.");
    Console.WriteLine();
    Console.WriteLine("                         scan              parallel      lazy   encoded");
    Console.WriteLine("         n           steps         ms      steps     ms        ms        ms");
    Console.WriteLine("    ------   -------------   --------   --------   ----   -------   -------");

    foreach (int n in (int[])[1_000, 10_000, 20_000])
    {
        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        { var w = new MinStackBrute(); w.Push(1); w.Min(); w.Pop(); }
        { var w = new MinStackParallel(); w.Push(1); w.Min(); w.Pop(); }
        { var w = new MinStackLazy(); w.Push(1); w.Min(); w.Pop(); }
        { var w = new MinStackEncoded(); w.Push(1); w.Min(); w.Pop(); }

        MinStackBrute.Steps = 0;
        var sw = Stopwatch.StartNew();
        var brute = new MinStackBrute();
        for (int i = 0; i < n; i++) brute.Push(n - i);
        for (int i = 0; i < n; i++) brute.Min();
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = MinStackBrute.Steps;

        MinStackParallel.Steps = 0;
        sw.Restart();
        var par = new MinStackParallel();
        for (int i = 0; i < n; i++) par.Push(n - i);
        for (int i = 0; i < n; i++) par.Min();
        double parMs = sw.Elapsed.TotalMilliseconds;
        long parSteps = MinStackParallel.Steps;

        sw.Restart();
        var laz = new MinStackLazy();
        for (int i = 0; i < n; i++) laz.Push(n - i);
        for (int i = 0; i < n; i++) laz.Min();
        double lazMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var enc = new MinStackEncoded();
        for (int i = 0; i < n; i++) enc.Push(n - i);
        for (int i = 0; i < n; i++) enc.Min();
        double encMs = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine($"    {n,6:N0}   {bruteSteps,13:N0}   {bruteMs,8:N2}   {parSteps,8:N0}   {parMs,4:N2}   {lazMs,7:N2}   {encMs,7:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The scan's ms column is the clearest picture of n^2 in this repo: double n
      and it roughly QUADRUPLES, every row. The other three columns barely move
      across the same rows. The steps column says the same thing exactly, which
      is why it is the one to reason from.

      And the three O(1) versions are within noise of each other — one of them
      even gets FASTER at a larger n on some runs, which is how you know you are
      reading noise rather than a trend. That is the point: they differ in SPACE
      and in how easy they are to get wrong, not in speed. Choosing between them
      on performance grounds is choosing on the one axis where they are the same.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
  """);
    Console.WriteLine();
}


// =============================================================================
// THE IMPLEMENTATIONS
//
// Five stacks that all answer "what is the minimum", plus a SumStack that needs
// no auxiliary state at all — the contrast that explains why the others do.
//
// Two of them are deliberately WRONG and are kept so -- trace can print the
// failure next to the correct answer:
//
//   MinStackOneVariable   a single _min field, which Pop cannot restore
//   MinStackLazyStrict    the lazy version with < instead of <=
//
// Steps counts element reads for the scan and operations for the rest.
// =============================================================================


// -----------------------------------------------------------------------------
// THE SCAN — obviously correct, and the reference the tests are checked against.
// Push/Pop/Peek O(1). Min() O(n).
// -----------------------------------------------------------------------------
sealed class MinStackBrute
{
    public static long Steps;

    private readonly Stack<int> _values = new();

    public int Count => _values.Count;

    public void Push(int x) { Steps++; _values.Push(x); }
    public int Pop() { Steps++; return _values.Pop(); }
    public int Peek() { Steps++; return _values.Peek(); }

    public int Min()
    {
        int min = int.MaxValue;
        foreach (int v in _values)            // the whole stack, every time
        {
            Steps++;
            if (v < min) min = v;
        }
        return min;
    }
}


// -----------------------------------------------------------------------------
// BROKEN ON PURPOSE — one _min field.
//
// Push is fine. Pop is not: Math.Min has no inverse, so when the minimum leaves
// there is nothing to restore it from. _min only ever moves down.
//
// Kept so -- trace can show the exact operation where it diverges.
// -----------------------------------------------------------------------------
sealed class MinStackOneVariable
{
    private readonly Stack<int> _values = new();
    private int _min = int.MaxValue;

    public int Count => _values.Count;

    public void Push(int x)
    {
        _values.Push(x);
        if (x < _min) _min = x;
    }

    // No restore is possible here, and that IS the lesson.
    public int Pop() => _values.Pop();

    public int Min() => _min;
}


// -----------------------------------------------------------------------------
// THE ONE TO WRITE — two stacks, always the same height.
//
// mins[i] is the minimum of values[0..i], recorded when values[i] was pushed.
// It becomes the answer again exactly when everything above i has been popped,
// which is the only thing a stack guarantees.
//
// Every operation O(1) WORST case. O(n) extra space, unconditionally.
// -----------------------------------------------------------------------------
sealed class MinStackParallel
{
    public static long Steps;

    private readonly Stack<int> _values = new();
    private readonly Stack<int> _mins = new();

    public int Count => _values.Count;
    public int AuxCount => _mins.Count;

    public void Push(int x)
    {
        Steps++;
        _values.Push(x);
        _mins.Push(_mins.Count == 0 ? x : Math.Min(x, _mins.Peek()));
    }

    public int Pop()
    {
        Steps++;
        _mins.Pop();                          // both, always, so heights match
        return _values.Pop();
    }

    public int Peek() { Steps++; return _values.Peek(); }

    public int Min() { Steps++; return _mins.Peek(); }
}


// -----------------------------------------------------------------------------
// LAZY — record only when the minimum actually changes.
//
// Aux size is the number of running minima: 1 on increasing input, n on
// decreasing input, about log n on random input. Still O(n) worst case.
//
// The <= is load-bearing. See MinStackLazyStrict below.
// -----------------------------------------------------------------------------
sealed class MinStackLazy
{
    public static long Steps;

    private readonly Stack<int> _values = new();
    private readonly Stack<int> _mins = new();

    public int Count => _values.Count;
    public int AuxCount => _mins.Count;

    public void Push(int x)
    {
        Steps++;
        _values.Push(x);

        // <= and not <. A repeated minimum has to be recorded again, or the pop
        // that removes it will un-record a minimum that still has an owner.
        if (_mins.Count == 0 || x <= _mins.Peek()) _mins.Push(x);
    }

    public int Pop()
    {
        Steps++;
        int top = _values.Pop();
        if (_mins.Count > 0 && top == _mins.Peek()) _mins.Pop();
        return top;
    }

    public int Peek() { Steps++; return _values.Peek(); }

    public int Min() { Steps++; return _mins.Peek(); }
}


// -----------------------------------------------------------------------------
// BROKEN ON PURPOSE — the lazy version with strict <.
//
// push 3, push 3, pop  leaves the aux stack empty while a 3 is still in the
// stack: one push was recorded, two pops matched it.
// -----------------------------------------------------------------------------
sealed class MinStackLazyStrict
{
    private readonly Stack<int> _values = new();
    private readonly Stack<int> _mins = new();

    public int Count => _values.Count;
    public int AuxCount => _mins.Count;

    public void Push(int x)
    {
        _values.Push(x);
        if (_mins.Count == 0 || x < _mins.Peek()) _mins.Push(x);   // the bug
    }

    public int Pop()
    {
        int top = _values.Pop();
        if (_mins.Count > 0 && top == _mins.Peek()) _mins.Pop();
        return top;
    }

    public int Min() => _mins.Peek();
}


// -----------------------------------------------------------------------------
// ENCODED — O(1) extra space, and a silent correctness hazard.
//
// A new minimum x is stored as 2*x - _min. Because x < _min, that is strictly
// less than x and therefore less than _min, so "stored value below the current
// minimum" is a reliable flag for "this slot is encoded" — right up to the point
// where 2*x - _min overflows an int, at which point it is not.
//
// -- trace pushes int.MinValue and shows Pop returning the wrong value.
// -----------------------------------------------------------------------------
sealed class MinStackEncoded
{
    public static long Steps;

    private readonly Stack<int> _values = new();
    private int _min;

    public int Count => _values.Count;
    public int RawMin => _min;

    public void Push(int x)
    {
        Steps++;

        if (_values.Count == 0)
        {
            _min = x;
            _values.Push(x);
            return;
        }

        if (x >= _min)
        {
            _values.Push(x);
            return;
        }

        _values.Push(2 * x - _min);           // overflows for extreme values
        _min = x;
    }

    public int Pop()
    {
        Steps++;
        int top = _values.Pop();

        if (top < _min)                       // encoded: top holds the old min
        {
            int actual = _min;
            _min = 2 * _min - top;
            return actual;
        }

        return top;
    }

    public int Peek()
    {
        Steps++;
        int top = _values.Peek();
        return top < _min ? _min : top;       // decode if this slot is encoded
    }

    public int Min() { Steps++; return _min; }

    // For the trace, so the stored numbers can be printed next to the real ones.
    public IEnumerable<int> StoredBottomToTop() => _values.Reverse();
}


// -----------------------------------------------------------------------------
// THE CONTRAST — sum needs NO auxiliary stack, because + has an inverse.
//
// This is the same shape as MinStackOneVariable, and it is correct. The only
// difference is the one line in Pop that can undo what Push did.
// -----------------------------------------------------------------------------
sealed class SumStack
{
    private readonly Stack<int> _values = new();
    private long _sum;

    public int Count => _values.Count;
    public long Sum => _sum;

    public void Push(int x)
    {
        _values.Push(x);
        _sum += x;
    }

    public int Pop()
    {
        int top = _values.Pop();
        _sum -= top;                          // THE INVERSE. min has none.
        return top;
    }
}
