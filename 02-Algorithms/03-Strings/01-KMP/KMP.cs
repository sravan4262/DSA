#!/usr/bin/env dotnet
// =============================================================================
// KMP — find a pattern in a text without ever re-reading a character of the
// text. O(n + m) instead of O(n x m).
//
// Node 03 · Strings.  Needs: array.
//
// -----------------------------------------------------------------------------
// WHAT THIS IS
//
// The job: find where pattern P occurs inside text T.
//
//   text     ABABDABABC
//   pattern  ABABC
//   answer   index 5
//
// THE WASTE IN THE BRUTE FORCE — and it is not the comparisons
//
// Brute force tries every starting position. On a mismatch it throws away
// everything it learned and restarts one character later:
//
//   text     A B A B D A B A B C
//   try 0    A B A B C            matched 4, then D != C
//   try 1      A B A B C          starts over from scratch
//
// Look at what it discarded. It had already established that text[0..3] is
// "ABAB". Try 1 then re-reads text[1], text[2], text[3] to learn what it
// already knew. The text pointer went BACKWARDS, and that is the whole cost.
//
// THE IDEA — the pattern knows the answer before it sees the text
//
// When "ABAB" matched and then failed, you know the last 4 characters of the
// text were A B A B. Is there a shorter prefix of the pattern that is still
// alive? "AB" — because the pattern's own first two characters "AB" are also
// its characters at positions 2..3.
//
// That fact is about the PATTERN ONLY. It does not mention the text. So it can
// be computed once, up front, for every prefix length:
//
//   lps[i] = the length of the longest PROPER prefix of P[0..i]
//            that is also a suffix of P[0..i]
//
//   P      A  B  A  B  C
//   lps    0  0  1  2  0
//               ^  ^
//               |  "AB" is both a prefix and a suffix of "ABAB"
//               "A" is both a prefix and a suffix of "ABA"
//
// "Proper" means it cannot be the whole string, or lps would always be i+1.
// That is why lps[0] is always 0.
//
// THE SEARCH — one rule
//
//   match      i++, j++
//   mismatch   j > 0  ->  j = lps[j-1]      the text pointer does NOT move
//              j == 0 ->  i++               nothing matched, slide along
//
// i NEVER DECREASES. Every character of the text is read at most once, which
// is the entire reason this is O(n).
//
// WHY O(n + m) — the amortised argument
//
// The inner "j = lps[j-1]" loop looks like it could run many times per
// character. Count differently:
//
//   j increases by at most 1 per text character  ->  at most n increases
//   j never goes below 0                         ->  at most n decreases
//
// So the fallbacks total at most n across the whole run, regardless of how they
// are distributed. Same shape of argument as the monotonic stack in node 05.
//
// WHEN NOT TO USE IT
//
// For one search over a short text, IndexOf is shorter and usually faster —
// it is vectorised and has no setup. KMP earns its keep when the pattern is
// reused across many texts (the lps is built once), when the data arrives as a
// stream and you cannot back up, or when you need a guaranteed bound rather
// than a good average. For multiple patterns at once, this generalises to
// Aho-Corasick; for an average-case win with hashing, see 02-RabinKarp.
// -----------------------------------------------------------------------------
//
// This one file IS the program. Put a breakpoint anywhere and press F5.
//
//   dotnet run KMP.cs                everything
//   dotnet run KMP.cs -- trace       one small input, step by step
//   dotnet run KMP.cs -- test        the edge cases
//   dotnet run KMP.cs -- compare     brute force vs this, in steps
//   dotnet run KMP.cs -- bench       steps and milliseconds
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

    string text = "ABABDABABC";
    string pattern = "ABABC";

    Console.WriteLine();
    Console.WriteLine($"  text     {text}");
    Console.WriteLine($"  pattern  {pattern}");
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Part 1: build the lps array, narrated. This uses only the pattern.
    // -------------------------------------------------------------------------
    Console.WriteLine("  STEP 1 — BUILD THE LPS ARRAY (the pattern alone, no text)");
    Console.WriteLine();
    Console.WriteLine("    lps[i] = longest proper prefix of P[0..i] that is also a suffix");
    Console.WriteLine();
    Console.WriteLine("     i   P[i]   compared with   result                                       lps[i]");
    Console.WriteLine("     -   ----   -------------   ------------------------------------------   ------");

    int[] lps = new int[pattern.Length];
    int len = 0;
    Console.WriteLine($"     0   {pattern[0],4}   {"-",-13}   {"a single char has no proper prefix",-42}   {0,6}");

    for (int i = 1; i < pattern.Length; i++)
    {
        var notes = new List<string>();

        while (len > 0 && pattern[i] != pattern[len])
        {
            notes.Add($"P[{len}]={pattern[len]} no, fall to lps[{len - 1}]={lps[len - 1]}");
            len = lps[len - 1];
        }

        if (pattern[i] == pattern[len])
        {
            notes.Add($"P[{len}]={pattern[len]} yes");
            len++;
        }
        else
        {
            notes.Add($"P[0]={pattern[0]} no, len already 0");
        }

        lps[i] = len;

        string shown = string.Join("; ", notes);
        if (shown.Length > 42) shown = shown[..39] + "...";
        string slot = $"P[{(len == 0 ? 0 : len - 1)}]";
        Console.WriteLine($"     {i}   {pattern[i],4}   {slot,-13}   {shown,-42}   {lps[i],6}");
    }

    Console.WriteLine();
    Console.Write("    lps = [");
    Console.Write(string.Join(", ", lps));
    Console.WriteLine("]");
    Console.WriteLine();
    Console.WriteLine("""
      READ lps[3] = 2 OUT LOUD: "the first 2 characters of the pattern are also
      the last 2 characters of ABAB". So if the text just matched ABAB and then
      failed, the AB at the end of what you matched is a live candidate for the
      start of a new match. You do not have to go back and re-read it.

      Everything above mentions only the pattern. No text was involved, which is
      why this can be computed once and reused for any number of searches.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Part 2: the search, narrated, with both pointers printed every step.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  STEP 2 — THE SEARCH");
    Console.WriteLine();
    Console.WriteLine("     i   j   T[i]   P[j]   what happens                           i after   j after");
    Console.WriteLine("     -   -   ----   ----   ------------------------------------   -------   -------");

    int ti = 0, pj = 0;
    long comparisons = 0;
    var found = new List<int>();

    while (ti < text.Length)
    {
        int pi = ti, ppj = pj;
        string action;

        if (text[ti] == pattern[pj])
        {
            comparisons++;
            ti++;
            pj++;

            if (pj == pattern.Length)
            {
                found.Add(ti - pj);
                action = $"match, pattern complete at {ti - pj}";
                pj = lps[pj - 1];
            }
            else action = "match, advance both";
        }
        else
        {
            comparisons++;
            if (pj != 0)
            {
                action = $"mismatch, j = lps[{pj - 1}] = {lps[pj - 1]}   (i stays)";
                pj = lps[pj - 1];
            }
            else
            {
                action = "mismatch at j=0, slide i";
                ti++;
            }
        }

        Console.WriteLine($"     {pi}   {ppj}   {text[pi],4}   {pattern[ppj],4}   {action,-36}   {ti,7}   {pj,7}");
    }

    Console.WriteLine();
    Console.WriteLine($"    matches at: {(found.Count == 0 ? "none" : string.Join(", ", found))}");
    Console.WriteLine($"    {comparisons} character comparisons for n={text.Length}, m={pattern.Length}");
    Console.WriteLine();
    Console.WriteLine("""
      LOOK AT THE "i after" COLUMN. It never goes down. Not once.

      Rows 5, 6 and 7 are the interesting ones: i sits at 4 while j falls
      4 -> 2 -> 0. Three rows, one text character. The pattern is sliding
      underneath a fixed text position, and each slide is decided by the lps
      array that was computed before the text was ever read.

      The brute force would have reset i to 1 and re-read text[1], text[2],
      text[3]. KMP does not, because lps already encodes what those characters
      were.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // Part 3: the two classic bugs, demonstrated rather than warned about.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  BUG 1 — ON MISMATCH, USING len-- INSTEAD OF len = lps[len-1]");
    Console.WriteLine();

    string tricky = "ABCABCC";
    Console.WriteLine($"    pattern  {tricky}");
    Console.WriteLine($"    correct  [{string.Join(", ", Kmp.BuildLps(tricky))}]");
    Console.WriteLine($"    len--    [{string.Join(", ", Kmp.BuildLpsBroken(tricky))}]");
    Console.WriteLine();
    Console.WriteLine("""
      The last slot is 3 instead of 0, and the claim it makes is false: "ABC"
      is NOT a suffix of "ABCABCC" — the string ends in CC.

      Why len-- fails: the lps chain lps[len-1], lps[lps[len-1]-1], ... visits
      exactly the lengths that ARE borders. Counting down one at a time visits
      lengths that are not, and the first one whose character happens to match
      is accepted. Here len falls 3 -> 2, P[6]='C' equals P[2]='C', and it stops
      there — but a prefix of length 2 was never a suffix in the first place.

      This one is nasty because it is often right. On most patterns the chain
      and the countdown agree, so a small test suite will not notice.
  """);
    Console.WriteLine();

    Console.WriteLine("  BUG 2 — AFTER A MATCH, SETTING j = 0 INSTEAD OF j = lps[j-1]");
    Console.WriteLine();

    string t2 = "AAAAA", p2 = "AAA";
    Console.WriteLine($"    text     {t2}");
    Console.WriteLine($"    pattern  {p2}");
    Console.WriteLine($"    correct  matches at {string.Join(", ", Kmp.Search(t2, p2))}");
    Console.WriteLine($"    j = 0    matches at {string.Join(", ", Kmp.SearchNoOverlap(t2, p2))}");
    Console.WriteLine();
    Console.WriteLine("""
      Three matches become one. Resetting j to 0 throws away the overlap, so
      the next search starts past the end of the match it just found.

      j = lps[j-1] keeps the longest piece of the match that could still begin
      the next one — which for AAA is AA. Whether you WANT overlapping matches
      is a question for the problem statement, but it should be a decision, not
      an accident.
  """);
    Console.WriteLine();
}


