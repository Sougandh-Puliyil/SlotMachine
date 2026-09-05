using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-level game controller. Owns the reels, RNG service, and player balance,
/// and is the single source of truth for win/payout evaluation. Reel and UI
/// classes stay "dumb" (no gameplay logic) so they're reusable and easy to test.
/// </summary>
public class SlotMachine : MonoBehaviour
{
    [Header("Reels")]
    [Tooltip("Assign all reels left-to-right in the Inspector")]
    public Reel[] reels;

    [Header("Economy")]
    public float startingBalance = 100f;
    public float betAmount = 10f;

    [Header("Spin Trigger (the lever)")]
    [Tooltip("The Button component on the Lever GameObject - clicking/pulling it spins the reels")]
    public Button leverButton;

    [Tooltip("Optional: the lever's Image component, swapped to a 'pulled' sprite briefly on spin for visual feedback")]
    public Image leverImage;
    public Sprite leverUpSprite;
    public Sprite leverDownSprite;
    public float leverPulldownDuration = 0.3f;

    [Header("Core UI")]
    public Text balanceText;
    public Text messageText; // kept as a lightweight fallback/status line

    [Header("Bet Menu")]
    [Tooltip("The whole bet menu panel - hidden entirely (not just disabled) while the lever is pulled")]
    public GameObject betMenuPanel;
    public Button bet10Button;
    public Button bet50Button;
    public Button bet100Button;
    public Button exitButton;
    public Text betAmountText; // optional: shows the currently selected bet

    private void SetBetMenuVisible(bool visible)
    {
        // Hides the entire panel (not just interactable=false) so it's out of the
        // way visually while the reels are spinning, matching the reference mockup
        // where the bet menu only appears before/after a spin, not during one.
        if (betMenuPanel != null) betMenuPanel.SetActive(visible);
    }

    [Header("Win Popup")]
    [Tooltip("The popup panel GameObject shown after every spin result (win or lose)")]
    public GameObject resultPopup;
    public Text resultPopupText;
    public Button resultPopupCloseButton; // reuse a sliced button (e.g. the 'X' or 'YES' sprite) as an OK/dismiss button

    [Tooltip("Seconds to wait after the reels stop before the result popup appears, so the player has a moment to see the landed symbols first")]
    public float resultPopupDelay = 1.2f;

    [Header("RNG (debug)")]
    [Tooltip("Leave at -1 for a real random seed each run. Set to a fixed value to reproduce a specific spin sequence while testing win logic.")]
    public int debugSeed = -1;

    private RNGService _rng;
    private float _balance;
    private int _reelsFinished;
    private bool _isSpinning;

    private void Awake()
    {
        _rng = debugSeed >= 0 ? new RNGService(debugSeed) : new RNGService();
        _balance = startingBalance;

        foreach (var reel in reels)
        {
            reel.Init(_rng);
        }

        // The lever is the single spin trigger - no separate "Spin" button.
        leverButton.onClick.AddListener(OnSpinPressed);

        // Bet menu buttons each set a fixed bet value.
        if (bet10Button != null) bet10Button.onClick.AddListener(() => SetBet(10f));
        if (bet50Button != null) bet50Button.onClick.AddListener(() => SetBet(50f));
        if (bet100Button != null) bet100Button.onClick.AddListener(() => SetBet(100f));
        if (exitButton != null) exitButton.onClick.AddListener(OnExitPressed);

        if (resultPopupCloseButton != null)
            resultPopupCloseButton.onClick.AddListener(OnResultPopupClosed);

        if (resultPopup != null) resultPopup.SetActive(false);

        UpdateBalanceUI();
        UpdateBetUI();
    }

    /// <summary>
    /// Called by the bet menu buttons (Bet 10G / Bet 50G / Bet 100G).
    /// Ignored mid-spin so the player can't change their bet after committing to a spin.
    /// </summary>
    public void SetBet(float amount)
    {
        if (_isSpinning) return;
        betAmount = amount;
        UpdateBetUI();
    }

