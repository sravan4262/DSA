#!/usr/bin/env dotnet
// =============================================================================
// DIFFERENCE ARRAY — why storing the GAPS lets you skip writing the middle.
//
//   dotnet run DifferenceArrayPractice.cs
//
// Every number printed below is COMPUTED, not typed in. If the explanation and
// the arithmetic ever disagree, the program is the one telling the truth.
// =============================================================================

Explain();
BruteForce();


void Explain()
{
    // a difference array is similar to prefix sum, we store the differnce between adjacent elements
    // let's take an array of [1,3,5,3]
    // now difference array would be [1,2,2,-2]
    // now add 10 for index of (1 thru 3)
    // now the new difference array would be [1,12,12,10]
    // now the prefix sum would be [1,13,25,35]
}
void BruteForce()
{
    // TODO
}
