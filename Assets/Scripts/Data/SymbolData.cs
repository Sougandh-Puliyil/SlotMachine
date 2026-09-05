using UnityEngine;
[CreateAssetMenu(fileName = "NewSymbol", menuName = "SlotGame/Symbol Data")]
public class SymbolData : ScriptableObject
{
    [Tooltip("Unique identifier used for win comparisons (e.g. 'cherry', 'seven')")]
    public string symbolId;

    [Tooltip("Sprite shown on the reel")]
    public Sprite icon;

    [Tooltip("Payout multiplier applied to the bet when all reels land on this symbol")]
    public float payoutMultiplier = 1f;

    [Tooltip("Relative weight used by the RNG - higher weight = more common on the reel")]
    [Range(1, 100)]
    public int spawnWeight = 10;

    [Tooltip("Marks this as a bonus symbol for special (non full-match) payouts")]
    public bool isBonusSymbol = false;
}
