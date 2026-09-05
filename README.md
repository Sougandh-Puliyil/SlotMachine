# Slot Machine Game (Unity Assignment)

## Game Overview
A 3-reel slot machine built in Unity. Each reel independently spins and settles
on a symbol chosen by a weighted RNG. The player wins the full jackpot when all
reels land on the same symbol, and receives a smaller bonus payout when 2+
"bonus" symbols appear even without a full match.

- **Engine:** Unity 6000.x (URP/2D)
- **Reels:** 3
- **Win condition:** All reels show matching symbols
- **Bonus feature:** Scatter-style bonus symbol payout (2+ bonus symbols = partial win)

## How to Run the WebGL Build
1. Clone this repository.
2. Open `Build/WebGL/index.html` in a modern browser (Chrome/Firefox/Edge), OR
3. Serve the folder locally (WebGL builds often need a server, not `file://`):
   ```
   cd Build/WebGL
   python3 -m http.server 8000
   ```
   Then open `http://localhost:8000` in your browser.
4. Click **SPIN** to play. Starting balance and bet amount are configurable in
   the Unity Inspector on the `SlotMachineManager` component.

## Project Structure
```
Assets/
  Scripts/
    Data/     - SymbolData ScriptableObject (symbol sprite, payout, weight)
    Core/     - RNGService, SlotMachineManager (game logic)
    Reel/     - Reel (spin animation + result)
    UI/       - any UI-only helper scripts
  Prefabs/    - Reel prefab, Symbol slot prefab
  Animations/ - any Animator-driven win/lose effects
  UI/         - Sprites/fonts used in the canvas
  Sounds/     - spin/win/lose SFX (if added)
```

## Bonus Features
- Weighted RNG per symbol (rare symbols are genuinely rare, not uniform-random)
- Staggered reel-stop animation (reels don't freeze in unison)
- Bonus/scatter symbol partial payout even without a full match

## Thought Process / Approach
1. Decoupled the **RNG result** from the **animation**: the winning symbol is
   locked in the instant Spin() is called, so the spinning visuals never
   affect fairness - they're just "showtelling" the true result.
2. Used a ScriptableObject (`SymbolData`) for symbols instead of a hardcoded
   enum, so payouts/weights can be tuned by a designer without touching code.
3. `SlotMachineManager` is the single source of truth for win/payout logic;
   `Reel` only knows how to spin and report its own result - keeping each
   class focused on one responsibility.
4. Compared symbols by `symbolId` string rather than sprite reference, so
   swapping art assets never breaks win detection.
