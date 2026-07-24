using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation controller för en karaktärs nameplate.
///
/// NameplateUI äger inte gameplaydata. Den läser CharacterStats,
/// CharacterStateController och BuffSystem och presenterar deras
/// aktuella tillstånd.
/// </summary>
public sealed class NameplateUI :
    MonoBehaviour
{
    private static readonly Dictionary<
        CharacterStats,
        NameplateUI> Registry =
            new();

    [Header("Target")]

    [SerializeField]
    private CharacterStats target;

    [Header("Identity")]

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private TMP_Text roleText;

    [SerializeField]
    private TMP_Text factionText;

    [Header("Health")]

    [SerializeField]
    private GameObject healthBarRoot;

    [SerializeField]
    private Image healthFill;

    [SerializeField]
    private TMP_Text hpText;

    [Header("Buffs")]

    [SerializeField]
    private GameObject buffRowRoot;

    [SerializeField]
    private NameplateBuffDisplay buffDisplay;

    [Header("Future Styling")]

    [SerializeField]
    [Tooltip(
        "Valfri rot för framtida ramar, bossmarkörer och " +
        "andra dekorationer."
    )]
    private GameObject decorationRoot;

    [Header("Refresh")]

    [SerializeField]
    [Min(0.05f)]
    private float passiveRefreshInterval =
        0.25f;

    private PlayerStats player;
    private PlayerReputationManager
        reputationManager;

    private CharacterStateController
        stateController;

    private BuffSystem buffSystem;

    private NameplatePresentationState
        currentState;

    private bool isHovered;
    private bool isCorpse;
    private bool subscribed;

    private float passiveRefreshTimer;

    public CharacterStats Target =>
        target;

    public NameplatePresentationState
        CurrentState =>
            currentState;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        player =
            PlayerReference.Player;

        reputationManager =
            FindFirstObjectByType<
                PlayerReputationManager
            >();

        BindBuffDisplay();
        RefreshAll();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Register();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        Unregister();
    }

    private void Update()
    {
        passiveRefreshTimer -=
            Time.deltaTime;

        if (passiveRefreshTimer > 0f)
            return;

        passiveRefreshTimer =
            passiveRefreshInterval;

        /*
         * Faction reputation och temporary hostility saknar
         * ännu egna events. En billig periodisk refresh fångar
         * därför dessa förändringar utan att uppdatera varje frame.
         */
        RefreshState();
        RefreshHealthColor();
    }

    public static bool TryGet(
        CharacterStats character,
        out NameplateUI nameplate)
    {
        if (character == null)
        {
            nameplate = null;
            return false;
        }

        return Registry.TryGetValue(
            character,
            out nameplate
        );
    }

    public void SetHovered(
        bool hovered)
    {
        if (isHovered == hovered)
            return;

        isHovered =
            hovered;

        RefreshState();
    }

    private bool ShouldShowFactionOnHover()
    {
        if (target == null ||
            target.faction == null)
        {
            return false;
        }

        if (!target.faction.showInReputationWindow)
            return false;

        return !string.IsNullOrWhiteSpace(
            target.FactionName
        );
    }

    public void SetCorpseMode()
    {
        isCorpse = true;

        ApplyState(
            NameplatePresentationState
                .Corpse
        );
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target =
                GetComponentInParent<
                    CharacterStats
                >();
        }

        if (target == null)
            return;

        stateController =
            target.GetComponent<
                CharacterStateController
            >();

        buffSystem =
            target.GetComponent<
                BuffSystem
            >();
    }

    private void BindBuffDisplay()
    {
        if (buffDisplay != null)
        {
            buffDisplay.Bind(
                buffSystem
            );
        }
    }

    private void Register()
    {
        if (target == null)
            return;

        Registry[target] =
            this;
    }

    private void Unregister()
    {
        if (target == null)
            return;

        if (Registry.TryGetValue(
                target,
                out NameplateUI registered) &&
            registered == this)
        {
            Registry.Remove(
                target
            );
        }
    }

    private void Subscribe()
    {
        if (subscribed ||
            target == null)
        {
            return;
        }

        target.OnHealthChanged +=
            HandleHealthChanged;

        target.OnStatsChanged +=
            HandleStatsChanged;

        target.OnDied +=
            HandleDied;

        if (stateController != null)
        {
            stateController
                .OnCombatStateChanged +=
                HandleCombatStateChanged;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed ||
            target == null)
        {
            return;
        }

        target.OnHealthChanged -=
            HandleHealthChanged;

        target.OnStatsChanged -=
            HandleStatsChanged;

        target.OnDied -=
            HandleDied;

        if (stateController != null)
        {
            stateController
                .OnCombatStateChanged -=
                HandleCombatStateChanged;
        }

        subscribed = false;
    }

    private void HandleHealthChanged()
    {
        RefreshHealth();
        RefreshState();
    }

    private void HandleStatsChanged()
    {
        RefreshIdentity();
        RefreshHealth();
    }

    private void HandleDied(
        CharacterStats deadCharacter)
    {
        SetCorpseMode();
    }

    private void HandleCombatStateChanged(
        bool inCombat)
    {
        RefreshState();
    }

    private void RefreshAll()
    {
        if (target == null)
            return;

        RefreshIdentity();
        RefreshHealth();
        RefreshState();
    }

    private void RefreshIdentity()
    {
        if (target == null)
            return;

        if (nameText != null)
        {
            nameText.text =
                target.DisplayName;
        }

        if (levelText != null)
        {
            levelText.text =
                target.level.ToString();

            if (player != null)
            {
                int levelDifference =
                    target.level -
                    player.level;

                levelText.color =
                    GetDifficultyColor(
                        levelDifference
                    );
            }
        }

        if (roleText != null)
        {
            roleText.text =
                target.RoleName;
        }

        if (factionText != null)
        {
            factionText.text =
                target.FactionName;
        }
    }

    private void RefreshHealth()
    {
        if (target == null)
            return;

        int maximumHealth =
            Mathf.Max(
                1,
                target.GetMaxHP()
            );

        float normalizedHealth =
            Mathf.Clamp01(
                target.currentHP /
                (float)maximumHealth
            );

        if (healthFill != null)
        {
            healthFill.rectTransform
                .localScale =
                new Vector3(
                    normalizedHealth,
                    1f,
                    1f
                );
        }

        if (hpText != null &&
            !isCorpse)
        {
            hpText.text =
                $"{target.currentHP} / " +
                $"{maximumHealth}";
        }

        RefreshHealthColor();
    }

    private void RefreshHealthColor()
    {
        if (target == null ||
            healthFill == null)
        {
            return;
        }

        Color fillColor =
            Color.white;

        NPCReactionController reaction =
            target.GetComponent<
                NPCReactionController
            >();

        bool hostile =
            player != null &&
            target.IsHostileToPlayer(
                player
            );

        bool temporarilyHostile =
            reaction != null &&
            reaction
                .IsTemporarilyHostile;

        if (hostile ||
            temporarilyHostile)
        {
            fillColor =
                ReputationColorUtility
                    .GetColor(1);
        }
        else if (
            reputationManager != null &&
            target.faction != null)
        {
            var reputation =
                reputationManager
                    .GetReputation(
                        target.faction
                    );

            int reputationLevel =
                reputation != null
                    ? reputation.level
                    : 3;

            fillColor =
                ReputationColorUtility
                    .GetColor(
                        reputationLevel
                    );
        }

        healthFill.color =
            fillColor;
    }

    private void RefreshState()
    {
        NameplatePresentationState
            resolvedState =
                ResolveState();

        ApplyState(
            resolvedState
        );
    }

    private NameplatePresentationState
        ResolveState()
    {
        if (isCorpse ||
            target == null ||
            !target.IsAlive)
        {
            return
                NameplatePresentationState
                    .Corpse;
        }

        if (stateController != null &&
            stateController.InCombat)
        {
            return
                NameplatePresentationState
                    .Combat;
        }

        if (isHovered)
        {
            return
                NameplatePresentationState
                    .Hover;
        }

        return
            NameplatePresentationState
                .Default;
    }

    private void ApplyState(
    NameplatePresentationState state)
    {
        currentState =
            state;

        switch (state)
        {
            case NameplatePresentationState.Default:

                bool hasRole =
                    target != null &&
                    target.HasRole;

                /*
                 * NPC:er med role visar sin titel i idle.
                 * NPC:er utan role använder samma utrymme till healthbaren.
                 */
                SetActive(
                    roleText,
                    hasRole
                );

                SetActive(
                    healthBarRoot,
                    !hasRole
                );

                SetActive(
                    factionText,
                    false
                );

                SetActive(
                    buffRowRoot,
                    false
                );
                break;

            case NameplatePresentationState.Hover:

                SetActive(
                    roleText,
                    false
                );

                SetActive(
                    healthBarRoot,
                    true
                );

                SetActive(
                    factionText,
                    ShouldShowFactionOnHover()
                );

                SetActive(
                    buffRowRoot,
                    false
                );
                break;

            case NameplatePresentationState.Combat:

                SetActive(
                    roleText,
                    false
                );

                SetActive(
                    healthBarRoot,
                    true
                );

                SetActive(
                    factionText,
                    false
                );

                SetActive(
                    buffRowRoot,
                    true
                );
                break;

            case NameplatePresentationState.Corpse:

                SetActive(
                    roleText,
                    false
                );

                SetActive(
                    healthBarRoot,
                    true
                );

                SetActive(
                    factionText,
                    false
                );

                SetActive(
                    buffRowRoot,
                    false
                );

                if (hpText != null)
                {
                    hpText.text =
                        "Corpse";
                }

                if (healthFill != null)
                {
                    healthFill
                        .rectTransform
                        .localScale =
                        new Vector3(
                            0f,
                            1f,
                            1f
                        );
                }
                break;
        }
    }

    private static void SetActive(
        Component component,
        bool active)
    {
        if (component != null)
        {
            component.gameObject
                .SetActive(active);
        }
    }

    private static void SetActive(
        GameObject targetObject,
        bool active)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(
                active
            );
        }
    }

    private static Color
        GetDifficultyColor(
            int difference)
    {
        if (difference >= 5)
            return Hex("#D51512");

        if (difference >= 3)
            return Hex("#D55912");

        if (difference >= -2)
            return Hex("#F5F207");

        if (difference >= -5)
            return Hex("#4D9927");

        return Hex("#9E9E9E");
    }

    private static Color Hex(
        string hex)
    {
        return ColorUtility
            .TryParseHtmlString(
                hex,
                out Color color)
                ? color
                : Color.white;
    }
}