    /// <summary>
    /// Called by the Exit button. NOTE: Application.Quit() is a no-op in WebGL builds
    /// (browsers block a page from closing itself), so this only does something
    /// meaningful in a standalone build. For WebGL, consider hiding/disabling this
    /// button, or replacing its behaviour with something like resetting the balance.
    /// </summary>
    public void OnExitPressed()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Exit pressed - Application.Quit() does nothing in WebGL builds.");
        #else
        Application.Quit();
        #endif
    }

    private void OnSpinPressed()
    {
        if (_isSpinning) return;

        if (_balance < betAmount)
        {
            messageText.text = "Not enough balance!";
            return;
        }

        _balance -= betAmount;
        UpdateBalanceUI();

        _isSpinning = true;
        _reelsFinished = 0;
        messageText.text = "";
        leverButton.interactable = false;
        SetBetMenuVisible(false);

        if (leverImage != null && leverDownSprite != null && leverUpSprite != null)
        {
            StartCoroutine(LeverPullAnimation());
        }

        for (int i = 0; i < reels.Length; i++)
        {
            // Stagger each reel's stop slightly so they don't freeze in unison.
            float extraDelay = i * 0.25f;
            reels[i].Spin(extraDelay, OnReelStopped);
        }
    }

    /// <summary>
    /// Purely visual: swaps the lever sprite to its "pulled down" pose briefly,
    /// then back to "up", giving tactile feedback that the spin was registered.
    /// </summary>
    private IEnumerator LeverPullAnimation()
    {
        leverImage.sprite = leverDownSprite;
        yield return new WaitForSeconds(leverPulldownDuration);
        leverImage.sprite = leverUpSprite;
    }

    private void OnReelStopped()
    {
        _reelsFinished++;
        if (_reelsFinished < reels.Length) return; // wait for all reels

        _isSpinning = false;
        leverButton.interactable = true;

        // Balance updates immediately (no suspense needed for the numbers),
        // but the popup itself is delayed so the player has a moment to look
        // at the actual landed symbols before a popup covers the view.
        EvaluateResult();
    }

    /// <summary>
    /// Win condition: every reel's result symbol matches (symbolId comparison,
    /// not sprite reference, so this stays robust even if sprites are swapped).
    /// Bonus feature: 2+ bonus symbols pay a smaller consolation prize even
    /// without a full match, inspired by classic "scatter" bonuses.
    /// Every result (win, bonus, or loss) is shown in the result popup for clarity.
    /// </summary>
    private void EvaluateResult()
    {
        var results = new List<SymbolData>();
        foreach (var reel in reels) results.Add(reel.ResultSymbol);

        bool allMatch = true;
        for (int i = 1; i < results.Count; i++)
        {
            if (results[i].symbolId != results[0].symbolId)
            {
                allMatch = false;
                break;
            }
        }

        int bonusCount = results.FindAll(s => s.isBonusSymbol).Count;
        string resultMessage;

        if (allMatch)
        {
            float payout = betAmount * results[0].payoutMultiplier;
            _balance += payout;
            resultMessage = $"JACKPOT!\nAll {results[0].symbolId}\nWon {payout:F0}!";
        }
        else if (bonusCount >= 2)
        {
            float payout = betAmount * 0.5f * bonusCount;
            _balance += payout;
            resultMessage = $"Bonus symbols!\nWon {payout:F0}!";
        }
        else
        {
            resultMessage = "No match.\nTry again!";
        }

        messageText.text = resultMessage.Replace("\n", " ");
        UpdateBalanceUI(); // balance itself updates immediately - only the popup is delayed

        StartCoroutine(ShowResultPopupAfterDelay(resultMessage));
    }

    /// <summary>
    /// Waits a moment after the reels stop (so the player can actually see the
    /// landed symbols) before showing the result popup on top of them.
    /// </summary>
    private IEnumerator ShowResultPopupAfterDelay(string message)
    {
        yield return new WaitForSeconds(resultPopupDelay);

        if (resultPopup == null || resultPopupText == null) yield break;
        resultPopupText.text = message;
        resultPopup.SetActive(true);
    }

    /// <summary>
    /// Called by the popup's close/OK button. Dismisses the popup and brings
    /// the bet menu back so the player can spin again.
    /// </summary>
    private void OnResultPopupClosed()
    {
        resultPopup.SetActive(false);
        SetBetMenuVisible(true);
    }

    private void UpdateBalanceUI()
    {
        balanceText.text = $"Balance: {_balance:F0}";
    }

    private void UpdateBetUI()
    {
        if (betAmountText != null) betAmountText.text = $"Bet: {betAmount:F0}";
    }
}
