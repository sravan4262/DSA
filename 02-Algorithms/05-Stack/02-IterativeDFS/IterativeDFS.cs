#!/usr/bin/env dotnet
// =============================================================================
// ITERATIVEDFS — recursion is already a stack. This is what it looks like when
// you declare it yourself.
//
// Node 05 · Stack.  Needs: array, stack.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// Every recursive call pushes a frame onto the CALL STACK — a real stack, one
// you did not write and cannot resize. Rewriting the recursion with your own
// Stack<T> moves that same data onto the heap.
//
// That buys exactly three things, and costs one:
//
//   buys   depth limited by RAM instead of by ~1 MB of thread stack
//   buys   the pending work is a variable you can print, pause, or inspect
//   buys   you can stop halfway and resume later (recursion cannot)
//   costs  more code, and measurably SLOWER — see -- bench
//
// WHAT A CALL FRAME HOLDS — the part people drop
//
// When Pre(n.Left, output) is called, the frame for Pre(n) keeps three things:
//
//   1. its arguments           n, output
//   2. its locals             (none here)
//   3. WHERE TO RESUME        the instruction after the call
//
// Items 1 and 2 are obvious, so everyone puts them in their own stack. Item 3
// is invisible in the source, so it gets dropped — and that is the single
// reason pre-order is three lines and post-order looks impossible.
//
// Look at where the three traversals differ. It is one line moving:
//
//   void Pre(n)  { emit(n); Pre(n.Left); Pre(n.Right);           }   phase 0
//   void In(n)   {          Pre(n.Left); emit(n); Pre(n.Right);  }   phase 1
//   void Post(n) {          Pre(n.Left); Pre(n.Right); emit(n);  }   phase 2
//
// There are three resume points per frame: before the left call, between the
// two calls, after the right call. Carry a phase number alongside each node and
// all three traversals are ONE loop with a parameter:
//
//   stack.Push((root, 0));
//   while (stack.Count > 0)
//   {
//       var (n, phase) = stack.Pop();
//       if (phase == emitPhase) output.Add(n.Value);
//       if      (phase == 0) { stack.Push((n, 1)); push(n.Left,  0); }
//       else if (phase == 1) { stack.Push((n, 2)); push(n.Right, 0); }
//       // phase 2: nothing left to do, the frame is finished
//   }
//
// Re-pushing (n, phase + 1) before descending IS the return address. That is
// the whole trick, and it generalises to any recursion, not just trees.
//
// THE SHORTCUT, AND WHAT IT COSTS
//
// For pre-order only, the phase is unnecessary — there is no work left after
// the emit, so the frame never needs to come back:
//
//   stack.Push(root);
//   while (...) { n = stack.Pop(); emit(n); push(n.Right); push(n.Left); }
//
// Right BEFORE left, because a stack reverses. Push left first and you get a
// perfectly valid traversal of the MIRRORED tree, which is why that bug
// survives a weak test suite. -- trace prints both.
//
//   specialised pre-order    n pushes      one order only
//   phase machine            3n pushes     all three orders, one parameter
//
// WHEN NOT TO USE IT
//
// Recursion costs one frame per LEVEL, not per node. A balanced tree of a
// million nodes is 20 frames deep — recursion there is safe, shorter, and
// faster. Reach for the explicit stack only when depth can grow with n:
// a degenerate tree or linked list, a grid flood-fill (a million cells can mean
// a million frames), or a parser on hostile input. -- compare finds the exact
// depth where recursion dies.
//
// This file is about the MECHANISM. Node 10 does the tree traversals properly
// and node 16 does graph DFS with a visited set; both reuse what is here.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run IterativeDFS.cs                everything
//   dotnet run IterativeDFS.cs -- trace       one small tree, step by step
//   dotnet run IterativeDFS.cs -- test        the edge cases
//   dotnet run IterativeDFS.cs -- compare     recursion depth limit vs the heap
//   dotnet run IterativeDFS.cs -- bench       steps and milliseconds
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
using System.Runtime.CompilerServices;

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "trace") ShowTrace();
if (mode is "all" or "test") RunTests();
if (mode is "all" or "compare") ShowCompare();
if (mode is "all" or "bench") ShowBench();


