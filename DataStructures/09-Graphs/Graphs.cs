#!/usr/bin/env dotnet
// =============================================================================
// GRAPHS — nodes and the connections between them. The interesting decision is
// HOW you store the connections, because that choice sets every complexity.
//
//   dotnet run Graphs.cs                  everything
//   dotnet run Graphs.cs -- memory        how it sits in memory
//   dotnet run Graphs.cs -- ops           each operation, and what it cost
//   dotnet run Graphs.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowTraversalCost();
    ShowRepresentationCost();
}


// =============================================================================
// HOW A GRAPH IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A GRAPH IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      A graph is just V nodes and E edges:

              0 ──── 1
              │      │
              │      │
              2 ──── 3 ──── 4

      There are two ways to store those edges, and the choice IS the design.

      ADJACENCY MATRIX — a V x V grid of bools:

               0  1  2  3  4
            0 [ 0  1  1  0  0 ]
            1 [ 1  0  0  1  0 ]        matrix[a][b] = is there an edge a->b
            2 [ 1  0  0  1  0 ]
            3 [ 0  1  1  0  1 ]
            4 [ 0  0  0  1  0 ]

        size            V^2 always, however few edges there are
        HasEdge(a,b)    O(1)   one array read
        neighbours(a)   O(V)   scan a whole row, mostly zeroes

      ADJACENCY LIST — per node, only its actual neighbours:

            0 -> [1, 2]
            1 -> [0, 3]              store what exists, nothing more
            2 -> [0, 3]
            3 -> [1, 2, 4]
            4 -> [3]

        size            V + 2E   (each edge appears twice, once per endpoint)
        HasEdge(a,b)    O(degree) walk that node's list
        neighbours(a)   O(degree) exactly the ones that exist

      THE NUMBERS. A social network with 1,000,000 users averaging 100 friends:

        matrix   1,000,000^2  = 1,000,000,000,000 cells   ~1 TB    impossible
        list     V + 2E       = 201,000,000 entries       ~800 MB  fine

      DENSITY DECIDES. A graph is DENSE when E approaches V^2, SPARSE when E is
      closer to V. Real graphs — road networks, social graphs, dependency trees —
      are almost always sparse, which is why the adjacency list is the default.

      Use a matrix when the graph is small, genuinely dense, or when you need
      O(1) "is there an edge?" more than you need memory.
      """);

    Console.WriteLine();
    Console.WriteLine("  MEASURED — the same graph stored both ways:");
    Console.WriteLine();
    Console.WriteLine("         V        E    matrix cells    list entries    matrix/list");

    int[] vertices = [100, 500, 1000, 5000];
    for (int i = 0; i < vertices.Length; i++)
    {
        int v = vertices[i];
        int e = v * 3;                       // sparse: 3 edges per node
        long matrixCells = (long)v * v;
        long listEntries = v + 2L * e;
        Console.WriteLine($"   {v,7}{e,9}{matrixCells,16}{listEntries,16}{matrixCells / listEntries,13}x");
    }

    Console.WriteLine();
    Console.WriteLine("  The gap widens with V, because the matrix grows quadratically while the");
    Console.WriteLine("  list grows linearly. At V=5000 the matrix wastes 99.8% of its cells.");
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
    Console.WriteLine("  `steps` counts every node or edge inspected.");
    Console.WriteLine();

    Graphs graph = new Graphs(6);
    long before;

    before = graph.steps;
    graph.AddEdge(0, 1);
    graph.AddEdge(0, 2);
    graph.AddEdge(1, 3);
    graph.AddEdge(2, 3);
    graph.AddEdge(3, 4);
    Console.WriteLine($"    AddEdge x5                              cost {graph.steps - before,6} step(s)");
    Console.WriteLine($"          {graph.Picture()}");
    Console.WriteLine("          Node 5 has no edges at all — a disconnected component.");
    Console.WriteLine();

    before = graph.steps;
    string bfs = graph.BreadthFirst(0);
    Console.WriteLine($"    BreadthFirst(0)                         cost {graph.steps - before,6} step(s)");
    Console.WriteLine($"          {bfs}");
    Console.WriteLine("          Explores in RINGS of increasing distance, so the first time you");
    Console.WriteLine("          reach a node is via the SHORTEST path. Needs a queue.");
    Console.WriteLine();

    before = graph.steps;
    string dfs = graph.DepthFirst(0);
    Console.WriteLine($"    DepthFirst(0)                           cost {graph.steps - before,6} step(s)");
    Console.WriteLine($"          {dfs}");
    Console.WriteLine("          Goes as deep as possible before backtracking. Needs a stack, and");
    Console.WriteLine("          gives NO shortest-path guarantee — same cost, different answer.");
    Console.WriteLine();

    before = graph.steps;
    int distance = graph.ShortestPathLength(0, 4);
    Console.WriteLine($"    ShortestPathLength(0,4) = {distance}             cost {graph.steps - before,6} step(s)");
    Console.WriteLine("          0 -> 1 -> 3 -> 4. BFS finds this; DFS might have wandered");
    Console.WriteLine("          0 -> 2 -> 3 -> 4 and reported the same length only by luck.");
    Console.WriteLine();

    before = graph.steps;
    int unreachable = graph.ShortestPathLength(0, 5);
    Console.WriteLine($"    ShortestPathLength(0,5) = {unreachable}            cost {graph.steps - before,6} step(s)");
    Console.WriteLine("          -1 means unreachable. The search exhausted the component and");
    Console.WriteLine("          never found 5 — which is how you detect disconnection.");
    Console.WriteLine();

    before = graph.steps;
    int components = graph.CountComponents();
    Console.WriteLine($"    CountComponents() = {components}                   cost {graph.steps - before,6} step(s)");
    Console.WriteLine("          Traverse from every unvisited node; each new traversal is one");
    Console.WriteLine("          more island. {0,1,2,3,4} and {5}.");
    Console.WriteLine();

    Console.WriteLine("    CYCLE DETECTION — the undirected case needs the parent check:");
    Console.WriteLine();
    Console.WriteLine($"          this graph has a cycle: {graph.HasCycle()}   (0-1-3-2-0)");

    Graphs tree = new Graphs(4);
    tree.AddEdge(0, 1);
    tree.AddEdge(1, 2);
    tree.AddEdge(1, 3);
    Console.WriteLine($"          a tree has a cycle:     {tree.HasCycle()}");
    Console.WriteLine();
    Console.WriteLine("          In an UNDIRECTED graph every edge looks like a 2-cycle (a->b");
    Console.WriteLine("          then b->a), so you must skip the node you arrived from. In a");
    Console.WriteLine("          DIRECTED graph you instead track nodes on the current path.");
    Console.WriteLine("          Getting this wrong is the classic cycle-detection bug.");
}


// =============================================================================
// COMPLEXITY — one method per property, each measuring itself
// =============================================================================

void ShowTraversalCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — BFS over a sparse graph");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  V nodes, 2V edges. Visiting everything reachable.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Graphs graph = new Graphs(sizes[s]);
        for (int i = 1; i < sizes[s]; i++) graph.AddEdge(i, i / 2);          // a tree
        for (int i = 0; i < sizes[s] - 1; i++) graph.AddEdge(i, i + 1);      // plus a chain

        graph.steps = 0;
        graph.BreadthFirst(0);
        work[s] = graph.steps;
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
                    : r < 2.4 ? "doubling    -> O(V + E)"
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
    Console.WriteLine("  Linear in V + E — every node is visited once and every edge is looked");
    Console.WriteLine("  at once. The `visited` array is what makes this linear rather than");
    Console.WriteLine("  infinite: without it, any cycle loops forever.");
}

void ShowRepresentationCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — adjacency list vs matrix, same sparse graph");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Visiting every neighbour of every node, both ways.");
    Console.WriteLine();

    int[] sizes = [100, 200, 400, 800, 1600];
    long[] listWork = new long[sizes.Length];
    long[] matrixWork = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        int v = sizes[s];

        // Adjacency list: touch only the edges that exist.
        Graphs graph = new Graphs(v);
        for (int i = 1; i < v; i++) graph.AddEdge(i, i / 2);

        graph.steps = 0;
        for (int node = 0; node < v; node++)
        {
            for (int k = 0; k < graph.degree[node]; k++)
            {
                int unused = graph.adjacency[node][k];
                graph.steps++;
            }
        }
        listWork[s] = graph.steps;

        // Adjacency matrix: scan every cell, mostly zeroes.
        long scanned = 0;
        bool[,] matrix = new bool[v, v];
        for (int i = 1; i < v; i++)
        {
            matrix[i, i / 2] = true;
            matrix[i / 2, i] = true;
        }
        for (int a = 0; a < v; a++)
        {
            for (int b = 0; b < v; b++)
            {
                bool unused = matrix[a, b];
                scanned++;
            }
        }
        matrixWork[s] = scanned;

        if (matrixWork[s] > max) max = matrixWork[s];
    }

    Console.WriteLine("         V         list       matrix    matrix/list");
    for (int s = 0; s < sizes.Length; s++)
    {
        Console.WriteLine($"   {sizes[s],7}{listWork[s],13}{matrixWork[s],13}{matrixWork[s] / listWork[s],13}x");
    }

    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * listWork[s] / max);
        Console.WriteLine($"   V={sizes[s],-7}{new string('#', width)}   list");
    }
    Console.WriteLine();
    for (int s = 0; s < sizes.Length; s++)
    {
        int width = max == 0 ? 0 : (int)(44.0 * matrixWork[s] / max);
        Console.WriteLine($"   V={sizes[s],-7}{new string('#', width)}   matrix");
    }

    Console.WriteLine();
    Console.WriteLine("  The list is O(V + E) — linear. The matrix is O(V^2) — quadratic, and");
    Console.WriteLine("  the ratio DOUBLES every time V doubles.");
    Console.WriteLine();
    Console.WriteLine("  Identical graph, identical answer. Only the storage choice differs, and");
    Console.WriteLine("  on a sparse graph it is the difference between linear and quadratic.");
    Console.WriteLine();
}


// =============================================================================
// THE CLASS
//
// An undirected graph as an adjacency list, using plain int arrays so the memory
// is visible. Every operation carries its own traversal loop.
// =============================================================================

public class Graphs
{
    // adjacency[a] holds the neighbours of a; degree[a] says how many are live.
    // Two flat arrays rather than List<int> so nothing is hidden.
    public int[][] adjacency;
    public int[] degree;

    public int vertexCount;
    public long steps = 0;

    public Graphs(int vertexCount)
    {
        this.vertexCount = vertexCount;
        adjacency = new int[vertexCount][];
        degree = new int[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            adjacency[i] = new int[4];
            degree[i] = 0;
        }
    }

    // -------------------------------------------------------------------------
    // AddEdge — connect a and b.
    //
    // O(1) amortised. UNDIRECTED, so the edge is recorded twice — once in each
    // node's list. That is why an adjacency list holds 2E entries, not E.
    // -------------------------------------------------------------------------
    public void AddEdge(int a, int b)
    {
        if (degree[a] == adjacency[a].Length)
        {
            int[] bigger = new int[adjacency[a].Length * 2];
            for (int i = 0; i < degree[a]; i++) bigger[i] = adjacency[a][i];
            adjacency[a] = bigger;
        }
        adjacency[a][degree[a]] = b;
        degree[a]++;
        steps++;

        if (degree[b] == adjacency[b].Length)
        {
            int[] bigger = new int[adjacency[b].Length * 2];
            for (int i = 0; i < degree[b]; i++) bigger[i] = adjacency[b][i];
            adjacency[b] = bigger;
        }
        adjacency[b][degree[b]] = a;
        degree[b]++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // BreadthFirst — visit everything reachable, in rings.
    //
    // O(V + E). A QUEUE is what makes it breadth-first: nodes come out in the
    // order they were discovered, so you finish distance 1 before starting
    // distance 2. That is why the first arrival at a node is the shortest path.
    // -------------------------------------------------------------------------
    public string BreadthFirst(int start)
    {
        bool[] visited = new bool[vertexCount];
        int[] queue = new int[vertexCount];
        int head = 0;
        int tail = 0;
        string order = "";

        visited[start] = true;
        queue[tail++] = start;

        while (head < tail)
        {
            int node = queue[head++];
            order += node + " ";
            steps++;

            for (int i = 0; i < degree[node]; i++)
            {
                int neighbour = adjacency[node][i];
                steps++;

                // Mark on DISCOVERY, not on visit — otherwise a node can be
                // queued twice before it is processed.
                if (!visited[neighbour])
                {
                    visited[neighbour] = true;
                    queue[tail++] = neighbour;
                }
            }
        }

        return order;
    }

    // -------------------------------------------------------------------------
    // DepthFirst — visit everything reachable, going deep first.
    //
    // O(V + E), the same cost as BFS. The ONLY difference is a stack instead of
    // a queue — and that difference removes the shortest-path guarantee.
    // -------------------------------------------------------------------------
    public string DepthFirst(int start)
    {
        bool[] visited = new bool[vertexCount];
        int[] stack = new int[vertexCount * 2];
        int top = 0;
        string order = "";

        stack[top++] = start;

        while (top > 0)
        {
            int node = stack[--top];
            steps++;

            if (visited[node])
            {
                continue;
            }

            visited[node] = true;
            order += node + " ";

            for (int i = degree[node] - 1; i >= 0; i--)
            {
                int neighbour = adjacency[node][i];
                steps++;
                if (!visited[neighbour])
                {
                    if (top < stack.Length) stack[top++] = neighbour;
                }
            }
        }

        return order;
    }

    // -------------------------------------------------------------------------
    // ShortestPathLength — fewest edges from a to b, or -1.
    //
    // O(V + E). BFS ONLY. Because BFS expands in rings of increasing distance,
    // the first time it reaches b it has used the fewest possible edges. DFS
    // cannot promise this — it might dive down a long path first.
    // -------------------------------------------------------------------------
    public int ShortestPathLength(int from, int to)
    {
        bool[] visited = new bool[vertexCount];
        int[] distance = new int[vertexCount];
        int[] queue = new int[vertexCount];
        int head = 0;
        int tail = 0;

        visited[from] = true;
        distance[from] = 0;
        queue[tail++] = from;

        while (head < tail)
        {
            int node = queue[head++];
            steps++;

            if (node == to)
            {
                return distance[node];
            }

            for (int i = 0; i < degree[node]; i++)
            {
                int neighbour = adjacency[node][i];
                steps++;

                if (!visited[neighbour])
                {
                    visited[neighbour] = true;
                    distance[neighbour] = distance[node] + 1;
                    queue[tail++] = neighbour;
                }
            }
        }

        return -1;   // exhausted the component without finding it
    }

    // -------------------------------------------------------------------------
    // CountComponents — how many disconnected islands are there?
    //
    // O(V + E). Traverse from every node not yet visited; each traversal that
    // has to start fresh is one more island.
    // -------------------------------------------------------------------------
    public int CountComponents()
    {
        bool[] visited = new bool[vertexCount];
        int[] stack = new int[vertexCount * 2];
        int islands = 0;

        for (int start = 0; start < vertexCount; start++)
        {
            if (visited[start])
            {
                continue;
            }

            islands++;
            int top = 0;
            stack[top++] = start;

            while (top > 0)
            {
                int node = stack[--top];
                steps++;

                if (visited[node])
                {
                    continue;
                }
                visited[node] = true;

                for (int i = 0; i < degree[node]; i++)
                {
                    int neighbour = adjacency[node][i];
                    steps++;
                    if (!visited[neighbour] && top < stack.Length)
                    {
                        stack[top++] = neighbour;
                    }
                }
            }
        }

        return islands;
    }

    // -------------------------------------------------------------------------
    // HasCycle — undirected cycle detection.
    //
    // O(V + E). The catch: in an undirected graph every edge looks like a
    // 2-cycle, because a->b is also b->a. So you must ignore the node you
    // arrived FROM. Seeing any other visited node means a real cycle.
    // -------------------------------------------------------------------------
    public bool HasCycle()
    {
        bool[] visited = new bool[vertexCount];
        int[] stack = new int[vertexCount * 4];
        int[] cameFrom = new int[vertexCount * 4];

        for (int start = 0; start < vertexCount; start++)
        {
            if (visited[start])
            {
                continue;
            }

            int top = 0;
            stack[top] = start;
            cameFrom[top] = -1;
            top++;

            while (top > 0)
            {
                top--;
                int node = stack[top];
                int parent = cameFrom[top];
                steps++;

                if (visited[node])
                {
                    return true;   // reached an already-visited node another way
                }
                visited[node] = true;

                for (int i = 0; i < degree[node]; i++)
                {
                    int neighbour = adjacency[node][i];
                    steps++;

                    // Skip the edge we walked in on — it is not a cycle.
                    if (neighbour == parent)
                    {
                        continue;
                    }

                    if (visited[neighbour])
                    {
                        return true;
                    }

                    if (top < stack.Length)
                    {
                        stack[top] = neighbour;
                        cameFrom[top] = node;
                        top++;
                    }
                }
            }
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Picture — the adjacency list, for the demos.
    // -------------------------------------------------------------------------
    public string Picture()
    {
        string result = "";

        for (int node = 0; node < vertexCount; node++)
        {
            result += node + "->[";
            for (int i = 0; i < degree[node]; i++)
            {
                result += adjacency[node][i];
                if (i < degree[node] - 1) result += ",";
            }
            result += "] ";
        }

        return result;
    }
}
