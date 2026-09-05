using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Reel : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Image components representing the visible symbol slots on this reel (top to bottom). Use an odd number (e.g. 3) so there's a clear middle/payline slot.")]
    public Image[] visibleSlots;

    [Tooltip("All symbols this reel can display")]
    public List<SymbolData> symbolPool;

    [Header("Animation")]
    [Tooltip("How fast symbols cycle at full speed")]
    public float cycleRate = 18f; // swaps per second at peak speed
    public float minSpinDuration = 1.2f;
    public AnimationCurve stopEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RNGService _rng;
    private SymbolData _resultSymbol;
    public SymbolData ResultSymbol => _resultSymbol;

    public void Init(RNGService rng)
    {
        _rng = rng;
    }

    public Coroutine Spin(float extraDelay, Action onComplete)
    {
        return StartCoroutine(SpinRoutine(extraDelay, onComplete));
    }

    private IEnumerator SpinRoutine(float extraDelay, Action onComplete)
    {

        _resultSymbol = _rng.GetWeightedRandomSymbol(symbolPool);

        float duration = minSpinDuration + extraDelay;
        float elapsed = 0f;
        float swapTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float currentRate = cycleRate * (1f - stopEase.Evaluate(t));
            swapTimer += Time.deltaTime;

            if (currentRate > 0f && swapTimer >= 1f / currentRate)
            {
                swapTimer = 0f;
                foreach (var slot in visibleSlots)
                {
                    slot.sprite = symbolPool[UnityEngine.Random.Range(0, symbolPool.Count)].icon;
                }
            }

            yield return null;
        }

        SnapToFinalSymbols();
        onComplete?.Invoke();
    }

    private void SnapToFinalSymbols()
    {
        int middle = visibleSlots.Length / 2;
        visibleSlots[middle].sprite = _resultSymbol.icon;

        for (int i = 0; i < visibleSlots.Length; i++)
        {
            if (i == middle) continue;
            visibleSlots[i].sprite = symbolPool[UnityEngine.Random.Range(0, symbolPool.Count)].icon;
        }
    }
}
