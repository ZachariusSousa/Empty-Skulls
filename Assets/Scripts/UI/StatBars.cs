using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatBars : MonoBehaviour
{
    public enum StatKind { HP, MP, XP }

    [Header("Wiring")]
    public PlayerStats player;              // single source of truth
    public StatKind statKind = StatKind.HP;
    public Slider slider;
    public TextMeshProUGUI label;

    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    void Awake()
    {
        if (autoFindPlayerByTag && player == null)
        {
            var t = GameObject.FindGameObjectWithTag(playerTag);
            if (t) player = t.GetComponent<PlayerStats>();
        }

        if (slider == null) slider = GetComponent<Slider>();

        if (label == null)
        {
            var textChild = transform.Find("Text");
            if (textChild) label = textChild.GetComponent<TextMeshProUGUI>();
        }
    }

    void OnEnable()
    {
        if (player != null)
        {
            player.onStatChanged.AddListener(OnStatChanged);
            RefreshAll();
        }
    }

    void OnDisable()
    {
        if (player != null)
            player.onStatChanged.RemoveListener(OnStatChanged);
    }

    void OnStatChanged(string changed, int _)
    {
        switch (statKind)
        {
            case StatKind.HP:
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
        if (player == null || slider == null) return;
        int max = player.EffMaxHP;
        int cur = player.HP;

        slider.maxValue = Mathf.Max(1, max);
        slider.value = Mathf.Clamp(cur, 0, max);

        if (label) label.text = $"{slider.value} / {slider.maxValue}";
    }

    void RefreshMP()
    {
        if (player == null || slider == null) return;
        int max = player.EffMaxMP;
        int cur = player.MP; // uses property

        slider.maxValue = Mathf.Max(1, max);
        slider.value = Mathf.Clamp(cur, 0, max);

        if (label) label.text = $"{slider.value} / {slider.maxValue}";
    }

    void RefreshXP()
    {
        if (player == null || slider == null) return;
        int max = Mathf.Max(1, player.xpToNext);
        int cur = Mathf.Clamp(player.xp, 0, max);

        slider.maxValue = max;
        slider.value = cur;

        if (label) label.text = $"{slider.value} / {slider.maxValue}";
    }
}
