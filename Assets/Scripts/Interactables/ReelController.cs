using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReelController : MonoBehaviour
{
    [Header("Wiring")]
    public Image display;

    [Header("Symbols")]
    public List<Sprite> symbols = new List<Sprite>();

    [Header("Spin Feel")]
    [Tooltip("How fast we cycle at full speed (frames per second).")]
    public float fastFps = 24f;
    [Tooltip("How fast we cycle at the slowest phase (frames per second).")]
    public float slowFps = 8f;
    [Tooltip("How long (seconds) we spend in each phase: fast → medium → slow.")]
    public Vector3 phaseDurations = new Vector3(0.5f, 0.5f, 0.6f);

    int _currentIndex;
    Coroutine _spinRoutine;

    public bool IsSpinning => _spinRoutine != null;

    void Reset()
    {
        display = GetComponent<Image>();
    }

    public int GetCurrentIndex() => _currentIndex;

    /// <summary>
    /// Spins the reel and lands on finalIndex. Returns the chosen index (for convenience).
    /// </summary>
    public int SpinTo(int finalIndex)
    {
        if (symbols == null || symbols.Count == 0)
        {
            Debug.LogWarning("[ReelController] No symbols assigned.");
            return -1;
        }

        finalIndex = Mathf.Clamp(finalIndex, 0, symbols.Count - 1);

        if (_spinRoutine != null)
        {
            StopCoroutine(_spinRoutine);
        }
        _spinRoutine = StartCoroutine(SpinRoutine(finalIndex));
        return finalIndex;
    }

    IEnumerator SpinRoutine(int finalIndex)
    {
        // 3-phase spin: fast → medium → slow, then land exactly on finalIndex.
        float[] fps = new float[] { fastFps, Mathf.Lerp(fastFps, slowFps, 0.5f), slowFps };
        float[] durations = new float[] { phaseDurations.x, phaseDurations.y, phaseDurations.z };

        for (int phase = 0; phase < 3; phase++)
        {
            float t = 0f;
            float dt = 1f / Mathf.Max(1f, fps[phase]);

            while (t < durations[phase])
            {
                NextSymbol();
                yield return new WaitForSeconds(dt);
                t += dt;
            }
        }

        // Nudge to the exact final symbol with a few gentle ticks.
        int safety = 0;
        while (_currentIndex != finalIndex && safety++ < symbols.Count + 5)
        {
            NextSymbol();
            yield return new WaitForSeconds(1f / Mathf.Max(1f, slowFps));
        }

        // Ensure exact sprite set.
        _currentIndex = finalIndex;
        display.sprite = symbols[_currentIndex];

        _spinRoutine = null;
    }

    void NextSymbol()
    {
        if (symbols.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % symbols.Count;
        if (display) display.sprite = symbols[_currentIndex];
    }
}
