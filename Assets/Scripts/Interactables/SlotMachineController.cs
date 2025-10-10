using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineController : MonoBehaviour
{
    [Header("Wiring")]
    public ReelController[] reels;
    public Button spinButton;

    [Header("Timings")]
    [Tooltip("Delay between each reel start (seconds) for a nice cascade effect).")]
    public float reelStagger = 0.25f;

    [Header("Randomness")]
    public bool allowDuplicates = true;

    void Awake()
    {
        if (spinButton != null)
            spinButton.onClick.AddListener(OnPressSpin);
    }

    void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(OnPressSpin);
    }

    void OnPressSpin()
    {
        if (reels == null || reels.Length == 0) return;

        // Don’t allow multiple concurrent spins.
        foreach (var r in reels) if (r && r.IsSpinning) return;

        if (spinButton) spinButton.interactable = false;
        StartCoroutine(SpinAll());
    }

    IEnumerator SpinAll()
    {
        int[] results = new int[reels.Length];

        // Choose target indices first (so we can add logic later, like weighted odds).
        for (int i = 0; i < reels.Length; i++)
        {
            var r = reels[i];
            if (!r || r.symbols.Count == 0) { results[i] = -1; continue; }

            int choice;
            if (allowDuplicates)
            {
                choice = Random.Range(0, r.symbols.Count);
            }
            else
            {
                // Simple “no duplicates” approach: re-roll until unique among chosen so far.
                // Fine for 3 reels; for many reels, switch to a set/knuth shuffle.
                int guard = 0;
                do
                {
                    choice = Random.Range(0, r.symbols.Count);
                    guard++;
                } while (Contains(results, choice, i) && guard < 50);
            }
            results[i] = choice;
        }

        // Stagger start
        for (int i = 0; i < reels.Length; i++)
        {
            var r = reels[i];
            if (r) r.SpinTo(results[i]);
            if (i < reels.Length - 1) yield return new WaitForSeconds(reelStagger);
        }

        // Wait for all reels to finish
        bool anySpinning;
        do
        {
            anySpinning = false;
            foreach (var r in reels) if (r && r.IsSpinning) { anySpinning = true; break; }
            yield return null;
        } while (anySpinning);

        // Hook for reward logic later:
        OnSpinResult(results);

        if (spinButton) spinButton.interactable = true;
    }

    void OnSpinResult(int[] indices)
    {
        // Placeholder: you’ll add your payout logic here later.
        // Example: check for 3 of a kind, pairs, special symbol in middle, etc.
        // For now we just log.
        Debug.Log($"Spin result: [{string.Join(",", indices)}]");
    }

    static bool Contains(int[] arr, int value, int countToCheck)
    {
        for (int i = 0; i < countToCheck; i++)
            if (arr[i] == value) return true;
        return false;
    }
}
