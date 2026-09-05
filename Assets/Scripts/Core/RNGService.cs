using System.Collections.Generic;
public class RNGService
{
    private readonly System.Random _random;

    public RNGService(int? seed = null)
    {
        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

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

