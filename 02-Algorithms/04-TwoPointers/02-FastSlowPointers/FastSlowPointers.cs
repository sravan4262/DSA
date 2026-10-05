#!/usr/bin/env dotnet
// =============================================================================
// FASTSLOWPOINTERS — two pointers in the SAME direction at different speeds,
// so their gap does the measuring for you.
//
// Node 04 · Two Pointers.  Needs: array, linked list.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// ConvergingPointers (01) walks two pointers toward each other. This walks them
// the same way at different rates. The trick is that the GAP between them
// carries information you would otherwise need a second pass or extra memory to
// get.
//
// Three jobs, one shape:
//
//   FindMiddle      fast moves 2, slow moves 1. When fast hits the end, slow
//                   is at the middle. ONE pass, no length needed.
//   NthFromEnd      move fast n ahead, then advance both. When fast hits the
//                   end, slow is n from the end. ONE pass.
//   HasCycle        fast moves 2, slow moves 1. If there is a loop they MEET.
//                   O(1) space instead of a visited set.
//
//   brute force   count the length, then walk again   two passes, or O(n) space
//   this          one pass, two pointers              one pass, O(1) space
//
// WHY THE GAP MEASURES THINGS
//
// If fast moves at 2 and slow at 1, then after k steps fast has gone 2k and
// slow has gone k — so slow is always at exactly HALF of fast's distance. Let
// fast run until it falls off the end at n, and slow is sitting at n/2. You
// never counted anything; the ratio did it.
//
// For NthFromEnd the gap is FIXED instead of growing: put fast n nodes ahead,
// then move both at the same speed so the gap stays n. When fast reaches the
// end, slow is n behind the end. Again, no counting.
//
// WHY A CYCLE FORCES A MEETING — the part worth actually proving
//
// Once both pointers are inside the loop, look at the distance from fast to
// slow measured FORWARD around the loop. Every step, fast gains exactly 1 on
// slow (it moves 2, slow moves 1). So that distance shrinks by 1 per step:
//
//   gap = 5, 4, 3, 2, 1, 0        it cannot skip 0, because it decreases by 1
//
// A gap that decreases by exactly one each step must hit zero. It cannot jump
// over it. So they meet, in at most (loop length) steps after both are inside.
//
// That is why the speeds must be 1 and 2. With 1 and 3 the gap shrinks by 2 per
// step and CAN step over zero — in a loop of even length it may never meet. The
// classic speeds are not arbitrary.
//
// For a list with no cycle, fast simply runs off the end and the loop exits.
//
// THE TWO BUGS THIS ALWAYS HAS
//
//   1. Null checks on the FAST pointer, in the right order:
//
//        while (fast != null && fast.next != null)       correct
//        while (fast.next != null && fast != null)       throws
//
//      fast takes two steps per iteration, so BOTH fast and fast.next must
//      exist before you move. Checking slow is pointless — it can never be
//      ahead of fast.
//
//   2. "Middle" of an even-length list is a choice, not a fact. [1,2,3,4] has
//      no single middle. Where slow lands depends on where fast starts:
//
//        fast = head          ->  slow ends on 3   the SECOND middle
//        fast = head.next     ->  slow ends on 2   the FIRST middle
//
//      Both are defensible. The problem statement has to say which, and this
//      file implements the second-middle version (the LeetCode convention).
//
// WHEN NOT TO USE IT
//
// When you need the cycle's START node or its LENGTH, this on its own only
// tells you a cycle EXISTS. Getting the entry point needs a second phase —
// that is Floyd's full algorithm, node 08. When random access is available an
// index-based version is simpler and faster. And if O(n) memory is free, a
// HashSet of visited nodes is easier to write and easier to read; the whole
// case for fast/slow is the O(1) space.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run FastSlowPointers.cs                everything
//   dotnet run FastSlowPointers.cs -- trace       one small input, step by step
//   dotnet run FastSlowPointers.cs -- test        the edge cases
//   dotnet run FastSlowPointers.cs -- compare     brute force vs this
//   dotnet run FastSlowPointers.cs -- bench       steps and milliseconds
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

    // -------------------------------------------------------------------------
    // Job 1: the middle, with the gap GROWING.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  JOB 1 — FIND THE MIDDLE, in one pass");
    Console.WriteLine();

    Node list = Lists.Build([1, 2, 3, 4, 5, 6, 7]);
    Console.WriteLine($"    list: {Lists.Show(list)}");
    Console.WriteLine();
    Console.WriteLine("  BRUTE FORCE — pass 1 counts, pass 2 walks to length/2");
    Console.WriteLine();

    int count = 0;
    for (Node? p = list; p != null; p = p.Next) count++;
    Console.WriteLine($"    pass 1: walked {count} nodes to learn the length is {count}");
    Console.WriteLine($"    pass 2: walk {count / 2} more to reach the middle");
    Console.WriteLine($"    total: {count + count / 2} node visits, and you must store the length");
    Console.WriteLine();

    Console.WriteLine("  FAST / SLOW — the gap does the counting");
    Console.WriteLine();
    Console.WriteLine("    step   slow   fast   gap   the list, s = slow, f = fast");
    Console.WriteLine("    ----   ----   ----   ---   ---------------------------------");

    Node? slow = list, fast = list;
    int st = 0;
    while (true)
    {
        // Draw the list with markers under the two pointers.
        var marks = new List<string>();
        for (Node? p = list; p != null; p = p.Next)
            marks.Add(p == slow && p == fast ? "sf" : p == slow ? "s " : p == fast ? " f" : "  ");

        int gap = 0;
        for (Node? p = slow; p != null && p != fast; p = p.Next) gap++;

        Console.WriteLine($"    {st,4}   {slow!.Value,4}   {(fast == null ? "end" : fast.Value.ToString()),4}   {gap,3}   {string.Join(" ", marks)}");

        if (fast == null || fast.Next == null) break;
        slow = slow.Next; fast = fast.Next!.Next; st++;
    }

    Console.WriteLine();
    Console.WriteLine($"    fast ran out after {st} steps; slow is at {slow!.Value} — the middle of 7.");
    Console.WriteLine($"    {st * 3} node visits in ONE pass, no length stored.");
    Console.WriteLine();

    Console.WriteLine("""
      WHY IT LANDS ON THE MIDDLE

      After k steps fast has travelled 2k and slow has travelled k. Slow is
      therefore always at exactly half of fast's position. Let fast run until it
      falls off the end at n, and slow is standing at n/2.

      Nothing was counted. The 2:1 ratio did the arithmetic, which is why one
      pass is enough where the brute force needed the length first.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Job 2: nth from the end, with the gap FIXED.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  JOB 2 — NTH FROM THE END, with a FIXED gap");
    Console.WriteLine();

    Node l2 = Lists.Build([1, 2, 3, 4, 5, 6, 7]);
    int nth = 3;
    Console.WriteLine($"    list: {Lists.Show(l2)}        find the {nth}rd node from the END");
    Console.WriteLine();
    Console.WriteLine($"    phase 1: move fast {nth} ahead, slow stays put");
    Console.WriteLine($"    phase 2: move BOTH one at a time, so the gap stays {nth}");
    Console.WriteLine();
    Console.WriteLine("    phase   slow   fast   gap   the list");
    Console.WriteLine("    -----   ----   ----   ---   ---------------------------------");

    Node? s2 = l2, f2 = l2;
    for (int i = 0; i < nth; i++) f2 = f2!.Next;

    while (true)
    {
        var marks = new List<string>();
        for (Node? p = l2; p != null; p = p.Next)
            marks.Add(p == s2 && p == f2 ? "sf" : p == s2 ? "s " : p == f2 ? " f" : "  ");

        Console.WriteLine($"    {(f2 == null ? "done " : "2    "),-5}   {s2!.Value,4}   {(f2 == null ? "end" : f2.Value.ToString()),4}   {nth,3}   {string.Join(" ", marks)}");

        if (f2 == null) break;
        s2 = s2.Next; f2 = f2.Next;
    }

    Console.WriteLine();
    Console.WriteLine($"    fast hit the end; slow is at {s2!.Value}, which is {nth} from the end. Correct.");
    Console.WriteLine();
    Console.WriteLine("  Here the gap does not grow — it is pinned at n and SLID along. Same");
    Console.WriteLine("  shape, different use of it: job 1 used the RATIO, job 2 uses a fixed");
    Console.WriteLine("  OFFSET. Both replace a counting pass with a second pointer.");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Job 3: the cycle, and why the meeting is forced.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  JOB 3 — CYCLE DETECTION, and why a meeting is FORCED");
    Console.WriteLine();

    Node l3 = Lists.BuildWithCycle([1, 2, 3, 4, 5, 6], cycleTo: 2);
    Console.WriteLine("    list: 1 -> 2 -> 3 -> 4 -> 5 -> 6");
    Console.WriteLine("                    ^              |");
    Console.WriteLine("                    +--------------+    (6 points back to 3)");
    Console.WriteLine();
    Console.WriteLine("    step   slow   fast   fast gains on slow");
    Console.WriteLine("    ----   ----   ----   -------------------------------");

    Node? s3 = l3, f3 = l3;
    int st3 = 0;
    while (f3 != null && f3.Next != null)
    {
        s3 = s3!.Next; f3 = f3.Next.Next; st3++;

        string note = s3 == f3 ? "MET — cycle confirmed" : "gap shrank by 1";
        Console.WriteLine($"    {st3,4}   {s3!.Value,4}   {f3!.Value,4}   {note}");

        if (s3 == f3) break;
    }

    Console.WriteLine();
    Console.WriteLine($"    met at node {s3!.Value} after {st3} steps, using O(1) memory.");
    Console.WriteLine();

    Console.WriteLine("""
      WHY THEY CANNOT MISS EACH OTHER

      Once both pointers are inside the loop, measure the distance from fast
      forward around the loop to slow. Fast moves 2 and slow moves 1, so fast
      closes that distance by exactly 1 every step:

          gap = 4, 3, 2, 1, 0

      A quantity that decreases by exactly one per step cannot step over zero.
      So a meeting is not likely, it is guaranteed.

      This is why the speeds are 1 and 2 and not something else. At speeds 1 and
      3 the gap shrinks by 2 per step and CAN jump from 1 straight past 0 to -1,
      so in a loop of even length the two may circle forever without ever
      landing on the same node.

      And if there is no cycle, fast just falls off the end — which is what the
      loop condition tests.
      """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The even-length ambiguity, shown rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  \"THE MIDDLE\" OF AN EVEN LIST IS A CHOICE, SHOWN");
    Console.WriteLine();

    Node l4 = Lists.Build([1, 2, 3, 4]);
    Console.WriteLine($"    list: {Lists.Show(l4)}   — there is no single middle");
    Console.WriteLine();

    // fast = head -> second middle
    Node? sA = l4, fA = l4;
    while (fA != null && fA.Next != null) { sA = sA!.Next; fA = fA.Next.Next; }

    // fast = head.next -> first middle
    Node? sB = l4, fB = l4.Next;
    while (fB != null && fB.Next != null) { sB = sB!.Next; fB = fB.Next.Next; }

    Console.WriteLine($"    fast starts at head        -> slow lands on {sA!.Value}   the SECOND middle");
    Console.WriteLine($"    fast starts at head.Next   -> slow lands on {sB!.Value}   the FIRST middle");
    Console.WriteLine();
    Console.WriteLine("  One line of difference, two different answers, both defensible. This");
    Console.WriteLine("  file implements the second-middle version, which is the convention most");
    Console.WriteLine("  problems expect — but it is the problem statement's job to say, and");
    Console.WriteLine("  \"which middle do you want?\" is a good question to ask out loud.");
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

    int passed = 0, total = 0;

    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  FindMiddle — second-middle convention");
    Console.WriteLine();
    Console.WriteLine("    case                      list                      expect   brute   fast/slow");
    Console.WriteLine("    -----------------------   -----------------------   ------   -----   ---------");

    void CheckMid(string name, int[] values, int expect)
    {
        total++;
        int brute = Lists.MiddleBrute(Lists.Build(values));
        int fs = Lists.MiddleFastSlow(Lists.Build(values));
        bool ok = brute == expect && fs == expect;
        if (ok) passed++;

        string shown = $"[{string.Join(",", values)}]";
        if (shown.Length > 23) shown = shown[..20] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-23}   {expect,6}   {brute,5}   {fs,9}");
    }

    CheckMid("single", [1], 1);
    CheckMid("two (second mid)", [1, 2], 2);
    CheckMid("three", [1, 2, 3], 2);
    CheckMid("four (second mid)", [1, 2, 3, 4], 3);
    CheckMid("five", [1, 2, 3, 4, 5], 3);
    CheckMid("six (second mid)", [1, 2, 3, 4, 5, 6], 4);
    CheckMid("seven", [1, 2, 3, 4, 5, 6, 7], 4);
    CheckMid("all identical", [9, 9, 9], 9);

    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  NthFromEnd");
    Console.WriteLine();
    Console.WriteLine("    case                      list                      n    expect   brute   fast/slow");
    Console.WriteLine("    -----------------------   -----------------------   --   ------   -----   ---------");

    void CheckNth(string name, int[] values, int n, int expect)
    {
        total++;
        int brute = Lists.NthFromEndBrute(Lists.Build(values), n);
        int fs = Lists.NthFromEndFastSlow(Lists.Build(values), n);
        bool ok = brute == expect && fs == expect;
        if (ok) passed++;

        string shown = $"[{string.Join(",", values)}]";
        if (shown.Length > 23) shown = shown[..20] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-23}   {n,2}   {expect,6}   {brute,5}   {fs,9}");
    }

    CheckNth("last (n=1)", [1, 2, 3, 4, 5], 1, 5);
    CheckNth("first (n=len)", [1, 2, 3, 4, 5], 5, 1);
    CheckNth("middle", [1, 2, 3, 4, 5], 3, 3);
    CheckNth("single, n=1", [7], 1, 7);
    CheckNth("n too big", [1, 2, 3], 9, -1);
    CheckNth("n = 0", [1, 2, 3], 0, -1);

    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  HasCycle");
    Console.WriteLine();
    Console.WriteLine("    case                      list                      cycle to   expect   hashset   fast/slow");
    Console.WriteLine("    -----------------------   -----------------------   --------   ------   -------   ---------");

    void CheckCycle(string name, int[] values, int cycleTo, bool expect)
    {
        total++;
        bool hs = Lists.HasCycleHashSet(Lists.BuildWithCycle(values, cycleTo));
        bool fs = Lists.HasCycleFastSlow(Lists.BuildWithCycle(values, cycleTo));
        bool ok = hs == expect && fs == expect;
        if (ok) passed++;

        string shown = $"[{string.Join(",", values)}]";
        if (shown.Length > 23) shown = shown[..20] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {shown,-23}   {(cycleTo < 0 ? "none" : cycleTo.ToString()),8}   {expect,6}   {hs,7}   {fs,9}");
    }

    CheckCycle("no cycle", [1, 2, 3, 4, 5], -1, false);
    CheckCycle("single, no cycle", [1], -1, false);
    CheckCycle("single, self loop", [1], 0, true);
    CheckCycle("two, no cycle", [1, 2], -1, false);
    CheckCycle("two, loop back", [1, 2], 0, true);
    CheckCycle("whole list loops", [1, 2, 3, 4], 0, true);
    CheckCycle("tail loops to mid", [1, 2, 3, 4, 5, 6], 2, true);
    CheckCycle("last node self loop", [1, 2, 3], 2, true);
    CheckCycle("even loop length", [1, 2, 3, 4, 5, 6], 2, true);

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
          Three cases earn their place.

          "even loop length" is the one that would expose wrong speeds. With
          fast moving 3 instead of 2 the gap shrinks by 2 per step and can step
          over zero, so an even-length loop may never produce a meeting. At
          speeds 1 and 2 the gap shrinks by exactly 1 and a meeting is forced.

          "single, self loop" is the smallest possible cycle, and it is the case
          that catches a loop condition checked in the wrong order.

          "n too big" and "n = 0" are the NthFromEnd guards. A fast pointer
          advanced past the end must be detected during phase 1, not dereferenced
          in phase 2.
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
    Console.WriteLine("  FindMiddle — steps are node visits. Both are O(n): this is a PASS-COUNT");
    Console.WriteLine("  and MEMORY win, not a complexity win.");
    Console.WriteLine();
    Console.WriteLine("        n   brute (2 passes)   fast/slow (1 pass)        ratio");
    Console.WriteLine("    -----   ----------------   ------------------   ----------");

    foreach (int n in (int[])[100, 1_000, 10_000, 100_000])
    {
        int[] values = new int[n];
        for (int i = 0; i < n; i++) values[i] = i;

        Lists.Steps = 0;
        Lists.MiddleBrute(Lists.Build(values));
        long brute = Lists.Steps;

        Lists.Steps = 0;
        Lists.MiddleFastSlow(Lists.Build(values));
        long fs = Lists.Steps;

        Console.WriteLine($"    {n,5}   {brute,16:N0}   {fs,18:N0}   {(double)brute / fs,9:N2}x");
    }

    Console.WriteLine();
    Console.WriteLine("  HasCycle — steps are node visits. Here the win is MEMORY.");
    Console.WriteLine();
    Console.WriteLine("        n   hashset steps   hashset MEMORY   fast/slow steps   fast/slow MEMORY");
    Console.WriteLine("    -----   -------------   --------------   ---------------   ----------------");

    foreach (int n in (int[])[100, 1_000, 10_000, 100_000])
    {
        int[] values = new int[n];
        for (int i = 0; i < n; i++) values[i] = i;

        Lists.Steps = 0;
        Lists.HasCycleHashSet(Lists.Build(values));
        long hs = Lists.Steps;

        Lists.Steps = 0;
        Lists.HasCycleFastSlow(Lists.Build(values));
        long fs = Lists.Steps;

        Console.WriteLine($"    {n,5}   {hs,13:N0}   {$"{n} refs",14}   {fs,15:N0}   {"2 refs",16}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Neither table shows a complexity win, and that is the honest reading. Both
      approaches are O(n) in time everywhere. What changes:

        FindMiddle    two passes -> one. A ~1.5x constant, plus you no longer
                      have to store or trust a length. On a stream you cannot
                      rewind, two passes are not available at all.

        HasCycle      O(n) memory -> O(1). At n = 100,000 that is 100,000 object
                      references and a hash of every one of them, versus two
                      local variables. THAT is the win, and it does not appear
                      in a step count at all.

      Which is the point worth taking from this node: a two-pointer technique
      usually buys passes or memory rather than an exponent. ConvergingPointers
      (01) is the exception — there the sortedness turns n^2 into n.
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
    Console.WriteLine("  HasCycle on an acyclic list — the worst case, since it must walk to");
    Console.WriteLine("  the end before it can answer 'no'.");
    Console.WriteLine();
    Console.WriteLine("            hashset                       fast / slow");
    Console.WriteLine("        n          steps       ms            steps       ms");
    Console.WriteLine("    -----   ------------   ------     ------------   ------");

    foreach (int n in (int[])[1_000, 10_000, 100_000, 1_000_000])
    {
        int[] values = new int[n];
        for (int i = 0; i < n; i++) values[i] = i;
        Node head = Lists.Build(values);

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Lists.HasCycleHashSet(head);
        Lists.HasCycleFastSlow(head);

        Lists.Steps = 0;
        var sw = Stopwatch.StartNew();
        Lists.HasCycleHashSet(head);
        double hsMs = sw.Elapsed.TotalMilliseconds;
        long hsSteps = Lists.Steps;

        Lists.Steps = 0;
        sw.Restart();
        Lists.HasCycleFastSlow(head);
        double fsMs = sw.Elapsed.TotalMilliseconds;
        long fsSteps = Lists.Steps;

        Console.WriteLine($"    {n,5}   {hsSteps,12:N0}   {hsMs,6:N2}     {fsSteps,12:N0}   {fsMs,6:N2}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Same O(n) steps, very different milliseconds. The hash-set version pays
      GetHashCode plus a bucket probe on every node, and allocates a table that
      grows to n entries. The fast/slow version does two pointer dereferences
      and nothing else.

      This is the clearest case in the repo of identical Big-O with a large real
      gap — complexity counts operations, not what they cost on hardware.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
      """);
    Console.WriteLine();
}


