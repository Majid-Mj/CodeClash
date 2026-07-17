using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        for (int n = 2; n <= 8; n++)
        {
            Console.WriteLine($"=== TESTING SEEDING FOR N = {n} PARTICIPANTS ===");
            TestSeeding(n);
            Console.WriteLine();
        }
    }

    static void TestSeeding(int n)
    {
        // Simulate players list sorted by rating descending
        var sortedPlayers = new List<string>();
        for (int i = 1; i <= n; i++)
        {
            sortedPlayers.Add($"Player_Seed_{i}");
        }

        int powerOfTwo = 1;
        while (powerOfTwo < n) powerOfTwo *= 2;

        int byes = powerOfTwo - n;
        Console.WriteLine($"Bracket Size: {powerOfTwo}, Byes: {byes}");

        // Generate standard seeding order of size powerOfTwo
        var seedOrder = new List<int> { 1 };
        while (seedOrder.Count < powerOfTwo)
        {
            int nextSize = seedOrder.Count * 2;
            var nextOrder = new List<int>();
            foreach (var seed in seedOrder)
            {
                nextOrder.Add(seed);
                nextOrder.Add(nextSize + 1 - seed);
            }
            seedOrder = nextOrder;
        }

        Console.WriteLine($"Seed Order: [{string.Join(", ", seedOrder)}]");

        // Create first round matches
        int matchCount = powerOfTwo / 2;
        for (int i = 0; i < matchCount; i++)
        {
            int leftSeed = seedOrder[2 * i];
            int rightSeed = seedOrder[2 * i + 1];

            var p1 = leftSeed <= n ? sortedPlayers[leftSeed - 1] : "BYE";
            var p2 = rightSeed <= n ? sortedPlayers[rightSeed - 1] : "BYE";

            Console.WriteLine($"  Match {i + 1}: {p1} vs {p2}");
        }
    }
}
