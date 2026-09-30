#!/usr/bin/env dotnet
// =============================================================================
// BINARY SEARCH TREES — scattered nodes like a linked list, but each one has TWO
// pointers and an ordering rule, so every comparison discards half the data.
//
//   dotnet run Trees.cs                  everything
//   dotnet run Trees.cs -- memory        how it sits in memory
//   dotnet run Trees.cs -- ops           each operation, and what it cost
//   dotnet run Trees.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowBalancedSearchCost();
    ShowDegenerateSearchCost();
}


// =============================================================================
// HOW A BINARY SEARCH TREE IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A BINARY SEARCH TREE IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      Like a linked list, every node is its OWN heap object, scattered anywhere.
      The difference is that each holds TWO references instead of one:

                         ┌──────────────┐
                         │ hdr    16 B  │
                         │ value    50  │
                         │ left  ──┐    │
                         │ right ──┼─┐  │
                         └─────────┼─┼──┘
                     ┌─────────────┘ └─────────────┐
                     ▼                             ▼
            ┌──────────────┐               ┌──────────────┐
            │ value    30  │               │ value    70  │
            │ left  · right│               │ left  · right│
            └──────────────┘               └──────────────┘
               0x9f88                         0x2c04
               └──── no relationship between the addresses ────┘

      COST OF ONE NODE holding a single int:

        object header                16 bytes
        left reference                8 bytes
        right reference               8 bytes
        int value                     4 bytes
        padding                       4 bytes
        --------------------------- ---------
        total                        40 bytes    to store 4 bytes of data

      A million ints: 4 MB as an int[], 40 MB as a BST, plus a million objects
      for the GC. You are paying 10x for the ability to search in O(log n) and
      to keep the data sorted through inserts.

      THE ORDERING RULE — everything left of a node is smaller, everything right
      is larger:

                        50
                   ┌────┴────┐
                  30         70
                ┌─┴─┐      ┌─┴─┐
               20   40    60   80

      That rule is what buys the log n. Comparing against 50 tells you which HALF
      of the tree to discard — you never look at the other side. Same idea as
      binary search, but on a structure you can insert into cheaply.

      WHY IT IS NOT AN ARRAY: keeping a sorted array sorted costs O(n) per insert
      because everything shifts. Here inserting is just hanging a new node off a
      leaf — no shifting at all. That is the trade the 40 bytes buys.
      """);

    Console.WriteLine();
    Console.WriteLine("  THE FATAL WEAKNESS — the shape depends on INSERTION ORDER:");
    Console.WriteLine();

    Trees balanced = new Trees();
    int[] mixed = [50, 30, 70, 20, 40, 60, 80];
    for (int i = 0; i < mixed.Length; i++) balanced.Insert(mixed[i]);

    Trees degenerate = new Trees();
    for (int i = 1; i <= 7; i++) degenerate.Insert(i * 10);

    Console.WriteLine("    inserted 50,30,70,20,40,60,80   -> height " + balanced.Height()
        + "   (balanced)");
    Console.WriteLine("                50");
    Console.WriteLine("           ┌────┴────┐");
    Console.WriteLine("          30         70");
    Console.WriteLine("        ┌─┴─┐      ┌─┴─┐");
    Console.WriteLine("       20   40    60   80");
    Console.WriteLine();
    Console.WriteLine("    inserted 10,20,30,40,50,60,70   -> height " + degenerate.Height()
        + "   (a linked list!)");
    Console.WriteLine("       10");
    Console.WriteLine("         └20");
    Console.WriteLine("            └30");
    Console.WriteLine("               └40");
    Console.WriteLine("                  └50 ...");
    Console.WriteLine();
    Console.WriteLine("  SAME VALUES, SAME CLASS, SAME CODE. Inserting already-sorted data gives");
    Console.WriteLine("  every node one child, so the tree degenerates into a linked list and");
    Console.WriteLine("  search collapses from O(log n) to O(n).");
    Console.WriteLine();
    Console.WriteLine("  This is why self-balancing trees (AVL, red-black) exist, and why .NET's");
    Console.WriteLine("  SortedDictionary is a red-black tree rather than a plain BST.");
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
    Console.WriteLine("  `steps` counts every node visited.");
    Console.WriteLine();

    Trees tree = new Trees();
    long before;

    before = tree.steps;
    int[] values = [50, 30, 70, 20, 40, 60, 80];
    for (int i = 0; i < values.Length; i++) tree.Insert(values[i]);
    Console.WriteLine($"    Insert x7                               cost {tree.steps - before,6} step(s)");
    Console.WriteLine($"          count={tree.count} height={tree.Height()}");
    Console.WriteLine("          Each insert walks down one path, not the whole tree.");
    Console.WriteLine();

    before = tree.steps;
    bool found = tree.Contains(40);
    Console.WriteLine($"    Contains(40) = {found}                    cost {tree.steps - before,6} step(s)");
    Console.WriteLine("          50 -> 30 -> 40. Three comparisons out of seven nodes; the");
    Console.WriteLine("          entire right half was discarded on the first one.");
    Console.WriteLine();

    before = tree.steps;
    bool missing = tree.Contains(45);
    Console.WriteLine($"    Contains(45) = {missing}                   cost {tree.steps - before,6} step(s)");
    Console.WriteLine("          A MISS costs the same as a hit — you walk to a leaf either way.");
    Console.WriteLine("          Unlike an array, absence does NOT mean checking everything.");
    Console.WriteLine();

    before = tree.steps;
    string sorted = tree.InOrder();
    Console.WriteLine($"    InOrder()                               cost {tree.steps - before,6} step(s)");
    Console.WriteLine($"          {sorted}");
    Console.WriteLine("          Left, self, right — and the output comes out SORTED, for free.");
    Console.WriteLine("          That is the ordering rule paying off. A hash table cannot do this.");
    Console.WriteLine();

    Console.WriteLine("    THE THREE DELETE CASES — the part interviews always probe:");
    Console.WriteLine();

    Trees d1 = new Trees();
    for (int i = 0; i < values.Length; i++) d1.Insert(values[i]);
    before = d1.steps;
    d1.Remove(20);
    Console.WriteLine($"      1. leaf (20)              cost {d1.steps - before,4}   just detach it");
    Console.WriteLine($"         {d1.InOrder()}");

    Trees d2 = new Trees();
    for (int i = 0; i < values.Length; i++) d2.Insert(values[i]);
    d2.Insert(35);
    before = d2.steps;
    d2.Remove(40);
    Console.WriteLine($"      2. one child (40)         cost {d2.steps - before,4}   promote the child");
    Console.WriteLine($"         {d2.InOrder()}");

    Trees d3 = new Trees();
    for (int i = 0; i < values.Length; i++) d3.Insert(values[i]);
    before = d3.steps;
    d3.Remove(30);
    Console.WriteLine($"      3. two children (30)      cost {d3.steps - before,4}   replace with the in-order successor");
    Console.WriteLine($"         {d3.InOrder()}");
    Console.WriteLine();
    Console.WriteLine("      Case 3 is the hard one: you cannot just remove the node, because two");
    Console.WriteLine("      subtrees would be orphaned. Replace its value with the SMALLEST value");
    Console.WriteLine("      in its right subtree — the only value that keeps the ordering rule");
    Console.WriteLine("      true — then delete that node instead, which has at most one child.");
}


// =============================================================================
// COMPLEXITY — the same search, on a balanced tree and a degenerate one
// =============================================================================

void ShowBalancedSearchCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Contains(x) on a BALANCED tree");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        // Insert in an order that keeps the tree roughly balanced: repeatedly
        // take the middle of each remaining range.
        Trees tree = new Trees();
        int[] pending = new int[sizes[s] * 2];
        int head = 0;
        int tail = 0;
        pending[tail++] = 0;
        pending[tail++] = sizes[s] - 1;

        while (head < tail)
        {
            int low = pending[head++];
            int high = pending[head++];
            if (low > high) continue;

            int mid = (low + high) / 2;
            tree.Insert(mid);

            if (tail + 4 < pending.Length)
            {
                pending[tail++] = low;
                pending[tail++] = mid - 1;
                pending[tail++] = mid + 1;
                pending[tail++] = high;
            }
        }

        // Probe ABOVE every value, so the search walks the longest path there
        // is. Probing below would hit a null left child immediately on a
        // right-leaning tree and report 1 step, which would be a lie.
        tree.steps = 0;
        tree.Contains(sizes[s] + 1);
        work[s] = tree.steps;
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
    Console.WriteLine("  The work goes up by ONE each time n doubles — the signature of O(log n).");
    Console.WriteLine("  Every comparison throws away half the remaining tree.");
}

void ShowDegenerateSearchCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Contains(x) on a DEGENERATE tree");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Identical class, identical values — inserted in SORTED order.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Trees tree = new Trees();
        for (int i = 0; i < sizes[s]; i++) tree.Insert(i);

        tree.steps = 0;
        tree.Contains(sizes[s] + 1);   // walks the entire right spine
        work[s] = tree.steps;
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
    Console.WriteLine("  O(n). The tree has become a linked list — and paid 40 bytes per node");
    Console.WriteLine("  for the privilege, where a plain array would have used 4.");
    Console.WriteLine();
    Console.WriteLine("  At n=16000: balanced ~14 steps, degenerate ~16000. Same code. The only");
    Console.WriteLine("  difference is the order the data arrived in — and sorted input is the");
    Console.WriteLine("  most likely input in real life, which is why nobody ships a plain BST.");
    Console.WriteLine();
}


// =============================================================================
// THE NODE
// =============================================================================

public class TreeNode
{
    public int value;
    public TreeNode? left;
    public TreeNode? right;

    public TreeNode(int value)
    {
        this.value = value;
        this.left = null;
        this.right = null;
    }
}


// =============================================================================
// THE CLASS
//
// A plain (non-balancing) binary search tree. Every operation carries its own
// walking loop rather than calling a shared helper.
// =============================================================================

public class Trees
{
    public TreeNode? root = null;
    public int count = 0;
    public long steps = 0;
    public long allocations = 0;

    // -------------------------------------------------------------------------
    // Insert — add a value, keeping the ordering rule true.
    //
    // O(log n) if the tree is balanced, O(n) if it is not. Walk down comparing;
    // smaller goes left, larger goes right. The new node is always hung off a
    // LEAF, so nothing is ever shifted — the advantage over a sorted array.
    // -------------------------------------------------------------------------
    public void Insert(int value)
    {
        TreeNode fresh = new TreeNode(value);
        allocations++;

        if (root == null)
        {
            root = fresh;
            count++;
            steps++;
            return;
        }

        TreeNode walk = root;
        while (true)
        {
            steps++;

            if (value < walk.value)
            {
                if (walk.left == null)
                {
                    walk.left = fresh;
                    count++;
                    return;
                }
                walk = walk.left;
            }
            else if (value > walk.value)
            {
                if (walk.right == null)
                {
                    walk.right = fresh;
                    count++;
                    return;
                }
                walk = walk.right;
            }
            else
            {
                return;   // already present; this tree holds no duplicates
            }
        }
    }

    // -------------------------------------------------------------------------
    // Contains — is this value in the tree?
    //
    // O(log n) balanced, O(n) degenerate. Each comparison discards an entire
    // subtree, which is where the log comes from. Note a MISS costs the same as
    // a hit — you walk to a leaf either way, unlike an array where proving
    // absence means touching every element.
    // -------------------------------------------------------------------------
    public bool Contains(int value)
    {
        TreeNode? walk = root;

        while (walk != null)
        {
            steps++;

            if (value == walk.value)
            {
                return true;
            }

            // One comparison throws away half the remaining tree.
            walk = value < walk.value ? walk.left : walk.right;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Remove — delete a value. Three genuinely different cases.
    //
    // O(log n) balanced. The two-children case is the one that gets asked about:
    // you cannot detach the node without orphaning two subtrees, so instead you
    // overwrite its value with its IN-ORDER SUCCESSOR — the smallest value in
    // the right subtree, the only one that keeps the ordering rule true — and
    // then delete that successor, which has at most one child.
    // -------------------------------------------------------------------------
    public bool Remove(int value)
    {
        TreeNode? parent = null;
        TreeNode? walk = root;

        // Find the node and remember its parent, so we can re-link.
        while (walk != null && walk.value != value)
        {
            steps++;
            parent = walk;
            walk = value < walk.value ? walk.left : walk.right;
        }

        if (walk == null)
        {
            return false;
        }

        // CASE 3 — two children. Rewrite this node's value with the successor,
        // then fall through to delete the successor node instead.
        if (walk.left != null && walk.right != null)
        {
            TreeNode successorParent = walk;
            TreeNode successor = walk.right;

            while (successor.left != null)
            {
                steps++;
                successorParent = successor;
                successor = successor.left;
            }

            walk.value = successor.value;
            parent = successorParent;
            walk = successor;
        }

        // CASES 1 and 2 — a leaf, or one child. Promote whichever child exists
        // (null if it is a leaf, which detaches it).
        TreeNode? child = walk.left ?? walk.right;

        if (parent == null)
        {
            root = child;
        }
        else if (parent.left == walk)
        {
            parent.left = child;
        }
        else
        {
            parent.right = child;
        }

        count--;
        steps++;
        return true;
    }

    // -------------------------------------------------------------------------
    // InOrder — every value, in sorted order.
    //
    // O(n) time, O(h) SPACE for the recursion stack — and that stack space is
    // the part people forget to count. Left, self, right is what produces sorted
    // output; it is the ordering rule cashed in.
    // -------------------------------------------------------------------------
    public string InOrder()
    {
        string result = "";

        // Iterative, with an explicit stack, so the O(h) space is visible rather
        // than hidden in call frames.
        TreeNode?[] stack = new TreeNode?[count + 1];
        int top = 0;
        TreeNode? walk = root;

        while (walk != null || top > 0)
        {
            while (walk != null)
            {
                stack[top++] = walk;
                walk = walk.left;
                steps++;
            }

            walk = stack[--top];
            result += walk!.value + " ";
            walk = walk.right;
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Height — the longest root-to-leaf path.
    //
    // O(n), because it has to visit everything. This number IS the complexity of
    // every other operation: search costs the height, so a balanced tree gives
    // log n and a degenerate one gives n.
    // -------------------------------------------------------------------------
    public int Height()
    {
        if (root == null)
        {
            return 0;
        }

        // Level-order walk, counting levels — avoids recursing deeply on a
        // degenerate tree, which would overflow the call stack.
        TreeNode?[] queue = new TreeNode?[count + 1];
        int head = 0;
        int tail = 0;
        queue[tail++] = root;
        int height = 0;

        while (head < tail)
        {
            int levelSize = tail - head;
            for (int i = 0; i < levelSize; i++)
            {
                TreeNode node = queue[head++]!;
                if (node.left != null) queue[tail++] = node.left;
                if (node.right != null) queue[tail++] = node.right;
            }
            height++;
        }

        return height;
    }
}
