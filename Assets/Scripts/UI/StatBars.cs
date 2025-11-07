using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatUI : MonoBehaviour
{
    public enum StatKind { HP, MP, XP }

    [Header("General")]
    public PlayerStats player;
    public StatKind statKind;

    [Header("Orb (HP / MP)")]
    public Image orbImage;          // The Image that changes sprite
    public Sprite[] orbFrames;      // Ordered from empty → full

    [Header("Slider (XP)")]
    public Slider xpSlider;
    public TextMeshProUGUI xpLabel;

    [Header("Options")]
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    void Awake()
    {
        if (autoFindPlayerByTag && player == null)
        {
            var t = GameObject.FindGameObjectWithTag(playerTag);
            if (t) player = t.GetComponent<PlayerStats>();
        }

        if (statKind == StatKind.XP && xpSlider == null)
            xpSlider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        if (player)
        {
            player.onStatChanged.AddListener(OnStatChanged);
            Refresh();
        }
    }

    void OnDisable()
    {
        if (player)
            player.onStatChanged.RemoveListener(OnStatChanged);
    }

    void OnStatChanged(string changed, int _)
    {
        switch (statKind)
        {
            case StatKind.HP:
                if (changed == "hp" || changed == "maxHP" || changed == "maxHP_eff") Refresh();
                break;
            case StatKind.MP:
                if (changed == "mp" || changed == "maxMP" || changed == "maxMP_eff") Refresh();
                break;
            case StatKind.XP:
                if (changed == "xp" || changed == "xpToNext" || changed == "level") Refresh();
                break;
        }
    }

    void Refresh()
    {
        if (!player) return;

        switch (statKind)
        {
            case StatKind.HP:
                UpdateOrb(player.HP, player.EffMaxHP);
                break;
            case StatKind.MP:
                UpdateOrb(player.MP, player.EffMaxMP);
                break;
            case StatKind.XP:
                UpdateXP();
                break;
        }
    }

    void UpdateOrb(int cur, int max)
    {
        if (!orbImage || orbFrames == null || orbFrames.Length == 0) return;
        float pct = max > 0 ? (float)cur / max : 0f;
        int idx = Mathf.Clamp(Mathf.FloorToInt(pct * (orbFrames.Length - 1)), 0, orbFrames.Length - 1);
        orbImage.sprite = orbFrames[idx];
    }

    void UpdateXP()
    {
        if (!xpSlider) return;

        int max = Mathf.Max(1, player.xpToNext);
        int cur = Mathf.Clamp(player.xp, 0, max);

        xpSlider.maxValue = max;
        xpSlider.value = cur;

        if (xpLabel) xpLabel.text = $"{cur} / {max}";
    }
}
