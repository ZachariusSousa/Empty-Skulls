using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatBars : MonoBehaviour
{
    public enum StatKind { HP, MP, XP }

    [Header("Wiring")]
    public PlayerStats stats;
    public StatKind statKind = StatKind.HP;
    public Slider slider;
    public TextMeshProUGUI label;

    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    void Awake()
    {
        // Auto-find PlayerStats if not assigned
        if (autoFindPlayerByTag && stats == null)
        {
            var t = GameObject.FindGameObjectWithTag(playerTag);
            if (t) stats = t.GetComponent<PlayerStats>();
        }

        // Auto-grab the Slider if on same object
        if (slider == null)
            slider = GetComponent<Slider>();

        // Auto-find TMP label if child named "Text"
        if (label == null)
        {
            var textChild = transform.Find("Text");
            if (textChild)
                label = textChild.GetComponent<TextMeshProUGUI>();
        }
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.onStatChanged.AddListener(OnStatChanged);
            RefreshAll();
        }
    }

    void OnDisable()
    {
        if (stats != null)
            stats.onStatChanged.RemoveListener(OnStatChanged);
    }

    void OnStatChanged(string changed, int _)
    {
        // Update only what this bar cares about
        switch (statKind)
        {
            case StatKind.HP:
                // respond to HP changes and to either base or effective max changes
                if (changed == "hp" || changed == "maxHP" || changed == "maxHP_eff")
                    RefreshHP();
                break;

            case StatKind.MP:
                if (changed == "mp" || changed == "maxMP" || changed == "maxMP_eff")
                    RefreshMP();
                break;

            case StatKind.XP:
                if (changed == "xp" || changed == "xpToNext" || changed == "level")
                    RefreshXP();
                break;
        }
    }

    void RefreshAll()
    {
        switch (statKind)
        {
            case StatKind.HP: RefreshHP(); break;
            case StatKind.MP: RefreshMP(); break;
            case StatKind.XP: RefreshXP(); break;
        }
    }

    void RefreshHP()
    {
        if (stats == null || slider == null) return;

        // Use effective max so equipment/auras resize the bar
        var max = stats.EffMaxHP;
        var cur = stats.HP;

        slider.maxValue = max;
        slider.value = cur;

        if (label)
            label.text = $"{cur} / {max}";
    }

    void RefreshMP()
    {
        if (stats == null || slider == null) return;

        var max = stats.EffMaxMP;
        var cur = stats.MP;

        slider.maxValue = max;
        slider.value = cur;

        if (label)
            label.text = $"{cur} / {max}";
    }

    void RefreshXP()
    {
        if (stats == null || slider == null) return;

        slider.maxValue = stats.xpToNext;
        slider.value = stats.xp;

        if (label)
            label.text = $"{stats.xp} / {stats.xpToNext}";
    }
}
