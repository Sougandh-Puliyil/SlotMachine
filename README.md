# Slot Machine Game (Unity Assignment)

## Game Overview
A slot machine built in Unity. Each reel independently spins and settles
on a symbol chosen by an RNG (Random Number Generator). The player wins the full jackpot when all
reels land on the same symbol, and receives a smaller bonus payout when 2+
"bonus" symbols appear even without a full match.
side note: You cannot Exit the game using the exit button as the web browsers blocks them...

- **Engine:** Unity 6000.x (URP/2D)
- **Reels:** 3
- **Win condition:** All reels show matching symbols
- **Bonus feature:** Scatter-style bonus symbol payout (2+ bonus symbols = partial win)

## How to Run the WebGL Build
1. Clone this repository.
2. Serve the folder locally (WebGL builds often need a server, not `file://`):
   ```
   cd Build/WebGL
   python3 -m http.server 8000
   ```
   Then open `http://localhost:8000` in your browser.
3. Click **SPIN** to play.

## Project Structure
```
Assets/
  Scripts/
    Data/     - SymbolData ScriptableObject (symbol sprite, payout, weight)
    Core/     - RNGService, SlotMachineManager (game logic)
    Reel/     - Reel (spin animation + result)
  Symbols/    - Symbols used in the game
  Scenes/     - Scenes existing in the game
  TextMeshPro - An add-on used for the making of the game

```

## Bonus Features
- Weighted RNG per symbol (rare symbols are genuinely rare, not uniform-random)
- Staggered reel-stop animation (reels don't freeze in unison)
- Bonus/scatter symbol partial payout even without a full match

## Thought Process / Approach
1. Decoupled the RNG result from the animation: the winning symbol is locked in the instant Spin() is called, so the spinning visuals never affect fairness - they're just "showtelling" the true result.
2. Used a ScriptableObject (`SymbolData`) for symbols instead of a hardcoded enum, so payouts/weights can be tuned by a designer without touching code.
3. `SlotMachineManager` is the single source of truth for win/payout logic; `Reel` only knows how to spin and report its own result - keeping each class focused on one responsibility.
4. Compared symbols by `symbolId` string rather than sprite reference, so swapping art assets never breaks win detection.
