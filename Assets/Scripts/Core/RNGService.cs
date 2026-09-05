using System.Collections.Generic;


public class RNGService // Wraps System.Random so win-deciding rolls stay separate and seedable from the cosmetic reel-scramble animation.
{
    private readonly System.Random _random;


    public RNGService(int? seed = null) // seed = null gives real randomness each run; a fixed number gives the same sequence every time, for testing.
    {
        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public SymbolData GetWeightedRandomSymbol(List<SymbolData> pool)    // Picks a symbol with odds proportional to its spawnWeight (higher weight = bigger "slice" of the roll range = picked more often).
    {
        int totalWeight = 0;
        foreach (var s in pool) totalWeight += s.spawnWeight;

        int roll = _random.Next(0, totalWeight);
        int cumulative = 0;

        foreach (var s in pool)
        {
            cumulative += s.spawnWeight;
            if (roll < cumulative) return s;
        }

        return pool[pool.Count - 1];     // Fallback - only reached due to float rounding edge cases
    }
}
