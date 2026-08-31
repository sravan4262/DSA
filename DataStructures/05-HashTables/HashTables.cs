#!/usr/bin/env dotnet
// =============================================================================
// HASH TABLES — turn a key into an ARRAY INDEX, so lookup skips the search.
// That is the whole idea. Everything else is handling the cases where two keys
// land on the same index.
//
//   dotnet run HashTables.cs                  everything
//   dotnet run HashTables.cs -- memory        how it sits in memory
//   dotnet run HashTables.cs -- ops           each operation, and what it cost
//   dotnet run HashTables.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowGoodHashCost();
    ShowBadHashCost();
}


// =============================================================================
// HOW A HASH TABLE IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A HASH TABLE IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      An array you cannot index by position, indexed by CONTENT instead.

        key ──► hash(key) ──► % bucketCount ──► an array index

        "apple"  ──►  9834721  ──►  9834721 % 8  ──►  bucket 1

      No scanning. One arithmetic step turns the key into a slot number, exactly
      like an array turns an index into an address. That is where O(1) comes from.

        buckets: int[8] of references          entries: separate heap objects
        ┌───┬───┬───┬───┬───┬───┬───┬───┐
        │ · │ ●─┼─· │ ●─┼─· │ · │ ●─┼─· │
        └───┴─┼─┴───┴─┼─┴───┴───┴─┼─┴───┘
          0   1   2   3   4   5   6   7
              │       │           │
              ▼       ▼           ▼
        ┌──────────┐ ┌──────────┐ ┌──────────┐
        │"apple" 3 │ │"pear"  9 │ │"fig"   1 │
        │ next ────┼┐│ next=null│ │ next=null│
        └──────────┘││└──────────┘ └──────────┘
                    ▼│
        ┌──────────┐ │   TWO KEYS IN BUCKET 1 = a COLLISION.
        │"grape" 7 │◄┘   They are chained in a linked list.
        │ next=null│
        └──────────┘

      COST. The bucket array is 24 + 8n bytes (references, not data). Each entry
      is its OWN heap object:

        object header                16 bytes
        key reference                 8 bytes
        value                         4 bytes
        next reference                8 bytes
        padding                       4 bytes
        --------------------------- ---------
        per entry                    40 bytes   plus the key object itself

      So a hash table is NOT compact. You trade memory and cache locality for
      the ability to skip the search entirely.
      """);

    Console.WriteLine();
    Console.WriteLine("  LOAD FACTOR = entries / buckets. It decides everything.");
    Console.WriteLine();
    Console.WriteLine("    load 0.25   mostly empty buckets, chains of 0-1   fast, wasteful");
    Console.WriteLine("    load 0.75   the usual target                      fast enough");
    Console.WriteLine("    load 4.00   every bucket holds a chain of ~4      degrading to O(n)");
    Console.WriteLine();
    Console.WriteLine("  When load passes the threshold the table RESIZES: allocate twice the");
    Console.WriteLine("  buckets and REHASH every entry, because `% bucketCount` changed and");
    Console.WriteLine("  every key now belongs somewhere else. That is O(n), and it is why");
    Console.WriteLine("  insert is O(1) *amortised* rather than O(1) outright.");
    Console.WriteLine();

    HashTables demo = new HashTables();
    string[] words = ["apple", "pear", "fig", "grape", "plum", "lime", "date"];
    for (int i = 0; i < words.Length; i++)
    {
        demo.Put(words[i], i + 1);
        Console.WriteLine($"    Put(\"{words[i]}\")".PadRight(24)
            + $"buckets={demo.buckets.Length} entries={demo.count} "
            + $"load={(double)demo.count / demo.buckets.Length:0.00} longestChain={demo.LongestChain()}");
    }

    Console.WriteLine();
    Console.WriteLine("  WHY O(1) IS ONLY *AVERAGE*: if every key hashed to the same bucket the");
    Console.WriteLine("  table would be one long linked list and lookup would be O(n). A good");
    Console.WriteLine("  hash spreads keys evenly; a bad one makes an expensive linked list.");
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
    Console.WriteLine("  `steps` counts every entry inspected — the chain walk, not the hash.");
    Console.WriteLine();

    HashTables table = new HashTables();
    long before;

    before = table.steps;
    string[] words = ["apple", "pear", "fig", "grape", "plum"];
    for (int i = 0; i < words.Length; i++) table.Put(words[i], i + 1);
    Console.WriteLine($"    Put x5                                  cost {table.steps - before,6} step(s)");
    Console.WriteLine($"          buckets={table.buckets.Length} entries={table.count} rehashes={table.rehashes}");
    Console.WriteLine();

    before = table.steps;
    int found = table.Get("fig");
    Console.WriteLine($"    Get(\"fig\") = {found}                        cost {table.steps - before,6} step(s)");
    Console.WriteLine("          Hash the key, jump to the bucket, walk a very short chain.");
    Console.WriteLine();

    before = table.steps;
    table.Put("apple", 99);
    Console.WriteLine($"    Put(\"apple\", 99) — overwrite            cost {table.steps - before,6} step(s)");
    Console.WriteLine($"          Get(\"apple\") = {table.Get("apple")}, entries still {table.count}");
    Console.WriteLine("          A key already present is updated in place, not duplicated.");
    Console.WriteLine();

    before = table.steps;
    bool missing = table.ContainsKey("banana");
    Console.WriteLine($"    ContainsKey(\"banana\") = {missing}          cost {table.steps - before,6} step(s)");
    Console.WriteLine("          A MISS is cheap here — unlike an array, where it means");
    Console.WriteLine("          scanning everything. You only walk one bucket's chain.");
    Console.WriteLine();

    before = table.steps;
    table.Remove("pear");
    Console.WriteLine($"    Remove(\"pear\")                          cost {table.steps - before,6} step(s)");
    Console.WriteLine($"          entries={table.count}, ContainsKey(\"pear\") = {table.ContainsKey("pear")}");
    Console.WriteLine();

    Console.WriteLine("    Get on a missing key:");
    try
    {
        table.Get("banana");
    }
    catch (KeyNotFoundException error)
    {
        Console.WriteLine($"          threw KeyNotFoundException — \"{error.Message}\"");
    }
    Console.WriteLine();

    Console.WriteLine("    WATCH A REHASH — insert until the load factor trips:");
    Console.WriteLine();
    HashTables growing = new HashTables();
    for (int i = 0; i < 14; i++)
    {
        int rehashesBefore = growing.rehashes;
        growing.Put("key" + i, i);
        if (growing.rehashes != rehashesBefore)
        {
            Console.WriteLine($"          insert #{i + 1,2}  REHASH -> {growing.buckets.Length} buckets, "
                + $"every one of the {growing.count} entries moved");
        }
    }
    Console.WriteLine($"          final: buckets={growing.buckets.Length} entries={growing.count} "
        + $"load={(double)growing.count / growing.buckets.Length:0.00}");
}


// =============================================================================
// COMPLEXITY — the same operation, with a good hash and a deliberately bad one
// =============================================================================

void ShowGoodHashCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Get(key) with a GOOD hash (the average case)");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        HashTables table = new HashTables();
        for (int i = 0; i < sizes[s]; i++) table.Put("key" + i, i);

        // Average the chain walk over 100 lookups spread across the table.
        table.steps = 0;
        for (int i = 0; i < 100; i++) table.Get("key" + (i * (sizes[s] / 100)));
        work[s] = table.steps / 100;
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
            reading = r < 1.02 ? "flat        -> O(1)"
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
    Console.WriteLine("  Flat. The table grows, the chains do not — because the bucket count");
    Console.WriteLine("  grows with it, holding the load factor roughly constant. THAT is why");
    Console.WriteLine("  lookup is O(1): not magic, just resizing to keep chains short.");
}

void ShowBadHashCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Get(key) with a BAD hash (the worst case)");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Identical table, identical keys — but every key hashes to bucket 0.");
    Console.WriteLine("  This is what an attacker does, or a badly written GetHashCode.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        HashTables table = new HashTables();
        table.sabotageHash = true;
        for (int i = 0; i < sizes[s]; i++) table.Put("key" + i, i);

        table.steps = 0;
        table.ContainsKey("missing");   // walks the whole chain to prove absence
        work[s] = table.steps;
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
            reading = r < 1.02 ? "flat        -> O(1)"
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
    Console.WriteLine("  O(n). The hash table has degenerated into ONE LINKED LIST.");
    Console.WriteLine();
    Console.WriteLine("  This is why the honest claim is \"O(1) AVERAGE, O(n) WORST\" — and why");
    Console.WriteLine("  saying just \"hash lookup is O(1)\" invites the follow-up question.");
    Console.WriteLine();
}


// =============================================================================
// THE ENTRY
//
// One heap object per key/value pair, chained when keys collide.
// =============================================================================

public class Entry
{
    public string key;
    public int value;
    public Entry? next;

    public Entry(string key, int value)
    {
        this.key = key;
        this.value = value;
        this.next = null;
    }
}


// =============================================================================
// THE CLASS
//
// Separate chaining, which is the easiest collision strategy to reason about.
// Every operation carries its own hashing and its own chain walk.
// =============================================================================

public class HashTables
{
    // An array of chain heads. Most are null; that emptiness is the point.
    public Entry?[] buckets = new Entry?[8];

    public int count = 0;

    // Resize when entries/buckets passes this. 0.75 is the usual compromise
    // between wasted space and long chains.
    public double maxLoad = 0.75;

    // Flip to make every key collide, to see the worst case.
    public bool sabotageHash = false;

    public long steps = 0;
    public int rehashes = 0;

    // -------------------------------------------------------------------------
    // Put — store a value under a key, or overwrite it.
    //
    // O(1) amortised. Hash to a bucket, walk that bucket's chain looking for the
    // key. The chain is short *because* we resize to keep the load factor down —
    // that resize is the O(n) case hiding inside the amortised O(1).
    // -------------------------------------------------------------------------
    public void Put(string key, int value)
    {
        // Grow BEFORE inserting if this would push us over the load factor.
        if ((double)(count + 1) / buckets.Length > maxLoad)
        {
            Entry?[] old = buckets;
            buckets = new Entry?[old.Length * 2];

            // Every entry must be REHASHED: `% bucketCount` changed, so almost
            // all of them now belong in a different bucket. This is the O(n).
            for (int i = 0; i < old.Length; i++)
            {
                Entry? walk = old[i];
                while (walk != null)
                {
                    Entry? saved = walk.next;

                    int slot = 0;
                    if (!sabotageHash)
                    {
                        int h = 0;
                        for (int c = 0; c < walk.key.Length; c++) h = h * 31 + walk.key[c];
                        slot = (h & 0x7fffffff) % buckets.Length;
                    }

                    walk.next = buckets[slot];
                    buckets[slot] = walk;
                    walk = saved;
                    steps++;
                }
            }

            rehashes++;
        }

        // Hash the key down to a bucket index. The & 0x7fffffff drops the sign
        // bit, because a negative hash would give a negative index.
        int bucket = 0;
        if (!sabotageHash)
        {
            int hash = 0;
            for (int c = 0; c < key.Length; c++) hash = hash * 31 + key[c];
            bucket = (hash & 0x7fffffff) % buckets.Length;
        }

        // Walk this bucket's chain: the key may already be here.
        Entry? current = buckets[bucket];
        while (current != null)
        {
            steps++;
            if (current.key == key)
            {
                current.value = value;
                return;
            }
            current = current.next;
        }

        // Not present — push a new entry onto the front of the chain.
        Entry fresh = new Entry(key, value);
        fresh.next = buckets[bucket];
        buckets[bucket] = fresh;
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // Get — read the value stored under a key.
    //
    // O(1) average, O(n) worst. The hash jumps straight to the right bucket; the
    // only work is walking that bucket's chain, which is short when the hash
    // spreads keys evenly and is the ENTIRE table when it does not.
    // -------------------------------------------------------------------------
    public int Get(string key)
    {
        int bucket = 0;
        if (!sabotageHash)
        {
            int hash = 0;
            for (int c = 0; c < key.Length; c++) hash = hash * 31 + key[c];
            bucket = (hash & 0x7fffffff) % buckets.Length;
        }

        Entry? current = buckets[bucket];
        while (current != null)
        {
            steps++;
            if (current.key == key)
            {
                return current.value;
            }
            current = current.next;
        }

        throw new KeyNotFoundException($"the key \"{key}\" is not in the table");
    }

    // -------------------------------------------------------------------------
    // ContainsKey — is this key present?
    //
    // O(1) average. Note how cheap a MISS is compared to an array, where proving
    // absence means scanning all n elements. Here you only walk one chain.
    // -------------------------------------------------------------------------
    public bool ContainsKey(string key)
    {
        int bucket = 0;
        if (!sabotageHash)
        {
            int hash = 0;
            for (int c = 0; c < key.Length; c++) hash = hash * 31 + key[c];
            bucket = (hash & 0x7fffffff) % buckets.Length;
        }

        Entry? current = buckets[bucket];
        while (current != null)
        {
            steps++;
            if (current.key == key)
            {
                return true;
            }
            current = current.next;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Remove — drop a key.
    //
    // O(1) average. Unlinking from a chain needs the PREVIOUS entry, which is
    // why the walk tracks two pointers — the same problem a singly linked list
    // has. Note the table never shrinks its bucket array.
    // -------------------------------------------------------------------------
    public bool Remove(string key)
    {
        int bucket = 0;
        if (!sabotageHash)
        {
            int hash = 0;
            for (int c = 0; c < key.Length; c++) hash = hash * 31 + key[c];
            bucket = (hash & 0x7fffffff) % buckets.Length;
        }

        Entry? previous = null;
        Entry? current = buckets[bucket];

        while (current != null)
        {
            steps++;
            if (current.key == key)
            {
                if (previous == null)
                {
                    buckets[bucket] = current.next;   // it was the chain head
                }
                else
                {
                    previous.next = current.next;     // skip over it
                }
                count--;
                return true;
            }
            previous = current;
            current = current.next;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // LongestChain — the worst bucket, i.e. the real cost of a lookup.
    //
    // O(buckets + entries). Diagnostic only: a healthy table keeps this at 1-3,
    // and a chain as long as `count` means the hash has failed completely.
    // -------------------------------------------------------------------------
    public int LongestChain()
    {
        int longest = 0;

        for (int i = 0; i < buckets.Length; i++)
        {
            int length = 0;
            Entry? walk = buckets[i];
            while (walk != null)
            {
                length++;
                walk = walk.next;
            }
            if (length > longest) longest = length;
        }

        return longest;
    }
}
