using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachine : MonoBehaviour
{
    [Header("Reels")]
    [Tooltip("Assign all reels left-to-right in the Inspector")]
    public Reel[] reels;

    [Header("Economy")]
    public float startingBalance = 100f;
    public float betAmount = 10f;

    [Header("UI")]
    public Button spinButton;
    public Text balanceText;
    public Text messageText;

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

        spinButton.onClick.AddListener(OnSpinPressed);
        UpdateBalanceUI();
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
        spinButton.interactable = false;

        for (int i = 0; i < reels.Length; i++)
        {
            // Stagger each reel's stop slightly so they don't freeze in unison.
            float extraDelay = i * 0.25f;
            reels[i].Spin(extraDelay, OnReelStopped);
        }
    }

    private void OnReelStopped()
    {
        _reelsFinished++;
        if (_reelsFinished < reels.Length) return; // wait for all reels

        _isSpinning = false;
        spinButton.interactable = true;
        EvaluateResult();
    }

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

        if (allMatch)
        {
            float payout = betAmount * results[0].payoutMultiplier;
            _balance += payout;
            messageText.text = $"JACKPOT! All {results[0].symbolId} - Won {payout:F0}!";
        }
        else if (bonusCount >= 2)
        {
            float payout = betAmount * 0.5f * bonusCount;
            _balance += payout;
            messageText.text = $"Bonus symbols! Won {payout:F0}!";
        }
        else
        {
            messageText.text = "No match - try again!";
        }

        UpdateBalanceUI();
    }

    private void UpdateBalanceUI()
    {
        balanceText.text = $"Balance: {_balance:F0}";
    }
}