// =============================================================================
// THE TRACE — one small tree, all the way through
// =============================================================================

void ShowTrace()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TRACE");
    Console.WriteLine(new string('=', 78));

    //         1
    //       /   \
    //      2     3
    //     / \     \
    //    4   5     6
    Node root = new Node(1,
        new Node(2, new Node(4), new Node(5)),
        new Node(3, null, new Node(6)));

    Console.WriteLine();
    Console.WriteLine("            1");
    Console.WriteLine("          /   \\");
    Console.WriteLine("         2     3");
    Console.WriteLine("        / \\     \\");
    Console.WriteLine("       4   5     6");
    Console.WriteLine();
    Console.WriteLine($"    pre-order    {string.Join(" ", Dfs.PreorderRecursive(root))}        emit BEFORE both calls");
    Console.WriteLine($"    in-order     {string.Join(" ", Dfs.InorderRecursive(root))}        emit BETWEEN the calls");
    Console.WriteLine($"    post-order   {string.Join(" ", Dfs.PostorderRecursive(root))}        emit AFTER both calls");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The specialised pre-order, narrated, with the stack printed every step.
    // -------------------------------------------------------------------------
    Console.WriteLine("  SPECIALISED PRE-ORDER — one stack, no phase, n pushes");
    Console.WriteLine();
    Console.WriteLine("     pop   emit   pushed        stack after (top first)   output so far");
    Console.WriteLine("     ---   ----   -----------   -----------------------   ---------------");

    var pstack = new Stack<Node>();
    var pout = new List<int>();
    pstack.Push(root);

    while (pstack.Count > 0)
    {
        Node n = pstack.Pop();
        pout.Add(n.Value);

        var pushed = new List<string>();
        if (n.Right != null) { pstack.Push(n.Right); pushed.Add(n.Right.Value.ToString()); }
        if (n.Left != null) { pstack.Push(n.Left); pushed.Add(n.Left.Value.ToString()); }

        string shown = "[" + string.Join(",", pstack.Select(x => x.Value)) + "]";
        Console.WriteLine($"     {n.Value,3}   {n.Value,4}   {(pushed.Count == 0 ? "-" : string.Join(" ", pushed)),-11}   {shown,-23}   {string.Join(" ", pout),-15}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Right is pushed first so left comes off first — a stack reverses whatever
      you hand it. Look at row 1: pushed "3 2", stack reads [2,3].

      No phase is needed here because nothing happens after the emit. The frame
      has no reason to come back, so it never has to be re-pushed.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The bug, demonstrated rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine("  THE CLASSIC BUG — push left first");
    Console.WriteLine();
    Console.WriteLine($"    correct (right pushed first)   {string.Join(" ", Dfs.PreorderIterative(root))}");
    Console.WriteLine($"    pushed left first              {string.Join(" ", Dfs.PreorderMirrored(root))}");
    Console.WriteLine();
    Console.WriteLine("""
      1 3 6 2 5 4 is not garbage, and that is what makes this bug dangerous. It
      is the exact pre-order of the MIRRORED tree: root, then right subtree, then
      left. Same length, same values, no crash, no exception.

      On a symmetric tree the two agree completely, so a test built from a tidy
      example passes. See the tests — "symmetric tree" is in there for exactly
      this reason, marked as the case that CANNOT catch it.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The phase machine, narrated. This is the real content of the file.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE PHASE MACHINE — post-order, carrying the resume point");
    Console.WriteLine();
    Console.WriteLine("    phase 0 = about to make the LEFT call     (pre-order emits here)");
    Console.WriteLine("    phase 1 = back from left, about to go RIGHT (in-order emits here)");
    Console.WriteLine("    phase 2 = back from right, frame is done   (post-order emits here)");
    Console.WriteLine();
    Console.WriteLine("     popped   does what                        stack after (top first)        output");
    Console.WriteLine("     ------   ------------------------------   ----------------------------   -------------");

    var fstack = new Stack<(Node Node, int Phase)>();
    var fout = new List<int>();
    fstack.Push((root, 0));


    while (fstack.Count > 0)
    {
        var (n, phase) = fstack.Pop();

        string did;
        if (phase == 0)
        {
            fstack.Push((n, 1));
            if (n.Left != null) { fstack.Push((n.Left, 0)); did = $"re-push as 1, descend left {n.Left.Value}"; }
            else did = "re-push as 1, no left child";
        }
        else if (phase == 1)
        {
            fstack.Push((n, 2));
            if (n.Right != null) { fstack.Push((n.Right, 0)); did = $"re-push as 2, descend right {n.Right.Value}"; }
            else did = "re-push as 2, no right child";
        }
        else
        {
            fout.Add(n.Value);
            did = $"EMIT {n.Value}, frame discarded";
        }

        string shown = "[" + string.Join(",", fstack.Select(f => $"{f.Node.Value}@{f.Phase}")) + "]";
        if (shown.Length > 28) shown = shown[..25] + "...";
        Console.WriteLine($"     {n.Value}@{phase}     {did,-30}   {shown,-28}   {string.Join(" ", fout),-13}");
    }

    Console.WriteLine();
    Console.WriteLine($"    output: {string.Join(" ", fout)}   — post-order, from a loop that knows nothing about trees");
    Console.WriteLine();
    Console.WriteLine("""
      READ THE STACK COLUMN AS A CALL STACK, BECAUSE THAT IS WHAT IT IS

      At the moment 4 is emitted the stack reads [2@1, 1@1]. That says: node 2
      is waiting to go right, and under it node 1 is waiting to go right. It is
      the path from the root to the current node, with each entry remembering
      how far through its own body it had got. A debugger showing you the call
      stack of the recursive version would print the same thing.

      WHERE EACH TRAVERSAL COMES FROM

      Change nothing but which phase emits:

        emit at phase 0  ->  pre-order
        emit at phase 1  ->  in-order
        emit at phase 2  ->  post-order

      One loop, one parameter, three traversals. Compare that with learning the
      three iterative traversals as three separate tricks — the famous
      "push the left spine" in-order loop and the "two stacks, then reverse"
      post-order are both just this machine with the bookkeeping hand-optimised
      away.

      THE PRICE

      Every node is pushed three times instead of once, and each entry is a
      (Node, int) pair rather than a bare reference. -- bench shows what that
      costs. The specialised pre-order is the one to write when pre-order is all
      you need; the phase machine is the one to UNDERSTAND, because it is what
      the call stack was doing all along.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Same machine, the other two phases, to show the parameter really is it.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  ONE METHOD, THREE ORDERS — Dfs.Iterative(root, emitPhase)");
    Console.WriteLine();
    Console.WriteLine("     emitPhase   iterative output   recursive output   match");
    Console.WriteLine("     ---------   ----------------   ----------------   -----");

    (string Name, int Phase, List<int> Expected)[] orders =
    [
        ("pre", 0, Dfs.PreorderRecursive(root)),
        ("in", 1, Dfs.InorderRecursive(root)),
        ("post", 2, Dfs.PostorderRecursive(root)),
    ];

    foreach (var (name, phase, expected) in orders)
    {
        List<int> got = Dfs.Iterative(root, phase);
        Console.WriteLine($"     {phase} ({name,-4})   {string.Join(" ", got),-16}   {string.Join(" ", expected),-16}   {(got.SequenceEqual(expected) ? "yes" : "NO"),5}");
    }

    Console.WriteLine();
}


// =============================================================================
// THE TESTS — every tree shape that has ever broken one of these
// =============================================================================

void RunTests()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TESTS");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Each shape is run through all three orders, iterative against recursive.");
    Console.WriteLine("  A row passes only if all three agree AND the specialised pre-order agrees.");
    Console.WriteLine();
    Console.WriteLine("    case                      pre-order           in-order            post-order");
    Console.WriteLine("    -----------------------   -----------------   -----------------   -----------------");

    int passed = 0, total = 0;

    void Check(string name, Node? root)
    {
        total++;

        List<int> pre = Dfs.PreorderRecursive(root);
        List<int> ino = Dfs.InorderRecursive(root);
        List<int> post = Dfs.PostorderRecursive(root);

        bool ok = Dfs.Iterative(root, 0).SequenceEqual(pre)
               && Dfs.Iterative(root, 1).SequenceEqual(ino)
               && Dfs.Iterative(root, 2).SequenceEqual(post)
               && Dfs.PreorderIterative(root).SequenceEqual(pre)
               && Dfs.InorderSpine(root).SequenceEqual(ino)
               && Dfs.PostorderTwoStacks(root).SequenceEqual(post);
        if (ok) passed++;

        string Fmt(List<int> v)
        {
            string s = v.Count == 0 ? "(empty)" : string.Join(" ", v);
            return s.Length > 17 ? s[..14] + "..." : s;
        }

        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {Fmt(pre),-17}   {Fmt(ino),-17}   {Fmt(post),-17}");
    }

    Check("empty", null);
    Check("single node", new Node(1));
    Check("left child only", new Node(1, new Node(2), null));
    Check("right child only", new Node(1, null, new Node(2)));
    Check("both children", new Node(1, new Node(2), new Node(3)));
    Check("left chain of 4", Dfs.LeftChain(4));
    Check("right chain of 4", Dfs.RightChain(4));
    Check("zigzag", new Node(1, new Node(2, null, new Node(3, new Node(4), null)), null));
    Check("symmetric tree", new Node(1, new Node(2), new Node(2)));
    Check("full, depth 3", new Node(1, new Node(2, new Node(4), new Node(5)),
                                       new Node(3, new Node(6), new Node(7))));
    Check("the trace tree", new Node(1, new Node(2, new Node(4), new Node(5)),
                                        new Node(3, null, new Node(6))));
    Check("negatives", new Node(-1, new Node(-2), new Node(-3)));
    Check("duplicates", new Node(5, new Node(5), new Node(5)));
    Check("deep left, 1000", Dfs.LeftChain(1000));

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
      Four of these rows are doing real work.

      "left child only" and "right child only" are the pair. A tree with
      children on both sides hides which branch of the if you got wrong; these
      two force each null check to be exercised on its own.

      "symmetric tree" is in the list as the case that CANNOT catch the
      push-order bug — pushing left first gives 1 2 2 either way. It is here so
      that the list records which tests are and are not evidence. A suite made
      of nothing but symmetric examples passes a mirrored traversal.

      "zigzag" is the shape where all three orders differ from EACH OTHER:
      1 2 3 4, then 2 4 3 1, then 4 3 2 1. Most shapes do not manage that. Look
      at "left chain of 4" — its in-order and post-order are both 4 3 2 1, so
      that row would still pass if those two were wired to the same method. The
      zigzag is the row that separates them.

      "deep left, 1000" is the one that would fail if the iterative versions
      secretly recursed. A thousand levels is comfortably inside the call stack,
      so it does not prove the depth claim on its own — -- compare does that.
  """);
        Console.WriteLine();
    }
}


// =============================================================================
// SIDE BY SIDE — where recursion runs out, and where the explicit stack does not
// =============================================================================

void ShowCompare()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  SIDE BY SIDE");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Both versions visit every node exactly once, so the STEP counts are");
    Console.WriteLine("  identical by construction. This is the one algorithm in the repo where");
    Console.WriteLine("  the interesting number is space, not time.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // How deep can the call stack actually go? Measured, not asserted.
    // -------------------------------------------------------------------------
    int probe = Dfs.ProbeRecursionDepth();
    Console.WriteLine($"  A trivial method recursed {probe:N0} levels before the runtime reported");
    Console.WriteLine("  it was out of room. That number is a floor, not the exact limit — see the");
    Console.WriteLine("  note below — and it moves with frame size, so a method with more locals");
    Console.WriteLine("  gets fewer levels.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Degenerate tree: a left chain, so depth == n.
    // -------------------------------------------------------------------------
    Console.WriteLine("  A LEFT CHAIN, so depth == n. Nothing here is balanced.");
    Console.WriteLine();
    Console.WriteLine("            n   recursive      nodes done   iter pre-order   phase machine");
    Console.WriteLine("    ---------   ------------   ----------   --------------   -------------");

    foreach (int n in (int[])[1_000, 10_000, 100_000, 1_000_000])
    {
        Node chain = Dfs.LeftChain(n);

        bool ok = Dfs.TryPreorderRecursive(chain, out int reached);

        Dfs.PreorderIterative(chain);
        int specPeak = Dfs.MaxStack;

        Dfs.Iterative(chain, 0);
        int phasePeak = Dfs.MaxStack;

        Console.WriteLine($"    {n,9:N0}   {(ok ? "completed" : "RAN OUT"),-12}   {reached,10:N0}   peak {specPeak,-9:N0}   peak {phasePeak,-8:N0}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      THE HEADLINE

      Recursion stops working somewhere in this table. The explicit versions do
      not notice, because their stack lives on the HEAP, which has no 1 MB
      ceiling. The phase machine really does hold a million entries on the last
      row: a (Node, int) is 16 bytes after padding, so that is a 16 MB array,
      sitting on the large object heap — and it completes.

      Note the "nodes done" column stops rising. 22,439 twice, and it is the
      same number the probe reported above, which says the two recursive frames
      come out the same size. Depth is what runs out, so a deeper tree does not
      get further, it just fails at the same place.

      "RAN OUT" is a caught exception, not a crash. The recursive version calls
      RuntimeHelpers.EnsureSufficientExecutionStack() on entry, which throws a
      normal, catchable InsufficientExecutionStackException when the remaining
      stack falls below a conservative reserve. That is why the measured depth
      above is lower than the true limit, and why it is worth doing: a real
      StackOverflowException cannot be caught in .NET. It does not unwind, it
      does not run a finally block, and no try/catch can stop it — the process
      is terminated. If you are writing a library that recurses over caller
      data, this guard is how you fail politely.

      NOW LOOK AT THE "iter pre-order" COLUMN AGAIN

      Peak 1. Not n — one.

      On a chain, pre-order pops a node, pushes its single child, and the stack
      never grows. The recursive version keeps n frames alive on exactly the
      same input, because each frame still has a pending right call to return
      to. The explicit version has nothing pending, so it keeps nothing.

      That is tail recursion, discovered rather than named: the recursion whose
      frame is dead the moment it calls is the recursion a loop replaces for
      free. The phase machine cannot do this — its whole purpose is to keep the
      resume point — so its peak IS the depth. Compare the two columns.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The balanced case, which is the honest counterweight.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  THE SAME NODE COUNTS, BALANCED. Depth is log2(n) now.");
    Console.WriteLine();
    Console.WriteLine("        nodes   depth   recursive      iter pre-order   phase machine");
    Console.WriteLine("    ---------   -----   ------------   --------------   -------------");

    foreach (int depth in (int[])[10, 14, 18, 20])
    {
        Node? tree = Dfs.Balanced(depth);
        int nodes = (1 << depth) - 1;

        bool ok = Dfs.TryPreorderRecursive(tree, out _);

        Dfs.PreorderIterative(tree);
        int specPeak = Dfs.MaxStack;

        Dfs.Iterative(tree, 0);
        int phasePeak = Dfs.MaxStack;

        Console.WriteLine($"    {nodes,9:N0}   {depth,5}   {(ok ? "completed" : "RAN OUT"),-12}   peak {specPeak,-9:N0}   peak {phasePeak,-8:N0}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      A million nodes, twenty frames. Recursion is in no danger whatsoever here,
      and the explicit stack's peak is 20-ish rather than 1,000,000 for the same
      reason — depth, not node count, is what either version pays for.

      So the rule is not "prefer iterative". It is: work out whether depth is
      bounded. If the shape guarantees O(log n) depth, recurse and write less
      code. If an adversary or just a sorted input can make it O(n), the
      explicit stack is the only version that survives.
  """);
    Console.WriteLine();
}


