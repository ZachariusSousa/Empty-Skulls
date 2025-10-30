using UnityEngine;

[RequireComponent(typeof(LootBag))]
public class LootBagSkinner : MonoBehaviour
{
    [Header("SpriteRenderer to recolor")]
    public SpriteRenderer targetRenderer;

    [Header("Bag sprites by rarity")]
    public Sprite commonSprite;
    public Sprite uncommonSprite;
    public Sprite rareSprite;
    public Sprite epicSprite;
    public Sprite legendarySprite;

    LootBag _bag;

    void Awake()
    {
        _bag = GetComponent<LootBag>();
        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    void OnEnable()
    {
        if (_bag != null)
            _bag.onChanged += UpdateVisual;
    }

    void OnDisable()
    {
        if (_bag != null)
            _bag.onChanged -= UpdateVisual;
    }

    void Start()
    {
        UpdateVisual(_bag);
    }

    void UpdateVisual(LootBag bag)
    {
        if (!bag || !targetRenderer) return;

        var rarity = bag.GetHighestRarity();
        Sprite spriteToUse = rarity switch
        {
            LootRarity.Uncommon  => uncommonSprite,
            LootRarity.Rare      => rareSprite,
            LootRarity.Epic      => epicSprite,
            LootRarity.Legendary => legendarySprite,
            _                    => commonSprite
        };

        if (spriteToUse)
            targetRenderer.sprite = spriteToUse;
    }
}