// =============================================================================
// THE TESTS — the cases that have broken a KMP
// =============================================================================

void RunTests()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  THE TESTS");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  PART 1 — the lps array, against a definition-following brute force.");
    Console.WriteLine();
    Console.WriteLine("    case                      pattern                 lps");
    Console.WriteLine("    -----------------------   ---------------------   ---------------------------");

    int passed = 0, total = 0;

    void CheckLps(string name, string pattern)
    {
        total++;
        int[] fast = Kmp.BuildLps(pattern);
        int[] slow = Kmp.BuildLpsBrute(pattern);
        bool ok = fast.SequenceEqual(slow);
        if (ok) passed++;

        string p = pattern.Length > 21 ? pattern[..18] + "..." : pattern;
        string l = "[" + string.Join(",", fast) + "]";
        if (l.Length > 27) l = l[..24] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {p,-21}   {l,-27}");
    }

    CheckLps("single char", "A");
    CheckLps("no repetition", "ABCD");
    CheckLps("all same", "AAAA");
    CheckLps("classic", "ABABC");
    CheckLps("the GfG one", "AABAACAABAA");
    CheckLps("len-- breaker", "ABCABCC");
    CheckLps("period 3", "ABCABCAB");
    CheckLps("nested borders", "AABAAAB");
    CheckLps("two chars alt", "ABABABAB");
    CheckLps("long tail", "AAABAAAB");

    Console.WriteLine();
    Console.WriteLine("  PART 2 — the search, against a brute force that checks every position.");
    Console.WriteLine();
    Console.WriteLine("    case                      text                    pattern       matches");
    Console.WriteLine("    -----------------------   ---------------------   -----------   -------------");

    void CheckSearch(string name, string text, string pattern)
    {
        total++;
        var fast = Kmp.Search(text, pattern);
        var slow = Kmp.SearchBrute(text, pattern);
        bool ok = fast.SequenceEqual(slow);
        if (ok) passed++;

        string t = text.Length > 21 ? text[..18] + "..." : text;
        string p = pattern.Length > 11 ? pattern[..8] + "..." : pattern;
        string m = fast.Count == 0 ? "none" : string.Join(",", fast);
        if (m.Length > 13) m = m[..10] + "...";
        Console.WriteLine($"    {(ok ? "PASS" : "FAIL")}  {name,-18}   {t,-21}   {p,-11}   {m,-13}");
    }

    CheckSearch("no match", "ABCDEF", "XYZ");
    CheckSearch("match at start", "ABCDEF", "ABC");
    CheckSearch("match at end", "ABCDEF", "DEF");
    CheckSearch("match in middle", "ABCDEF", "CD");
    CheckSearch("whole string", "ABC", "ABC");
    CheckSearch("pattern longer", "AB", "ABC");
    CheckSearch("overlapping", "AAAAA", "AAA");
    CheckSearch("all same both", "AAAA", "AA");
    CheckSearch("classic", "ABABDABABC", "ABABC");
    CheckSearch("the GfG one", "ABABDABACDABABCABAB", "ABABCABAB");
    CheckSearch("many matches", "ABABABABAB", "ABAB");
    CheckSearch("near miss", "AAAAAB", "AAAB");
    CheckSearch("single char pat", "ABCABC", "C");
    CheckSearch("repeats + tail", "AABAACAABAACAABAA", "AABAA");

    Console.WriteLine();
    Console.WriteLine($"    {passed}/{total} passed.");
    Console.WriteLine();

    if (passed == total)
    {
        Console.WriteLine("""
      Four of these rows earn their place.

      "len-- breaker" (ABCABCC) is the pattern from -- trace that separates a
      correct lps build from the countdown version. Without it in the list, a
      broken build passes every other row here.

      "overlapping" (AAA in AAAAA) is the one that separates j = lps[j-1] from
      j = 0 after a match. Three matches or one.

      "pattern longer than text" is the row people forget to guard. Both
      implementations return no matches rather than throwing.

      "near miss" (AAAB in AAAAAB) is where the brute force does its worst
      work — it matches three characters and fails on the fourth at almost
      every starting position. See -- compare.
  """);
        Console.WriteLine();
    }
}