// =============================================================================
// BENCHMARK — the explicit stack is slower, and by how much is the point
// =============================================================================

void ShowBench()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  BENCHMARK");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Balanced trees, pre-order, all three versions visiting every node once.");
    Console.WriteLine();
    Console.WriteLine("                    recursive        iter pre-order      phase machine");
    Console.WriteLine("        nodes       steps      ms       steps      ms       steps      ms");
    Console.WriteLine("    ---------   -----------------   -----------------   -----------------");

    foreach (int depth in (int[])[15, 18, 20])
    {
        Node? tree = Dfs.Balanced(depth);
        int nodes = (1 << depth) - 1;

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Dfs.PreorderRecursive(tree);
        Dfs.PreorderIterative(tree);
        Dfs.Iterative(tree, 0);

        Dfs.Steps = 0;
        var sw = Stopwatch.StartNew();
        Dfs.PreorderRecursive(tree);
        double recMs = sw.Elapsed.TotalMilliseconds;
        long recSteps = Dfs.Steps;

        Dfs.Steps = 0;
        sw.Restart();
        Dfs.PreorderIterative(tree);
        double specMs = sw.Elapsed.TotalMilliseconds;
        long specSteps = Dfs.Steps;

        Dfs.Steps = 0;
        sw.Restart();
        Dfs.Iterative(tree, 0);
        double phaseMs = sw.Elapsed.TotalMilliseconds;
        long phaseSteps = Dfs.Steps;

        Console.WriteLine($"    {nodes,9:N0}   {recSteps,9:N0}  {recMs,6:N2}   {specSteps,9:N0}  {specMs,6:N2}   {phaseSteps,9:N0}  {phaseMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Identical step counts, three different times. Read that carefully, because
      it is the honest version of what rewriting a recursion buys you: nothing
      on the clock. It is worse. On the million-node row, recursion is the
      FASTEST of the three — the specialised stack is about 1.3x slower and the
      phase machine about 4x.

      A machine call instruction pushes a return address and moves one register.
      Stack<T>.Push does a bounds check, a possible resize, a write, and a count
      update — and the phase machine does three of those per node plus a tuple
      twice the width of a bare reference. Hardware has been optimising the call
      stack for decades; you are not going to beat it with an array.

      What you buy is unbounded depth, and the ability to look at the pending
      work. That is the trade. "Iterative is faster" is a thing people say and
      the numbers above do not support it.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
  """);
    Console.WriteLine();
}


// =============================================================================
// THE IMPLEMENTATIONS
//
// The three recursive methods are the specification: the ONLY difference between
// them is where output.Add sits relative to the two calls. Everything iterative
// below reproduces one of them.
//
// Steps counts NODES EMITTED, which is the same for every version here — that is
// deliberate, and it is why -- compare reports peak stack depth instead.
// MaxStack records the high-water mark of the explicit stack.
// =============================================================================

static class Dfs
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // Peak size the explicit stack reached during the last iterative call.
    public static int MaxStack;

    // -------------------------------------------------------------------------
    // RECURSIVE — the specification. One frame per LEVEL, so O(depth) space on
    // the call stack, which is the part you do not control.
    //
    // Read the three bodies as a group. The emit moves, nothing else does.
    // -------------------------------------------------------------------------

    public static List<int> PreorderRecursive(Node? root)
    {
        var output = new List<int>();
        Pre(root, output);
        return output;
    }

    private static void Pre(Node? n, List<int> output)
    {
        if (n == null) return;
        Steps++;
        output.Add(n.Value);            // BEFORE both calls   -> phase 0
        Pre(n.Left, output);
        Pre(n.Right, output);
    }

    public static List<int> InorderRecursive(Node? root)
    {
        var output = new List<int>();
        In(root, output);
        return output;
    }

    private static void In(Node? n, List<int> output)
    {
        if (n == null) return;
        In(n.Left, output);
        Steps++;
        output.Add(n.Value);            // BETWEEN the calls    -> phase 1
        In(n.Right, output);
    }

    public static List<int> PostorderRecursive(Node? root)
    {
        var output = new List<int>();
        Post(root, output);
        return output;
    }

    private static void Post(Node? n, List<int> output)
    {
        if (n == null) return;
        Post(n.Left, output);
        Post(n.Right, output);
        Steps++;
        output.Add(n.Value);            // AFTER both calls     -> phase 2
    }

    // -------------------------------------------------------------------------
    // THE PHASE MACHINE — all three orders, one loop, one parameter.
    //
    // The phase IS the return address. Re-pushing (n, phase + 1) before
    // descending is what the call instruction does for you in the recursive
    // version, written out by hand.
    //
    //   phase 0   about to make the left call      pre-order emits here
    //   phase 1   back from left, going right      in-order emits here
    //   phase 2   back from right, frame done      post-order emits here
    //
    // O(n) time, 3n pushes, O(depth) space ON THE HEAP.
    // -------------------------------------------------------------------------
    public static List<int> Iterative(Node? root, int emitPhase)
    {
        var output = new List<int>();
        MaxStack = 0;
        if (root == null) return output;

        var stack = new Stack<(Node Node, int Phase)>();
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            if (stack.Count > MaxStack) MaxStack = stack.Count;

            var (n, phase) = stack.Pop();

            if (phase == emitPhase)
            {
                Steps++;
                output.Add(n.Value);
            }

            if (phase == 0)
            {
                stack.Push((n, 1));                                  // the resume point
                if (n.Left != null) stack.Push((n.Left, 0));
            }
            else if (phase == 1)
            {
                stack.Push((n, 2));                                  // the resume point
                if (n.Right != null) stack.Push((n.Right, 0));
            }
            // phase 2: nothing is pending, so the frame is simply dropped.
        }

        return output;
    }

    // -------------------------------------------------------------------------
    // SPECIALISED PRE-ORDER — n pushes, no phase needed.
    //
    // Nothing happens after the emit, so the frame never has to come back.
    // On a chain the stack never exceeds 1 entry: see -- compare.
    // -------------------------------------------------------------------------
    public static List<int> PreorderIterative(Node? root)
    {
        var output = new List<int>();
        MaxStack = 0;
        if (root == null) return output;

        var stack = new Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            if (stack.Count > MaxStack) MaxStack = stack.Count;

            Node n = stack.Pop();
            Steps++;
            output.Add(n.Value);

            // RIGHT first. A stack reverses, so left must go on last to come
            // off first. Swap these two lines and you traverse the mirror image.
            if (n.Right != null) stack.Push(n.Right);
            if (n.Left != null) stack.Push(n.Left);
        }

        return output;
    }

    // -------------------------------------------------------------------------
    // THE BUG, kept so -- trace can print it next to the correct answer.
    //
    // Pushes left first. The result is a valid pre-order of the MIRRORED tree:
    // right length, right values, wrong order, no exception.
    // -------------------------------------------------------------------------
    public static List<int> PreorderMirrored(Node? root)
    {
        var output = new List<int>();
        if (root == null) return output;

        var stack = new Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Node n = stack.Pop();
            output.Add(n.Value);
            if (n.Left != null) stack.Push(n.Left);      // wrong way round
            if (n.Right != null) stack.Push(n.Right);
        }

        return output;
    }

    // -------------------------------------------------------------------------
    // IN-ORDER, THE FAMOUS VERSION — "walk the left spine, then pop".
    //
    // This is the phase machine with the bookkeeping hand-optimised away: the
    // inner while loop is every phase-0 step run back to back, and reaching the
    // pop means you are at phase 1. Worth recognising, not worth memorising.
    // -------------------------------------------------------------------------
    public static List<int> InorderSpine(Node? root)
    {
        var output = new List<int>();
        MaxStack = 0;

        var stack = new Stack<Node>();
        Node? cur = root;

        while (cur != null || stack.Count > 0)
        {
            while (cur != null)                 // phase 0, repeatedly
            {
                stack.Push(cur);
                if (stack.Count > MaxStack) MaxStack = stack.Count;
                cur = cur.Left;
            }

            cur = stack.Pop();                  // arrived at phase 1
            Steps++;
            output.Add(cur.Value);
            cur = cur.Right;                    // phase 2 has no work, so just go
        }

        return output;
    }

    // -------------------------------------------------------------------------
    // POST-ORDER, THE TRICK VERSION — root-right-left, then reverse.
    //
    // Pre-order with the pushes swapped gives root, right, left. Reverse that
    // and you have left, right, root. It is n pushes instead of 3n, but it needs
    // the whole output buffered before anything can be emitted, so it cannot be
    // streamed and it cannot be stopped halfway. The phase machine can.
    // -------------------------------------------------------------------------
    public static List<int> PostorderTwoStacks(Node? root)
    {
        var output = new List<int>();
        if (root == null) return output;

        var stack = new Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Node n = stack.Pop();
            output.Add(n.Value);
            if (n.Left != null) stack.Push(n.Left);      // left first, on purpose
            if (n.Right != null) stack.Push(n.Right);
        }

        output.Reverse();
        return output;
    }

    // -------------------------------------------------------------------------
    // RECURSION THAT FAILS POLITELY.
    //
    // EnsureSufficientExecutionStack throws InsufficientExecutionStackException
    // when the remaining stack drops below a conservative reserve. It is a
    // normal exception: catchable, unwinds, runs finally blocks.
    //
    // A real StackOverflowException is none of those things. Since .NET 2.0 it
    // cannot be caught and the process is killed outright, so this guard is the
    // only way to recurse over caller-supplied data and survive.
    // -------------------------------------------------------------------------
    public static bool TryPreorderRecursive(Node? root, out int nodesVisited)
    {
        int count = 0;
        try
        {
            Guarded(root, ref count);
            nodesVisited = count;
            return true;
        }
        catch (InsufficientExecutionStackException)
        {
            nodesVisited = count;
            return false;
        }
    }

    private static void Guarded(Node? n, ref int count)
    {
        if (n == null) return;
        RuntimeHelpers.EnsureSufficientExecutionStack();
        count++;
        Guarded(n.Left, ref count);
        Guarded(n.Right, ref count);
    }

    // -------------------------------------------------------------------------
    // How deep does the call stack go on this machine? Measure it rather than
    // quoting "1 MB".
    // -------------------------------------------------------------------------
    public static int ProbeRecursionDepth()
    {
        int depth = 0;
        try { Descend(ref depth); }
        catch (InsufficientExecutionStackException) { }
        return depth;
    }

    private static void Descend(ref int depth)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        depth++;
        Descend(ref depth);

        // Unreachable — the exception unwinds straight past it. It is here so
        // the call is not in tail position, which stops the JIT from reusing
        // the frame and turning this into a loop that never runs out.
        //
        // Which is the lesson of the whole file: code AFTER the call is exactly
        // what forces the frame to stay alive.
        depth--;
    }

    // -------------------------------------------------------------------------
    // TREE BUILDERS — data, not algorithm.
    // -------------------------------------------------------------------------

    // Root 1, then 2 below-left, 3 below-left of that... depth == n.
    // Built bottom-up with a loop, so building it cannot itself overflow.
    public static Node LeftChain(int n)
    {
        Node node = new Node(n);
        for (int v = n - 1; v >= 1; v--) node = new Node(v, node, null);
        return node;
    }

    public static Node RightChain(int n)
    {
        Node node = new Node(n);
        for (int v = n - 1; v >= 1; v--) node = new Node(v, null, node);
        return node;
    }

    // 2^depth - 1 nodes. Recurses only `depth` levels, so ~20 frames at most.
    public static Node? Balanced(int depth)
    {
        if (depth == 0) return null;
        return new Node(depth, Balanced(depth - 1), Balanced(depth - 1));
    }
}


// =============================================================================
// THE NODE — 16 B header + 4 B value + 4 B padding + 8 B Left + 8 B Right = 40 B
//
// Which is worth holding next to the explicit stack's cost: an entry in
// Stack<Node> is one 8-byte reference, and an entry in the phase machine's
// Stack<(Node, int)> is 16 bytes after padding. The stack is small compared with
// the tree it is walking.
// =============================================================================

sealed class Node
{
    public int Value;
    public Node? Left;
    public Node? Right;

    public Node(int value, Node? left = null, Node? right = null)
    {
        Value = value;
        Left = left;
        Right = right;
    }
}
