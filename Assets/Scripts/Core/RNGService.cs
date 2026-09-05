using System.Collections.Generic;

// Wraps System.Random (not UnityEngine.Random) so win-deciding rolls stay separate and seedable from the cosmetic reel-scramble animation.
public class RNGService
{
    private readonly System.Random _random;

    // seed = null gives real randomness each run; a fixed number gives the same sequence every time, for testing.
    public RNGService(int? seed = null)
    {
        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    // Picks a symbol with odds proportional to its spawnWeight (higher weight = bigger "slice" of the roll range = picked more often).
    public SymbolData GetWeightedRandomSymbol(List<SymbolData> pool)
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

        // Fallback - only reached due to float rounding edge cases
        return pool[pool.Count - 1];
    }
}
