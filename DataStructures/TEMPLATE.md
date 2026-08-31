# <Data structure name>

> Copy this file, fill every section, delete these quote lines. This folder answers
> *what is it and how does it work* — algorithms that use it live under `Algorithms/`.

## 1. Why was it invented?

> What did people do before it, and what was unbearable about that?

## 2. What problem does it solve?

> One sentence. The single job it exists to do well.

## 3. What is the trade?

> Every structure makes one thing fast by making something else slow. Name both.
> If you can name the trade you know when to reach for it.

**Fast:**
**Paid for with:**

## 4. How is it stored in memory?

> The most important section. See `Memory.md` in this folder for the full detail —
> summarise the picture here.

```
STACK                 HEAP
...draw it...
```

**Cost of one element:** ___ bytes of data, ___ bytes of overhead

## 5. How is it implemented internally?

> The mechanics: what fields exist, what happens on insert, on grow, on delete.

## 6. Visual model

> The physical analogy you will actually recall under pressure, plus a diagram.
> Silly is fine. Memorable beats precise.

**Picture:**
**What it explains:**

## 7. Operations

| Operation | Best | Average | Worst | Space | **Why** |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

> The **Why** column is the point. If you cannot fill it, you have memorised the row
> rather than understood it. Be precise about *worst* vs *average* vs *amortised*.

## 8. Strengths

## 9. Weaknesses

## 10. When should I NOT use it?

| Situation | Use instead |
| --- | --- |
| | |

> The usual giveaway is an O(n) operation inside a loop, making the whole thing O(n²).
> That is almost always the wrong structure rather than the wrong code.

## 11. What does .NET actually ship?

> You will use the BCL type at work. How does it differ from yours — growth factor,
> collision strategy, thread safety, surprising API behaviour?

## 12. Implementation

- [ ] Built from scratch, no BCL collections → `<Name>.cs`
- [ ] Rebuilt from memory a week later

## 13. Unit tests

- [ ] Empty
- [ ] Single element
- [ ] Growth / resize boundary
- [ ] Remove from front, middle, end
- [ ] Duplicates
- [ ] Operation on empty throws correctly
- [ ] References released on removal (no leak)

## 14. Benchmark

| Operation | n | steps | ms |
| --- | --- | --- | --- |
| | | | |

## 15. Recall check

- Where does one element actually live, and what does it cost in bytes?
- Which operation is expensive, and which memory property causes that?
- What would you reach for instead, and when?
