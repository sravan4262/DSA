#!/usr/bin/env dotnet
// =============================================================================
// TRIES — a tree where the PATH spells the word, so shared prefixes are stored
// exactly once and lookup cost depends on the word, never on how many words.
//
//   dotnet run Tries.cs                  everything
//   dotnet run Tries.cs -- memory        how it sits in memory
//   dotnet run Tries.cs -- ops           each operation, and what it cost
//   dotnet run Tries.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowLookupCost();
    ShowPrefixSharingCost();
}


// =============================================================================
// HOW A TRIE IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A TRIE IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      The letters are NOT stored in the nodes. The letter is the EDGE you take,
      and a node's position in the tree is what identifies it:

        insert "car", "cart", "cat", "dog"

                       (root)
                      /      \
                    c          d
                    │          │
                    a          o
                  /   \        │
                r       t*     g*
                │
                *  \
                    t*             * = "a word ends here"

      Walking c-a-r spells "car". Walking c-a-r-t spells "cart" and REUSES every
      node of "car" — the shared prefix is stored once, not twice. That is the
      entire point.

      COST OF ONE NODE. Each node needs a slot per possible next letter:

        object header                 16 bytes
        children array reference       8 bytes
        isEndOfWord bool + padding     8 bytes
        --------------------------- ---------
        the node itself               32 bytes

        the children array (26 refs)  24 + 26*8 = 232 bytes

        total per node               ~264 bytes

      That is enormous for one letter. A trie is FAST but very memory-hungry —
      the opposite trade from an array. Storing 1000 short words can easily cost
      more than storing them as plain strings.

      Two common fixes:
        - a Dictionary<char, Node> instead of a 26-slot array: much smaller when
          nodes are sparse (most are), slightly slower per hop
        - compressing chains of single-child nodes into one (a radix tree)
      """);

    Console.WriteLine();
    Console.WriteLine("  WATCH PREFIXES BE SHARED — nodes created per insert:");
    Console.WriteLine();

    Tries demo = new Tries();
    string[] words = ["car", "cart", "cat", "dog", "do"];
    for (int i = 0; i < words.Length; i++)
    {
        int before = demo.nodeCount;
        demo.Insert(words[i]);
        int created = demo.nodeCount - before;
        Console.WriteLine($"    Insert(\"{words[i]}\")".PadRight(24)
            + $"created {created} new node(s), {demo.nodeCount} total"
            + (created < words[i].Length ? $"   ({words[i].Length - created} reused)" : ""));
    }

    Console.WriteLine();
    Console.WriteLine("  \"cart\" only cost ONE node — c, a and r already existed. \"do\" cost");
    Console.WriteLine("  nothing at all, because the path already existed for \"dog\"; it just");
    Console.WriteLine("  had to be marked as a word ending.");
    Console.WriteLine();
    Console.WriteLine($"  {demo.nodeCount} nodes for {words.Length} words totalling "
        + $"{words[0].Length + words[1].Length + words[2].Length + words[3].Length + words[4].Length} letters.");
}


// =============================================================================
// EVERY OPERATION, AND WHAT IT COST
// =============================================================================

void ShowOperations()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  EVERY OPERATION, AND WHAT IT COST");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  `steps` counts every node hop — one per letter.");
    Console.WriteLine();

    Tries trie = new Tries();
    long before;

    before = trie.steps;
    string[] words = ["car", "cart", "cat", "care", "dog", "do", "done"];
    for (int i = 0; i < words.Length; i++) trie.Insert(words[i]);
    Console.WriteLine($"    Insert x7                               cost {trie.steps - before,6} step(s)");
    Console.WriteLine($"          words={trie.wordCount} nodes={trie.nodeCount}");
    Console.WriteLine();

    before = trie.steps;
    bool has = trie.Contains("cart");
    Console.WriteLine($"    Contains(\"cart\") = {has}                  cost {trie.steps - before,6} step(s)");
    Console.WriteLine("          Four hops for a four-letter word. The number of words in the");
    Console.WriteLine("          trie is IRRELEVANT — that is the headline property.");
    Console.WriteLine();

    before = trie.steps;
    bool prefixOnly = trie.Contains("car" + "e" + "s");
    Console.WriteLine($"    Contains(\"cares\") = {prefixOnly}                cost {trie.steps - before,6} step(s)");
    Console.WriteLine("          Ran out of trie after 4 letters and stopped. A miss is often");
    Console.WriteLine("          CHEAPER than a hit — it fails as soon as the path breaks.");
    Console.WriteLine();

    before = trie.steps;
    bool isWord = trie.Contains("ca");
    Console.WriteLine($"    Contains(\"ca\") = {isWord}                   cost {trie.steps - before,6} step(s)");
    Console.WriteLine("          The path EXISTS but was never marked as a word ending. This is");
    Console.WriteLine("          why nodes carry isEndOfWord — \"ca\" is a prefix, not a word.");
    Console.WriteLine();

    before = trie.steps;
    bool startsWith = trie.StartsWith("car");
    Console.WriteLine($"    StartsWith(\"car\") = {startsWith}              cost {trie.steps - before,6} step(s)");
    Console.WriteLine("          Same walk, without the isEndOfWord check. This is the operation");
    Console.WriteLine("          a hash table simply cannot do — it can find a key, but it has no");
    Console.WriteLine("          idea which keys share a prefix.");
    Console.WriteLine();

    before = trie.steps;
    string found = trie.WordsWithPrefix("ca");
    Console.WriteLine($"    WordsWithPrefix(\"ca\")                   cost {trie.steps - before,6} step(s)");
    Console.WriteLine($"          {found}");
    Console.WriteLine("          Walk to the prefix node, then collect everything below it. This");
    Console.WriteLine("          is autocomplete, and it is why tries exist.");
}


// =============================================================================
// COMPLEXITY — one method per property, each measuring itself
// =============================================================================

void ShowLookupCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Contains(word) as the DICTIONARY grows");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Same 8-letter word looked up in tries holding n words.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Tries trie = new Tries();
        for (int i = 0; i < sizes[s]; i++)
        {
            // Distinct 8-letter words built from the index.
            string word = "";
            int n = i;
            for (int c = 0; c < 8; c++)
            {
                word += (char)('a' + n % 26);
                n /= 26;
            }
            trie.Insert(word);
        }

        trie.steps = 0;
        trie.Contains("aaaaaaaa");
        work[s] = trie.steps;
        if (work[s] > max) max = work[s];
    }

    Console.WriteLine("         n         work    ratio   reading");
    for (int s = 0; s < sizes.Length; s++)
    {
        string ratio = "   -  ";
        string reading = "";
        if (s > 0 && work[s - 1] > 0)
        {
            double r = (double)work[s] / work[s - 1];
            ratio = r.ToString("0.00").PadLeft(6);
            reading = r < 1.02 ? "flat        -> O(1) in n"
                    : r < 1.5 ? "creeping    -> O(log n)"
                    : r < 2.4 ? "doubling    -> O(n)"
                    : r < 3.2 ? "doubling+   -> O(n log n)"
                              : "quadrupling -> O(n^2)";
        }
        Console.WriteLine($"   {sizes[s],7}{work[s],13}{ratio}   {reading}");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * work[s] / max);
        Console.WriteLine($"   n={sizes[s],-7}{new string('#', width)}");
    }

    Console.WriteLine();
    Console.WriteLine("  Perfectly flat — 8 hops for an 8-letter word whether the trie holds a");
    Console.WriteLine("  thousand words or a million. Lookup is O(m), the LENGTH OF THE KEY, and");
    Console.WriteLine("  n does not appear in the complexity at all.");
    Console.WriteLine();
    Console.WriteLine("  A balanced BST would be O(m log n) here — log n comparisons, each");
    Console.WriteLine("  comparing up to m characters. The trie drops the log n entirely.");
}

void ShowPrefixSharingCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  MEMORY — what prefix sharing actually saves");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];

    Console.WriteLine("         n      letters        nodes   nodes/letter   shared away");
    for (int s = 0; s < sizes.Length; s++)
    {
        Tries trie = new Tries();
        long letters = 0;

        for (int i = 0; i < sizes[s]; i++)
        {
            string word = "";
            int n = i;
            for (int c = 0; c < 8; c++)
            {
                word += (char)('a' + n % 26);
                n /= 26;
            }
            trie.Insert(word);
            letters += word.Length;
        }

        double perLetter = (double)trie.nodeCount / letters;
        double saved = 100.0 * (1.0 - perLetter);
        Console.WriteLine($"   {sizes[s],7}{letters,13}{trie.nodeCount,13}{perLetter,15:0.00}{saved,13:0}%");
    }

    Console.WriteLine();
    Console.WriteLine("  Every word here is 8 letters, so a naive store would need one node per");
    Console.WriteLine("  letter. The nodes/letter column shows how much the shared prefixes save");
    Console.WriteLine("  — and it IMPROVES as the dictionary grows, because more words collide");
    Console.WriteLine("  on early letters.");
    Console.WriteLine();
    Console.WriteLine("  But remember the constant: each node is ~264 bytes with a 26-slot child");
    Console.WriteLine("  array. Sharing prefixes is a real saving on a bad baseline — a trie is");
    Console.WriteLine("  still far heavier than storing the strings in a hash set.");
    Console.WriteLine();
    Console.WriteLine("  You pay that memory for two things a hash set cannot do: prefix queries,");
    Console.WriteLine("  and lookup cost independent of the dictionary size.");
    Console.WriteLine();
}


// =============================================================================
// THE NODE
//
// The letter is not stored — it is the INDEX into the parent's children array.
// =============================================================================

public class TrieNode
{
    // One slot per possible next letter. Almost all of these are null, which is
    // exactly where the memory goes.
    public TrieNode?[] children = new TrieNode?[26];

    // Without this, "ca" and "car" are indistinguishable — every prefix would
    // look like a word.
    public bool isEndOfWord = false;
}


// =============================================================================
// THE CLASS
//
// A lowercase a-z trie. Every operation carries its own walking loop.
// =============================================================================

public class Tries
{
    public TrieNode root = new TrieNode();
    public int wordCount = 0;
    public int nodeCount = 1;      // the root itself
    public long steps = 0;

    // -------------------------------------------------------------------------
    // Insert — add a word.
    //
    // O(m) where m is the word LENGTH — the number of words already stored does
    // not matter. Walk letter by letter, creating nodes only where the path does
    // not already exist, which is how prefixes get shared.
    // -------------------------------------------------------------------------
    public void Insert(string word)
    {
        TrieNode walk = root;

        for (int i = 0; i < word.Length; i++)
        {
            int slot = word[i] - 'a';
            steps++;

            if (walk.children[slot] == null)
            {
                walk.children[slot] = new TrieNode();
                nodeCount++;
            }

            // If it already existed, this letter cost NOTHING — that is the
            // shared prefix.
            walk = walk.children[slot]!;
        }

        if (!walk.isEndOfWord)
        {
            walk.isEndOfWord = true;
            wordCount++;
        }
    }

    // -------------------------------------------------------------------------
    // Contains — is this exact word stored?
    //
    // O(m). Note the final isEndOfWord check: the path for "ca" exists once you
    // insert "car", but "ca" was never marked as a word. Without that flag every
    // prefix would falsely report as a word.
    // -------------------------------------------------------------------------
    public bool Contains(string word)
    {
        TrieNode walk = root;

        for (int i = 0; i < word.Length; i++)
        {
            int slot = word[i] - 'a';
            steps++;

            if (walk.children[slot] == null)
            {
                return false;   // the path breaks — fails early, often cheaply
            }

            walk = walk.children[slot]!;
        }

        return walk.isEndOfWord;
    }

    // -------------------------------------------------------------------------
    // StartsWith — does any stored word begin with this?
    //
    // O(m). Identical walk to Contains, minus the isEndOfWord check. This is the
    // operation a hash table cannot do at all: hashing destroys the relationship
    // between "car" and "cart", so a hash set has no idea they share anything.
    // -------------------------------------------------------------------------
    public bool StartsWith(string prefix)
    {
        TrieNode walk = root;

        for (int i = 0; i < prefix.Length; i++)
        {
            int slot = prefix[i] - 'a';
            steps++;

            if (walk.children[slot] == null)
            {
                return false;
            }

            walk = walk.children[slot]!;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // WordsWithPrefix — every stored word beginning with this prefix.
    //
    // O(m + k) where k is the size of the subtree below the prefix. Walk to the
    // prefix node, then explore everything under it. This is autocomplete, and
    // it is the reason to accept the memory cost of a trie.
    // -------------------------------------------------------------------------
    public string WordsWithPrefix(string prefix)
    {
        TrieNode walk = root;

        for (int i = 0; i < prefix.Length; i++)
        {
            int slot = prefix[i] - 'a';
            steps++;

            if (walk.children[slot] == null)
            {
                return "(none)";
            }

            walk = walk.children[slot]!;
        }

        // Explore the subtree with an explicit stack, carrying the word built so
        // far alongside each node.
        string result = "";
        TrieNode?[] stack = new TrieNode?[nodeCount + 1];
        string[] built = new string[nodeCount + 1];
        int top = 0;

        stack[top] = walk;
        built[top] = prefix;
        top++;

        while (top > 0)
        {
            top--;
            TrieNode node = stack[top]!;
            string sofar = built[top];
            steps++;

            if (node.isEndOfWord)
            {
                result += sofar + " ";
            }

            for (int slot = 25; slot >= 0; slot--)
            {
                if (node.children[slot] != null)
                {
                    stack[top] = node.children[slot];
                    built[top] = sofar + (char)('a' + slot);
                    top++;
                }
            }
        }

        return result;
    }
}
