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
        switch (statKind)
        {
            case StatKind.HP:
                if (changed == "hp" || changed == "maxHP") RefreshHP();
                break;
            case StatKind.MP:
                if (changed == "mp" || changed == "maxMP") RefreshMP();
                break;
            case StatKind.XP:
                if (changed == "xp" || changed == "xpToNext" || changed == "level") RefreshXP();
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
        slider.maxValue = stats.MaxHP;
        slider.value = stats.HP;
        if (label) label.text = $"{stats.HP} / {stats.MaxHP}";
    }

    void RefreshMP()
    {
        if (stats == null || slider == null) return;
        slider.maxValue = stats.MaxMP;
        slider.value = stats.MP;
        if (label) label.text = $"{stats.MP} / {stats.MaxMP}";
    }

    void RefreshXP()
    {
        if (stats == null || slider == null) return;
        slider.maxValue = stats.xpToNext;
        slider.value = stats.xp;
        if (label) label.text = $"{stats.xp} / {stats.xpToNext}";
    }
}
