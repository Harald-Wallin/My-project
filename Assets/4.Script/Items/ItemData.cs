using System;
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
    Shield,
    FavourItem
}

[CreateAssetMenu(
    menuName = "Items/Item"
)]
public class ItemData :
    ScriptableObject,
    ITooltipProvider
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Permanent ID för favours och save/load. " +
        "Ändra inte efter release."
    )]
    private string id;

    [Header("Basic Info")]

    public string itemName;

    public Sprite worldSprite;

    [Header("Rarity")]

    public ItemRarity rarity =
        ItemRarity.Common;

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

    [Header("Favour Item")]

    [SerializeField]
    [Tooltip(
    "Den favour som måste ha accepterats innan detta item " +
    "får förekomma som loot. Används endast för FavourItem.")]
    private FavourData requiredFavour;

    [Header("Flags")]

    public bool equippable;
    public bool stackable;

    [Min(1)]
    public int maxStack = 1;

    [Header("Stat Modifiers")]

    public ItemStatModifier[] statModifiers;

    [Header("Requirements")]

    public bool useLevelRequirement;
    public int requiredLevel;
    public Faction reputationFaction;

    public bool useReputationRequirement;
    public ReputationState requiredReputation;

    [Header("Economy")]

    [SerializeField]
    private int buyPrice = 10;

    [Range(0f, 1f)]
    [SerializeField]
    private float sellMultiplier = 0.6f;

    public string Id =>
        id;

    public FavourData RequiredFavour =>
    requiredFavour;

    public string ItemTypeDisplayName =>
        GetItemTypeDisplayName(
            itemType);

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            itemName
        )
            ? name
            : itemName;

    public int BuyPrice =>
        buyPrice;

    public int SellPrice =>
        Mathf.RoundToInt(
            buyPrice *
            sellMultiplier
        );

    [Serializable]
    public class VendorItem
    {
        public ItemData item;
        public int price;
        public bool unique;
        public float cooldownSeconds;

        [HideInInspector]
        public float lastSoldTime;

        public string warningDialog;
    }

    public TooltipData GetTooltipData(
        CharacterStats caster)
    {
        TooltipData data =
            new TooltipData();

        data.title =
            DisplayName;

        data.titleColor =
            ItemRarityColors.GetColor(
                rarity
            );

        data.subtitle =
            ItemTypeDisplayName;

        data.description =
            description;

        if (statModifiers != null)
        {
            foreach (ItemStatModifier modifier
                     in statModifiers)
            {
                if (modifier == null)
                    continue;

                if (Mathf.Approximately(
                        modifier.value,
                        0f))
                {
                    continue;
                }

                data.stats.Add(
                    StatFormatting.FormatModifier(
                        modifier.stat,
                        modifier.modifierType,
                        modifier.value
                    )
                );
            }
        }

        data.footer =
            $"Sellprice: {SellPrice}";

        data.showFooter =
            true;

        PlayerStats player =
            PlayerReference.Player;

        AddRequirementLines(
            data,
            player
        );

        return data;
    }

    public bool MeetsRequirements(
        PlayerStats player)
    {
        if (player == null)
            return false;

        if (useLevelRequirement &&
            player.level < requiredLevel)
        {
            return false;
        }

        if (useReputationRequirement)
        {
            if (reputationFaction == null)
                return false;

            PlayerReputationManager rep =
                player.GetComponent<
                    PlayerReputationManager
                >();

            if (rep == null)
                return false;

            ReputationState state =
                rep.GetReputationState(
                    reputationFaction
                );

            if (state <
                requiredReputation)
            {
                return false;
            }
        }

        return true;
    }

    public void AddRequirementLines(
        TooltipData data,
        PlayerStats player)
    {
        if (data == null ||
            player == null)
        {
            return;
        }

        if (useLevelRequirement)
        {
            bool meets =
                player.level >=
                requiredLevel;

            string color =
                meets
                    ? "white"
                    : "#FF5555";

            data.requirements.Add(
                $"<color={color}>Requires Level {requiredLevel}</color>"
            );
        }

        if (useReputationRequirement &&
            reputationFaction != null)
        {
            PlayerReputationManager rep =
                player.GetComponent<
                    PlayerReputationManager
                >();

            if (rep != null)
            {
                ReputationState state =
                    rep.GetReputationState(
                        reputationFaction
                    );

                bool meets =
                    state >=
                    requiredReputation;

                string color =
                    meets
                        ? "white"
                        : "#FF5555";

                data.requirements.Add(
                    $"<color={color}>Requires {requiredReputation} " +
                    $"with {reputationFaction.factionName}</color>"
                );
            }
        }
    }

    private static string GetItemTypeDisplayName(
    ItemType type)
    {
        return type switch
        {
            ItemType.FavourItem =>
                "Favour item",

            _ =>
                type.ToString()
        };
    }

    public bool CanDropForPlayer(
    PlayerFavourManager favourManager)
    {
        /*
         * Vanliga items påverkas aldrig av favour-systemet.
         * De kan fortsätta droppa precis som tidigare.
         */
        if (itemType != ItemType.FavourItem)
            return true;

        /*
         * Ett FavourItem utan konfigurerad favour betraktas som
         * felkonfigurerat och får inte droppa.
         */
        if (requiredFavour == null)
            return false;

        if (favourManager == null)
            return false;

        return favourManager.HasAccepted(
            requiredFavour);
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(
        typeof(ItemData)
    )]
    public class ItemDataEditor :
        UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ItemData item =
                (ItemData)target;

            UnityEditor.EditorGUILayout
                .Space();

            UnityEditor.EditorGUILayout
                .LabelField(
                    "Calculated Sell Price",
                    item.SellPrice.ToString()
                );
        }
    }

    protected virtual void OnValidate()
    {
        id = id?.Trim();

        maxStack = stackable
                ? Mathf.Max(
                    1,
                    maxStack
                )
                : 1;
        if (itemType != ItemType.FavourItem)
        {
            requiredFavour = null;
        }
        else if (requiredFavour == null)
        {
            Debug.LogWarning(
                $"FavourItem '{name}' saknar Required Favour.",
                this);
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning(
                $"ItemData '{name}' saknar permanent ID.",
                this
            );
        }
    }
#endif
}