// =============================================================================
// SIDE BY SIDE — the same search, both implementations, counted
// =============================================================================

void ShowCompare()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  SIDE BY SIDE");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  The brute force's worst case: a pattern that almost matches everywhere.");
    Console.WriteLine("  text = AAAA...A, pattern = AAA...AB — so every start matches m-1");
    Console.WriteLine("  characters and then fails on the last one.");
    Console.WriteLine();
    Console.WriteLine("          n       m   brute comparisons   KMP comparisons        ratio");
    Console.WriteLine("    -------   -----   -----------------   ---------------   ----------");

    foreach (var (n, m) in (( int, int )[])[(1_000, 10), (10_000, 50), (50_000, 100), (100_000, 200)])
    {
        string text = new string('A', n);
        string pattern = new string('A', m - 1) + "B";

        Kmp.Steps = 0;
        Kmp.SearchBrute(text, pattern);
        long brute = Kmp.Steps;

        Kmp.Steps = 0;
        Kmp.Search(text, pattern);
        long kmp = Kmp.Steps;

        Console.WriteLine($"    {n,7:N0}   {m,5:N0}   {brute,17:N0}   {kmp,15:N0}   {(double)brute / kmp,9:N1}x");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The brute force does about n x m. KMP does about n + m, and the lps build
      is the m part — paid once, before the text is touched.

      Look at the ratio column against m: 10 -> 4.9x, 50 -> 24.8x, 100 -> 49.9x,
      200 -> 99.7x. It is m/2 every time, and it does not depend on n at all.

      That is the shape of the win. KMP is not being clever about the text — it
      beats the brute force by never re-reading a character, and the number of
      re-reads the brute force does is set by the PATTERN length. Double the
      pattern, double the gap. Double the text, nothing changes.
  """);
    Console.WriteLine();

    // -------------------------------------------------------------------------
    // The honest counterweight: the brute force's good case.
    // -------------------------------------------------------------------------
    Console.WriteLine();
    Console.WriteLine("  AND THE ORDINARY CASE — random text, random pattern, nothing adversarial.");
    Console.WriteLine();
    Console.WriteLine("          n       m   brute comparisons   KMP comparisons        ratio");
    Console.WriteLine("    -------   -----   -----------------   ---------------   ----------");

    var rng = new Random(20261009);
    foreach (var (n, m) in (( int, int )[])[(1_000, 10), (10_000, 50), (50_000, 100), (100_000, 200)])
    {
        char[] buf = new char[n];
        for (int i = 0; i < n; i++) buf[i] = (char)('a' + rng.Next(26));
        string text = new string(buf);
        string pattern = new string(buf, n / 2, m);        // guarantee one match

        Kmp.Steps = 0;
        Kmp.SearchBrute(text, pattern);
        long brute = Kmp.Steps;

        Kmp.Steps = 0;
        Kmp.Search(text, pattern);
        long kmp = Kmp.Steps;

        string ratio = brute >= kmp ? $"{(double)brute / kmp,9:N2}x" : $"1/{(double)kmp / brute,-7:N2}";
        Console.WriteLine($"    {n,7:N0}   {m,5:N0}   {brute,17:N0}   {kmp,15:N0}   {ratio,10}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      Read the ratio column again: the brute force WINS every row. Not by much
      — about 1% — but it wins, consistently, at every size.

      Why: over a 26-letter alphabet a mismatch almost always happens on the
      first character, so the brute force basically never backs up, and it does
      not pay for an lps array it never uses. Its bad case needs a text that
      keeps nearly-matching, and random data does not produce one.

      So the honest claim is not "KMP is faster" — on ordinary text it is
      slightly slower. The claim is "KMP has no bad case". That is worth
      nothing until the input is chosen by someone who wants you to be slow.
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
    Console.WriteLine("  The adversarial input, plus string.IndexOf for scale.");
    Console.WriteLine();
    Console.WriteLine("                     brute force              KMP        IndexOf");
    Console.WriteLine("          n       m        steps       ms        steps       ms         ms");
    Console.WriteLine("    -------   -----   ----------   ------   ----------   ------   --------");

    foreach (var (n, m) in (( int, int )[])[(10_000, 50), (50_000, 100), (200_000, 200)])
    {
        string text = new string('A', n);
        string pattern = new string('A', m - 1) + "B";

        // Warm up the JIT first, or the first row measures compilation rather
        // than the algorithm. See HashMapCounting.cs for the measurement.
        Kmp.SearchBrute(text[..100], pattern);
        Kmp.Search(text[..100], pattern);
        text.IndexOf(pattern, StringComparison.Ordinal);

        Kmp.Steps = 0;
        var sw = Stopwatch.StartNew();
        Kmp.SearchBrute(text, pattern);
        double bruteMs = sw.Elapsed.TotalMilliseconds;
        long bruteSteps = Kmp.Steps;

        Kmp.Steps = 0;
        sw.Restart();
        Kmp.Search(text, pattern);
        double kmpMs = sw.Elapsed.TotalMilliseconds;
        long kmpSteps = Kmp.Steps;

        sw.Restart();
        text.IndexOf(pattern, StringComparison.Ordinal);
        double netMs = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine($"    {n,7:N0}   {m,5:N0}   {bruteSteps,10:N0}   {bruteMs,6:N2}   {kmpSteps,10:N0}   {kmpMs,6:N2}   {netMs,8:N3}");
    }

    Console.WriteLine();
    Console.WriteLine("""
      The IndexOf column is the one to be humbled by. On the last row it is
      about 60x faster than KMP — on KMP's own best input, the one chosen to
      make the brute force look terrible.

      .NET's implementation compares many bytes per instruction (SIMD), so it
      does far fewer actual operations per character even though it does more
      character comparisons on paper. Big-O counts comparisons; the hardware
      does not charge per comparison. A worse algorithm with a better constant
      wins at these sizes, and it is not close.

      That is not an argument against knowing KMP. It is an argument for
      calling IndexOf in production and knowing why KMP exists: a guaranteed
      bound, a reusable preprocessed pattern, and the ability to run over a
      stream you cannot rewind.

      Steps are deterministic — rerun this and they are identical, so reason with
      those. Milliseconds move with the machine, the JIT and whatever else is
      running, so treat them as a sanity check rather than a measurement.
  """);
    Console.WriteLine();
}


// =============================================================================
// THE IMPLEMENTATIONS
//
// BuildLps and Search are the real ones. BuildLpsBrute and SearchBrute follow
// the definitions literally and exist so the tests can check against something
// obviously correct rather than against an assertion.
//
// BuildLpsBroken and SearchNoOverlap are WRONG ON PURPOSE and are kept so
// -- trace can print the failure next to the correct answer.
//
// Steps counts CHARACTER COMPARISONS, which is the unit both the O(n x m) and
// the O(n + m) claims are stated in.
// =============================================================================

static class Kmp
{
    // The step counter. Public and static so the report methods above can read
    // it straight after a call, with no plumbing in the signatures.
    public static long Steps;

    // -------------------------------------------------------------------------
    // THE LPS ARRAY — O(m) time, O(m) space. The pattern only; no text.
    //
    // lps[i] = length of the longest proper prefix of P[0..i] that is also a
    // suffix of P[0..i]. lps[0] is always 0, because the only prefix of a
    // 1-character string that is also a suffix is the string itself, and
    // "proper" excludes that.
    //
    // This is KMP run against the pattern and itself, which is why `len` plays
    // the same role here that `j` plays in Search.
    // -------------------------------------------------------------------------
    public static int[] BuildLps(string pattern)
    {
        int[] lps = new int[pattern.Length];
        if (pattern.Length == 0) return lps;

        int len = 0;                                 // length of the current border

        for (int i = 1; i < pattern.Length; i++)
        {
            // Fall back along the chain of borders. lps[len-1] is the next
            // shorter border — NOT len-1, which is usually not a border at all.
            while (len > 0 && pattern[i] != pattern[len])
            {
                Steps++;
                len = lps[len - 1];
            }

            Steps++;
            if (pattern[i] == pattern[len]) len++;

            lps[i] = len;
        }

        return lps;
    }

    // -------------------------------------------------------------------------
    // BROKEN ON PURPOSE — len-- instead of len = lps[len-1].
    //
    // Counting down visits lengths that are not borders. The first one whose
    // character happens to match is accepted, and the resulting lps value
    // claims a border that does not exist. ABCABCC gives 3 instead of 0.
    // -------------------------------------------------------------------------
    public static int[] BuildLpsBroken(string pattern)
    {
        int[] lps = new int[pattern.Length];
        if (pattern.Length == 0) return lps;

        int len = 0;

        for (int i = 1; i < pattern.Length; i++)
        {
            while (len > 0 && pattern[i] != pattern[len]) len--;      // the bug
            if (pattern[i] == pattern[len]) len++;
            lps[i] = len;
        }

        return lps;
    }

    // -------------------------------------------------------------------------
    // THE DEFINITION, LITERALLY — O(m^2) or worse. The tests check against this.
    //
    // For each prefix, try every proper length from longest down and compare the
    // prefix with the suffix directly.
    // -------------------------------------------------------------------------
    public static int[] BuildLpsBrute(string pattern)
    {
        int[] lps = new int[pattern.Length];

        for (int i = 0; i < pattern.Length; i++)
        {
            string s = pattern[..(i + 1)];

            for (int k = s.Length - 1; k >= 1; k--)        // k = candidate length
            {
                if (s[..k] == s[^k..]) { lps[i] = k; break; }
            }
        }

        return lps;
    }

    // -------------------------------------------------------------------------
    // THE SEARCH — O(n + m) time, O(m) space.
    //
    // i only ever increases. On a mismatch the PATTERN slides; the text does
    // not rewind. That is the whole algorithm.
    //
    // Returns every match, including overlapping ones.
    // -------------------------------------------------------------------------
    public static List<int> Search(string text, string pattern)
    {
        var matches = new List<int>();
        if (pattern.Length == 0 || pattern.Length > text.Length) return matches;

        int[] lps = BuildLps(pattern);
        int i = 0, j = 0;

        while (i < text.Length)
        {
            Steps++;

            if (text[i] == pattern[j])
            {
                i++;
                j++;

                if (j == pattern.Length)
                {
                    matches.Add(i - j);
                    j = lps[j - 1];              // keep the overlap, do NOT zero it
                }
            }
            else if (j != 0)
            {
                j = lps[j - 1];                  // i deliberately does not move
            }
            else
            {
                i++;
            }
        }

        return matches;
    }

    // -------------------------------------------------------------------------
    // BROKEN ON PURPOSE — j = 0 after a match.
    //
    // Throws away the overlap, so AAA in AAAAA reports one match instead of
    // three. Whether overlaps count is a question for the problem statement;
    // this version answers it by accident.
    // -------------------------------------------------------------------------
    public static List<int> SearchNoOverlap(string text, string pattern)
    {
        var matches = new List<int>();
        if (pattern.Length == 0 || pattern.Length > text.Length) return matches;

        int[] lps = BuildLps(pattern);
        int i = 0, j = 0;

        while (i < text.Length)
        {
            if (text[i] == pattern[j])
            {
                i++;
                j++;
                if (j == pattern.Length) { matches.Add(i - j); j = 0; }   // the bug
            }
            else if (j != 0) j = lps[j - 1];
            else i++;
        }

        return matches;
    }

    // -------------------------------------------------------------------------
    // BRUTE FORCE — O(n x m) worst case, O(1) space.
    //
    // Try every starting position. On a mismatch, throw away everything and
    // restart one character later — the text pointer goes backwards.
    // -------------------------------------------------------------------------
    public static List<int> SearchBrute(string text, string pattern)
    {
        var matches = new List<int>();
        if (pattern.Length == 0 || pattern.Length > text.Length) return matches;

        for (int start = 0; start + pattern.Length <= text.Length; start++)
        {
            int k = 0;
            while (k < pattern.Length)
            {
                Steps++;
                if (text[start + k] != pattern[k]) break;
                k++;
            }

            if (k == pattern.Length) matches.Add(start);
        }

        return matches;
    }
}
