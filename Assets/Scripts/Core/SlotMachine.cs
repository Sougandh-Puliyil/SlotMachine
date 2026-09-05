using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class SlotMachine : MonoBehaviour    // Top-level game controller: owns the reels, RNG, and balance, and is the only place that decides win/payout - Reel and UI scripts stay logic-free.
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


    private void SetBetMenuVisible(bool visible)     // Hides the bet menu while a spin is in progress.
    {
        if (betMenuPanel != null) betMenuPanel.SetActive(visible);
    }

    [Header("Win Popup")]
    [Tooltip("The popup panel GameObject shown after every spin result (win or lose)")]
    public GameObject resultPopup;
    public Text resultPopupText;
    public Button resultPopupCloseButton;

    [Tooltip("Seconds to wait after the reels stop before the result popup appears, so the player has a moment to see the landed symbols first")]
    public float resultPopupDelay = 1.2f;

    [Header("RNG (debug)")]
    [Tooltip("Leave at -1 for a real random seed each run. Set to a fixed value to reproduce a specific spin sequence while testing win logic.")]
    public int debugSeed = -1;

    private RNGService _rng;
    private float _balance;
    private int _reelsFinished;
    private bool _isSpinning;


    private void Awake()    // Runs once at startup: sets up the RNG, hands it to every reel, and wires up every button's click behaviour.
    {
        _rng = debugSeed >= 0 ? new RNGService(debugSeed) : new RNGService();
        Debug.Log($"[SlotMachine] RNG initialized with seed: {(debugSeed >= 0 ? debugSeed.ToString() : "random (no fixed seed)")}");
        _balance = startingBalance;

        foreach (var reel in reels)
        {
            reel.Init(_rng);
        }


        leverButton.onClick.AddListener(OnSpinPressed); //spin button

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


    public void SetBet(float amount)        // Called by the Bet 10G/50G/100G buttons; ignored mid-spin so the bet can't change after the lever's already been pulled.
    {
        if (_isSpinning) return;
        betAmount = amount;
        UpdateBetUI();
    }


    public void OnExitPressed()  // Called by the Exit button: stops Play mode in-editor, quits in a standalone build, and does nothing in WebGL (browsers block self-closing tabs).
    {
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
        Debug.Log("Exit pressed - Application.Quit() does nothing in WebGL builds (browser restriction).");
        #else
        Application.Quit();
        #endif
    }


    private void OnSpinPressed()    // Called when the lever is pulled: deducts the bet (or shows a "no money" popup instead) and starts every reel spinning.
    {
        if (_isSpinning) return;

        if (_balance < betAmount)
        {
            messageText.text = "Not enough balance!";
            ShowImmediatePopup("NO MONEY\nLower your bet or you're out of funds!");
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


    private IEnumerator LeverPullAnimation()
    {
        leverImage.sprite = leverDownSprite;
        yield return new WaitForSeconds(leverPulldownDuration);
        leverImage.sprite = leverUpSprite;
    }


    private void OnReelStopped()    // Called once per reel as it finishes; once all reels have reported in, evaluates the result.
    {
        _reelsFinished++;
        if (_reelsFinished < reels.Length) return; // wait for all reels

        _isSpinning = false;
        leverButton.interactable = true;
        EvaluateResult();
    }


    private void EvaluateResult()       // Checks whether all reels matched (jackpot) or 2+ bonus symbols landed (smaller payout), updates balance, and queues the result popup.
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


    private void ShowImmediatePopup(string message) // Shows the popup with no delay - used for "no money", where there's nothing on the reels worth waiting to look at first.
    {
        if (resultPopup == null || resultPopupText == null) return;
        resultPopupText.text = message;
        resultPopup.SetActive(true);
    }


    private IEnumerator ShowResultPopupAfterDelay(string message)    // Waits resultPopupDelay seconds after the reels stop (so the player sees the symbols first) before showing the result popup.
    {
        yield return new WaitForSeconds(resultPopupDelay);

        if (resultPopup == null || resultPopupText == null) yield break;
        resultPopupText.text = message;
        resultPopup.SetActive(true);
    }


    private void OnResultPopupClosed()  // Called by the popup's close button: hides the popup and brings the bet menu back so the player can spin again.
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
