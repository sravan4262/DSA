#!/usr/bin/env dotnet
// =============================================================================
// QUEUES — both ends are used, which breaks the naive array. The fix is to stop
// moving the data and move the INDICES instead, wrapping them around.
//
//   dotnet run Queues.cs                  everything
//   dotnet run Queues.cs -- memory        how it sits in memory
//   dotnet run Queues.cs -- ops           each operation, and what it cost
//   dotnet run Queues.cs -- complexity    measured growth curves
//
// Nothing is shared or factored out. Every method carries its own code.
// =============================================================================

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (mode is "all" or "memory") ShowMemory();
if (mode is "all" or "ops") ShowOperations();
if (mode is "all" or "complexity")
{
    ShowDequeueCost();
    ShowNaiveDequeueCost();
}


// =============================================================================
// HOW A QUEUE IS STORED IN MEMORY
// =============================================================================

void ShowMemory()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  HOW A QUEUE IS STORED IN MEMORY");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    Console.WriteLine("""
      THE PROBLEM. A queue adds at the back and removes from the FRONT — which is
      an array's worst case:

        Dequeue()
        ┌────┬────┬────┬────┬────┐
        │ 10 │ 20 │ 30 │ 40 │    │   return 10, then...
        └────┴────┴────┴────┴────┘
              ◄────┴────┴────         ...shift EVERYTHING left    O(n) each time
        ┌────┬────┬────┬────┬────┐
        │ 20 │ 30 │ 40 │    │    │    n dequeues = O(n^2). Unusable.
        └────┴────┴────┴────┴────┘

      THE FIX. Do not move the data — move two indices:

        ┌────┬────┬────┬────┬────┬────┬────┬────┐
        │  · │  · │ 30 │ 40 │ 50 │  · │  · │  · │
        └────┴────┴────┴────┴────┴────┴────┴────┘
                    ▲              ▲
                  head           tail
              (next to remove) (next to insert)

      Dequeue reads head and increments it. Enqueue writes at tail and increments
      it. Both O(1), nothing shifts. But now the front is dead space and tail
      marches off the end.

      THE CIRCULAR BUFFER. Let the indices WRAP with modulo — treat the array as
      a ring:

             linear view                       ring view
        ┌────┬────┬────┬────┬────┬────┬────┐        0   1
        │ 60 │ 70 │  · │  · │ 30 │ 40 │ 50 │      7 ┌───────┐ 2
        └────┴────┴────┴────┴────┴────┴────┘        │       │
               ▲         ▲                        6 │   ●   │ 3
             tail      head                          └───────┘
                                                    5   4
        head = 4, tail = 2, count = 5
        the queue in order is 30 40 50 60 70 — it simply wraps past the end

        Enqueue(x)  ->  items[tail] = x;  tail = (tail + 1) % items.Length
        Dequeue()   ->  x = items[head];  head = (head + 1) % items.Length

      The % is the entire trick. No shifting, no wasted space, both ends O(1).

      WHY WE STORE count: when head == tail the queue is either completely empty
      or completely full, and those two cannot be told apart. Tracking count
      resolves it. (The alternative is deliberately leaving one slot empty.)
      """);

    Console.WriteLine();
    Console.WriteLine("  WATCH IT WRAP — enqueue 6, dequeue 4, enqueue 3 more:");
    Console.WriteLine();

    Queues demo = new Queues();
    for (int i = 1; i <= 6; i++) demo.Enqueue(i * 10);
    for (int i = 0; i < 4; i++) demo.Dequeue();
    for (int i = 7; i <= 9; i++) demo.Enqueue(i * 10);

    string cells = "";
    for (int i = 0; i < demo.items.Length; i++)
    {
        bool live = false;
        for (int k = 0; k < demo.count; k++)
        {
            if ((demo.head + k) % demo.items.Length == i) live = true;
        }
        cells += (live ? demo.items[i].ToString() : "·") + " ";
    }

    Console.WriteLine($"    buffer  [ {cells}]  head={demo.head} tail={demo.tail} count={demo.count}");
    Console.Write("    in order ");
    for (int k = 0; k < demo.count; k++)
    {
        Console.Write(demo.items[(demo.head + k) % demo.items.Length] + " ");
    }
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine("  Notice tail has wrapped past head. The data is physically out of order");
    Console.WriteLine("  in the buffer but logically correct — that is the whole point.");
    Console.WriteLine();
    Console.WriteLine("  GROWING A WRAPPED BUFFER is the fiddly part: you cannot block-copy,");
    Console.WriteLine("  because the data straddles the end. Enqueue walks it in LOGICAL");
    Console.WriteLine("  order instead — bigger[i] = items[(head + i) % items.Length] —");
    Console.WriteLine("  which unwraps it, then resets head to 0 and tail to count.");
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

    Queues queue = new Queues();
    long before;

    before = queue.steps;
    for (int i = 1; i <= 9; i++) queue.Enqueue(i * 10);
    Console.WriteLine($"    Enqueue(10..90) — nine adds             cost {queue.steps - before,6} step(s)");
    Console.WriteLine($"          head={queue.head} tail={queue.tail} count={queue.count} capacity={queue.items.Length}");
    Console.WriteLine("          One step each, plus three resizes.");
    Console.WriteLine();

    before = queue.steps;
    int front = queue.Peek();
    Console.WriteLine($"    Peek() = {front}                            cost {queue.steps - before,6} step(s)");
    Console.WriteLine();

    before = queue.steps;
    queue.Dequeue();
    Console.WriteLine($"    Dequeue()                               cost {queue.steps - before,6} step(s)");
    Console.WriteLine($"          head={queue.head} tail={queue.tail} count={queue.count}");
    Console.WriteLine("          head just moved forward. NOTHING was shifted — this is the");
    Console.WriteLine("          whole reason the circular buffer exists.");
    Console.WriteLine();

    before = queue.steps;
    for (int i = 0; i < 5; i++) queue.Dequeue();
    Console.WriteLine($"    Dequeue() x5                            cost {queue.steps - before,6} step(s)");
    Console.WriteLine($"          head={queue.head} tail={queue.tail} count={queue.count}");
    Console.WriteLine("          Still one step each, at any size.");
    Console.WriteLine();

    Console.WriteLine("    Dequeue() on an empty queue:");
    Queues empty = new Queues();
    try
    {
        empty.Dequeue();
    }
    catch (InvalidOperationException error)
    {
        Console.WriteLine($"          threw InvalidOperationException — \"{error.Message}\"");
    }
    Console.WriteLine();

    Console.WriteLine("    FIFO in one line — enqueue 1,2,3 then drain:");
    Queues order = new Queues();
    order.Enqueue(1);
    order.Enqueue(2);
    order.Enqueue(3);
    string drained = "";
    while (order.count > 0) drained += order.Dequeue() + " ";
    Console.WriteLine($"          {drained} — first in, first out (a stack would give 3 2 1)");
}


// =============================================================================
// COMPLEXITY — one method per operation, each measuring itself
// =============================================================================

void ShowDequeueCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — Dequeue(), circular buffer");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        Queues queue = new Queues();
        for (int i = 0; i < sizes[s]; i++) queue.Enqueue(i);

        queue.steps = 0;
        queue.Dequeue();
        work[s] = queue.steps;
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
    Console.WriteLine("  Flat. One read, one modulo, one decrement — regardless of size.");
}

void ShowNaiveDequeueCost()
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  COMPLEXITY — the NAIVE dequeue, for contrast");
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();
    Console.WriteLine("  Same operation, implemented by shifting everything left instead of");
    Console.WriteLine("  moving an index. This is what the circular buffer exists to avoid.");
    Console.WriteLine();

    int[] sizes = [1000, 2000, 4000, 8000, 16000];
    long[] work = new long[sizes.Length];
    long max = 0;

    for (int s = 0; s < sizes.Length; s++)
    {
        // A deliberately naive queue: a plain array, dequeue from the front.
        int[] buffer = new int[sizes[s]];
        for (int i = 0; i < sizes[s]; i++) buffer[i] = i;
        int liveCount = sizes[s];

        long shifted = 0;
        for (int i = 0; i < liveCount - 1; i++)
        {
            buffer[i] = buffer[i + 1];
            shifted++;
        }

        work[s] = shifted;
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
    Console.WriteLine("  O(n) for ONE dequeue, so draining the queue is O(n^2). At n=16000 that");
    Console.WriteLine("  is 16000 moves per call versus 1 for the circular version. Identical");
    Console.WriteLine("  data, identical API — the index arithmetic is doing all the work.");
    Console.WriteLine();
}


// =============================================================================
// THE CLASS
//
// A circular-buffer queue, which is what .NET's Queue<T> is. Every operation
// carries its own code, including its own resize block.
// =============================================================================

public class Queues
{
    // One contiguous block, treated as a ring.
    public int[] items = new int[0];

    // Index of the next element to come OUT.
    public int head = 0;

    // Index of the next free slot to write INTO.
    public int tail = 0;

    // Needed because head == tail means empty AND full — they are otherwise
    // indistinguishable.
    public int count = 0;

    public long steps = 0;
    public long copies = 0;
    public int resizes = 0;

    // -------------------------------------------------------------------------
    // Enqueue — add to the back.
    //
    // O(1) amortised. Writes at `tail`, then wraps the index with modulo. The
    // resize is the only expensive case, and doubling makes it rare.
    // -------------------------------------------------------------------------
    public void Enqueue(int value)
    {
        if (count == items.Length)
        {
            int newCapacity = items.Length == 0 ? 4 : items.Length * 2;
            int[] bigger = new int[newCapacity];

            // Cannot block-copy: the live data may WRAP past the end of the
            // buffer. Walk it in logical order instead, which unwraps it.
            for (int i = 0; i < count; i++)
            {
                bigger[i] = items[(head + i) % items.Length];
                copies++;
                steps++;
            }

            items = bigger;
            head = 0;          // unwrapped, so the front is back at 0
            tail = count;
            resizes++;
        }

        items[tail] = value;
        tail = (tail + 1) % items.Length;   // the wrap
        count++;
        steps++;
    }

    // -------------------------------------------------------------------------
    // Dequeue — take from the front.
    //
    // O(1). This is the whole reason the circular buffer exists: head simply
    // moves forward. A naive array queue would shift every element, making this
    // O(n) and draining the queue O(n^2).
    // -------------------------------------------------------------------------
    public int Dequeue()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("the queue is empty");
        }

        int value = items[head];

        // Blank the vacated slot — on a Queue<string> this is what lets the GC
        // reclaim the dequeued object.
        items[head] = 0;

        head = (head + 1) % items.Length;   // the wrap
        count--;
        steps++;

        return value;
    }

    // -------------------------------------------------------------------------
    // Peek — look at the front without removing it.
    //
    // O(1). One read at items[head].
    // -------------------------------------------------------------------------
    public int Peek()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("the queue is empty");
        }

        steps++;
        return items[head];
    }

    // -------------------------------------------------------------------------
    // Clear — empty the queue.
    //
    // O(n) to blank the live slots, walking in logical order because the data
    // may wrap. The buffer is kept, so refilling costs no reallocations.
    // -------------------------------------------------------------------------
    public void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            items[(head + i) % items.Length] = 0;
            steps++;
        }

        head = 0;
        tail = 0;
        count = 0;
    }
}