// =============================================================================
// THE NODE
//
// A minimal singly linked node, defined here so the file stays self-contained.
// The data structure itself is covered in 01-DataStructures/02-LinkedLists.
// =============================================================================

class Node
{
    public int Value;
    public Node? Next;
    public Node(int value) => Value = value;
}


// =============================================================================
// THE IMPLEMENTATIONS
//
// Each job has a brute force and a fast/slow version. Steps counts NODE VISITS
// so the two are measured in the same unit.
// =============================================================================

static class Lists
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // BUILDERS — not part of the algorithm, just scaffolding for the demos.
    // -------------------------------------------------------------------------
    public static Node Build(int[] values)
    {
        if (values.Length == 0) throw new ArgumentException("need at least one node", nameof(values));

        Node head = new(values[0]);
        Node tail = head;
        for (int i = 1; i < values.Length; i++) { tail.Next = new Node(values[i]); tail = tail.Next; }
        return head;
    }

    // cycleTo < 0 builds an acyclic list; otherwise the last node points back
    // to the node at that index.
    public static Node BuildWithCycle(int[] values, int cycleTo)
    {
        Node head = Build(values);
        if (cycleTo < 0) return head;

        Node tail = head, target = head;
        while (tail.Next != null) tail = tail.Next;
        for (int i = 0; i < cycleTo; i++) target = target.Next!;
        tail.Next = target;
        return head;
    }

    public static string Show(Node head)
    {
        var parts = new List<string>();
        for (Node? p = head; p != null; p = p.Next) parts.Add(p.Value.ToString());
        return string.Join(" -> ", parts);
    }

    // -------------------------------------------------------------------------
    // MIDDLE, BRUTE FORCE — O(n) time, TWO passes, O(1) space.
    //
    // Pass 1 counts. Pass 2 walks half that far. Correct, and it needs the
    // length to exist as a number before it can start — which is exactly what
    // a single-pass version avoids.
    // -------------------------------------------------------------------------
    public static int MiddleBrute(Node head)
    {
        int count = 0;
        for (Node? p = head; p != null; p = p.Next) { Steps++; count++; }

        Node walk = head;
        for (int i = 0; i < count / 2; i++) { Steps++; walk = walk.Next!; }

        return walk.Value;
    }

    // -------------------------------------------------------------------------
    // MIDDLE, FAST / SLOW — O(n) time, ONE pass, O(1) space.
    //
    // fast moves 2, slow moves 1, so slow is always at half of fast's position.
    // When fast falls off the end at n, slow is at n/2.
    //
    // fast starts at head, which gives the SECOND middle on even lengths.
    // Starting fast at head.Next gives the first. See -- trace.
    // -------------------------------------------------------------------------
    public static int MiddleFastSlow(Node head)
    {
        Node slow = head, fast = head;

        // Both fast and fast.Next must exist, and in THAT order — fast takes
        // two steps per iteration.
        while (fast.Next != null && fast.Next.Next != null)
        {
            Steps += 3;                      // slow moves 1, fast moves 2
            slow = slow.Next!;
            fast = fast.Next.Next;
        }

        // One more half-step for even lengths, to land on the second middle.
        if (fast.Next != null) { Steps++; slow = slow.Next!; }

        return slow.Value;
    }

    // -------------------------------------------------------------------------
    // NTH FROM END, BRUTE FORCE — O(n) time, TWO passes.
    // -------------------------------------------------------------------------
    public static int NthFromEndBrute(Node head, int n)
    {
        if (n <= 0) return -1;

        int count = 0;
        for (Node? p = head; p != null; p = p.Next) { Steps++; count++; }
        if (n > count) return -1;

        Node walk = head;
        for (int i = 0; i < count - n; i++) { Steps++; walk = walk.Next!; }

        return walk.Value;
    }

    // -------------------------------------------------------------------------
    // NTH FROM END, FAST / SLOW — O(n) time, ONE pass.
    //
    // Phase 1 opens a gap of exactly n. Phase 2 slides that gap along. When
    // fast reaches the end, slow is n behind it.
    //
    // The guard matters: if fast runs out during phase 1 then n is bigger than
    // the list, and that has to be caught before phase 2 dereferences it.
    // -------------------------------------------------------------------------
    public static int NthFromEndFastSlow(Node head, int n)
    {
        if (n <= 0) return -1;

        Node? slow = head, fast = head;

        for (int i = 0; i < n; i++)
        {
            if (fast == null) return -1;     // n is longer than the list
            Steps++;
            fast = fast.Next;
        }

        while (fast != null)
        {
            Steps += 2;
            slow = slow!.Next;
            fast = fast.Next;
        }

        return slow!.Value;
    }

    // -------------------------------------------------------------------------
    // CYCLE, HASH SET — O(n) time, O(n) SPACE.
    //
    // Remember every node seen; a repeat means a loop. Easy to write and easy
    // to read, and the n stored references are the whole reason to prefer the
    // other one.
    //
    // Note the set is of NODES, by reference identity — not of values. Two
    // different nodes can hold the same value without that being a cycle.
    // -------------------------------------------------------------------------
    public static bool HasCycleHashSet(Node head)
    {
        var seen = new HashSet<Node>();

        for (Node? p = head; p != null; p = p.Next)
        {
            Steps++;
            if (!seen.Add(p)) return true;   // Add returned false -> seen before
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // CYCLE, FAST / SLOW — O(n) time, O(1) SPACE.
    //
    // Speeds 1 and 2, so inside a loop fast closes on slow by exactly 1 per
    // step. A gap shrinking by exactly 1 cannot skip zero, so a meeting is
    // forced. Any other speed pair loses that guarantee — see -- trace.
    //
    // The loop condition tests FAST and FAST.NEXT, in that order, because fast
    // takes two steps. Testing slow would be pointless; it is never ahead.
    // -------------------------------------------------------------------------
    public static bool HasCycleFastSlow(Node head)
    {
        Node? slow = head, fast = head;

        while (fast != null && fast.Next != null)
        {
            Steps++;
            slow = slow!.Next;               // 1 step
            fast = fast.Next.Next;           // 2 steps
            if (slow == fast) return true;   // reference equality, not value
        }

        return false;                        // fast fell off the end
    }
}
