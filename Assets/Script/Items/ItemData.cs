using UnityEngine;

public enum ItemRarity
{
    Trash,
    Common,
    Uncommon,
    Strange,
    Extraordinary,
    FromTheSagas
}
public enum ItemType
{
    None,
    Weapon,
    Helmet,
    Chest,
    Back,
    Shoulders,
    Legs,
    Feet,
    Neck,
    Ring,
    Reagent,
    Reputation,
    Trash,
    Offhand,
    Shield

}

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemData : ScriptableObject, ITooltipProvider
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite worldSprite;

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Directional Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Appearance")]
    public bool hideHair;
    public bool hideBeard;
    public bool hideHead;
    public bool hideArms;
    public bool hideTorso;
    public bool hideLegs;
    public bool hideFeet;

    [TextArea(2, 3)]
    public string description;
    public Sprite icon;
    public ItemType itemType;

    [Header("Flags")]
    public bool equippable;
    public bool stackable;
    public int maxStack = 1;

    [Header("Stat Bonuses")]
    public int damageBonus;
    public int armorBonus;
    public float blockChanceBonus;
    public int blockValueBonus;
    public int strengthBonus;
    public int healthBonus;

    [Header("Requirements")]
    public bool useLevelRequirement;
    public int requiredLevel;
    public Faction reputationFaction;

    public bool useReputationRequirement;
    public ReputationState requiredReputation;

    [Header("Economy")]
    [SerializeField] private int buyPrice = 10;
    [Range(0f, 1f)]
    [SerializeField] private float sellMultiplier = 0.6f;
    public int BuyPrice => buyPrice;
    public int SellPrice => Mathf.RoundToInt(buyPrice * sellMultiplier);

    [System.Serializable]
    public class VendorItem
    {
        public ItemData item;
        public int price;
        public bool unique;            // Max 1 i inventory/equip/bank
        public float cooldownSeconds;  // 0 = oändligt
        [HideInInspector] public float lastSoldTime;
        public string warningDialog;   // Optional confirmation
    }

    public TooltipData GetTooltipData(CharacterStats caster)
    {
        TooltipData data = new TooltipData();

        data.title = itemName;
        data.titleColor = ItemRarityColors.GetColor(rarity);
        data.subtitle = itemType.ToString();
        data.description = description;

        if (damageBonus > 0)
            data.stats.Add($"Damage: {damageBonus}");

        if (armorBonus > 0)
            data.stats.Add($"Armor: {armorBonus}");

        if (strengthBonus > 0)
            data.stats.Add($"+{strengthBonus} Strength");

        if (healthBonus > 0)
            data.stats.Add($"+{healthBonus} Health");

        if (blockChanceBonus > 0)
            data.stats.Add($"Block Chance: +{blockChanceBonus * 100f:0}%");

        if (blockValueBonus > 0)
            data.stats.Add($"Block Value: +{blockValueBonus}");

        data.footer = $"Sellprice: {SellPrice}";
        data.showFooter = true;

        PlayerStats player = PlayerReference.Player;
        AddRequirementLines(data, player);

        return data;
    }

    public bool MeetsRequirements(PlayerStats player)
    {
        if (player == null)
            return false;

        // LEVEL
        if (useLevelRequirement)
        {
            if (player.level < requiredLevel)
                return false;
        }

        // REPUTATION
        if (useReputationRequirement)
        {
            if (reputationFaction == null)
                return false;

            PlayerReputationManager rep =
                player.GetComponent<PlayerReputationManager>();

            if (rep == null)
                return false;

            ReputationState state =
                rep.GetReputationState(reputationFaction);

            if (state < requiredReputation)
                return false;
        }

        return true;
    }

    public void AddRequirementLines(
    TooltipData data,
    PlayerStats player
)
    {
        if (player == null)
            return;

        // LEVEL
        if (useLevelRequirement)
        {
            bool meets = player.level >= requiredLevel;

            string color = meets ? "white" : "#FF5555";

            data.requirements.Add(
                $"<color={color}>Requires Level {requiredLevel}</color>"
            );
        }

        // REPUTATION
        if (useReputationRequirement && reputationFaction != null)
        {
            PlayerReputationManager rep =
                player.GetComponent<PlayerReputationManager>();

            if (rep != null)
            {
                ReputationState state =
                    rep.GetReputationState(reputationFaction);

                bool meets = state >= requiredReputation;

                string color = meets ? "white" : "#FF5555";

                data.requirements.Add(
                    $"<color={color}>Requires {requiredReputation} with {reputationFaction.factionName}</color>"
                );
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ItemData))]
    public class ItemDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ItemData item = (ItemData)target;

            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField(
                "Calculated Sell Price",
                item.SellPrice.ToString()
            );
        }
    }
#endif
}




