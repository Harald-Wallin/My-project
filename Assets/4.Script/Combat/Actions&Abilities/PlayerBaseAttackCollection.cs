using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class PlayerBaseAttackCollection :
    MonoBehaviour
{
    public const int SlotCount = 2;

    public const int PrimarySlotIndex = 0;
    public const int SecondarySlotIndex = 1;

    [Header("Learned Base Attacks")]

    [SerializeField]
    private List<AbilityData> learnedAttacks =
        new();

    [Header("Equipped Base Attacks")]

    [FormerlySerializedAs("equippedAttack")]
    [SerializeField]
    private AbilityData primaryAttack;

    [SerializeField]
    private AbilityData secondaryAttack;

    /// <summary>
    /// Anropas när innehållet i en slot ändras.
    ///
    /// Parametrar:
    /// - slot-index
    /// - attacken som nu ligger i slotten
    /// </summary>
    public event Action<int, AbilityData>
        OnAttackSlotChanged;

    /// <summary>
    /// Anropas när primary-attacken ändras.
    ///
    /// Primary är alltid den aktiva attacken.
    ///
    /// Parametrar:
    /// - alltid PrimarySlotIndex
    /// - den nya aktiva attacken
    /// </summary>
    public event Action<int, AbilityData>
        OnActiveAttackChanged;

    /// <summary>
    /// Kompatibilitetsevent för äldre kod.
    /// Skickar alltid den aktuella primary-attacken.
    /// </summary>
    public event Action<AbilityData>
        OnEquippedAttackChanged;

    public int ActiveSlotIndex =>
        PrimarySlotIndex;

    public AbilityData ActiveAttack =>
        primaryAttack;

    public AbilityData PrimaryAttack =>
        primaryAttack;

    public AbilityData SecondaryAttack =>
        secondaryAttack;

    public bool CanSwap =>
        secondaryAttack != null;

    public IReadOnlyList<AbilityData>
        GetLearnedAttacks()
    {
        return learnedAttacks;
    }

    /// <summary>
    /// Det utrustade vapnet är alltid det som ligger
    /// i primary-slotten.
    /// </summary>
    public AbilityData GetEquippedAttack()
    {
        return primaryAttack;
    }

    public AbilityData GetActiveAttack()
    {
        return primaryAttack;
    }

    public AbilityData GetAttack(
        int slotIndex)
    {
        return slotIndex switch
        {
            PrimarySlotIndex =>
                primaryAttack,

            SecondarySlotIndex =>
                secondaryAttack,

            _ =>
                null
        };
    }

    /// <summary>
    /// Äldre API. Utrustar alltid attacken i primary-slotten.
    /// </summary>
    public bool EquipAttack(
        AbilityData attack)
    {
        return EquipAttack(
            PrimarySlotIndex,
            attack
        );
    }

    /// <summary>
    /// Utrustar en base attack i angiven slot.
    ///
    /// Om attacken redan ligger i den andra slotten byter
    /// de två attackerna plats. Samma attack kan därför
    /// aldrig ligga i båda slotarna samtidigt.
    /// </summary>
    public bool EquipAttack(
        int slotIndex,
        AbilityData attack)
    {
        if (!IsValidSlotIndex(
                slotIndex))
        {
            Debug.LogWarning(
                $"Ogiltigt base attack-slotindex: " +
                $"{slotIndex}.",
                this
            );

            return false;
        }

        if (!ValidateAttack(
                attack))
        {
            return false;
        }

        AbilityData currentAttack =
            GetAttack(
                slotIndex
            );

        if (currentAttack == attack)
            return true;

        int otherSlotIndex =
            GetOtherSlotIndex(
                slotIndex
            );

        AbilityData otherAttack =
            GetAttack(
                otherSlotIndex
            );

        /*
         * Attacken finns redan i den andra slotten.
         * Byt därför plats på de båda slotarnas innehåll.
         */
        if (attack != null &&
            otherAttack == attack)
        {
            SetAttackInternal(
                slotIndex,
                attack
            );

            SetAttackInternal(
                otherSlotIndex,
                currentAttack
            );

            RaiseBothSlotsChanged();
            RaiseActiveAttackChanged();

            return true;
        }

        SetAttackInternal(
            slotIndex,
            attack
        );

        RaiseSlotChanged(
            slotIndex
        );

        RaiseActiveAttackChanged();

        return true;
    }

    public bool ClearAttack(
        int slotIndex)
    {
        if (!IsValidSlotIndex(
                slotIndex))
        {
            return false;
        }

        if (GetAttack(
                slotIndex) == null)
        {
            return true;
        }

        SetAttackInternal(
            slotIndex,
            null
        );

        RaiseSlotChanged(
            slotIndex
        );

        RaiseActiveAttackChanged();

        return true;
    }

    /// <summary>
    /// Byter attackreferenserna mellan primary och secondary.
    ///
    /// UI-objekten flyttas inte. Deras innehåll uppdateras,
    /// vilket gör att secondary-attackens ikon kommer fram
    /// i den stora primary-slotten.
    /// </summary>
    public bool SwapAttacks()
    {
        /*
         * Det finns ingenting användbart att växla till om
         * secondary-slotten är tom.
         */
        if (secondaryAttack == null)
            return false;

        AbilityData previousPrimary =
            primaryAttack;

        primaryAttack =
            secondaryAttack;

        secondaryAttack =
            previousPrimary;

        RaiseBothSlotsChanged();
        RaiseActiveAttackChanged();

        return true;
    }

    /// <summary>
    /// Kompatibilitetsalias för den tidigare metoden.
    /// Den byter nu faktiskt plats på attackerna.
    /// </summary>
    public bool CycleActiveAttack()
    {
        return SwapAttacks();
    }

    /// <summary>
    /// Kan kopplas direkt till en Unity UI Button.
    /// </summary>
    public void SwapActiveAttack()
    {
        SwapAttacks();
    }

    /// <summary>
    /// Behålls för kompatibilitet.
    ///
    /// Primary är alltid aktiv. Ett försök att aktivera
    /// secondary innebär därför att attackerna byter plats.
    /// </summary>
    public bool SetActiveSlot(
        int slotIndex)
    {
        if (!IsValidSlotIndex(
                slotIndex))
        {
            return false;
        }

        if (slotIndex ==
            PrimarySlotIndex)
        {
            return true;
        }

        return SwapAttacks();
    }

    public bool LearnAttack(
        AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Ability '{attack.abilityName}' kan inte " +
                $"läras som base attack eftersom dess " +
                $"Usage Type inte är BaseAttack.",
                this
            );

            return false;
        }

        if (learnedAttacks.Contains(
                attack))
        {
            return false;
        }

        learnedAttacks.Add(
            attack
        );

        AnnouncementSpawner.Instance
            ?.QueueAnnouncement(
                AnnouncementSpawner
                    .Instance
                    .Database
                    .abilityLearned,
                AnnouncementFormatter
                    .BuildAbilityLearnedAnnouncement(
                        attack.abilityName
                    )
            );

        SpellbookNotificationManager.Instance
            ?.NotifyNewEntry();

        return true;
    }

    private bool ValidateAttack(
        AbilityData attack)
    {
        /*
         * Null är tillåtet eftersom samma API även används
         * för att tömma en slot.
         */
        if (attack == null)
            return true;

        if (attack.IsBaseAttack)
            return true;

        Debug.LogWarning(
            $"Ability '{attack.abilityName}' kan inte " +
            $"utrustas som base attack eftersom dess " +
            $"Usage Type inte är BaseAttack.",
            this
        );

        return false;
    }

    private void SetAttackInternal(
        int slotIndex,
        AbilityData attack)
    {
        switch (slotIndex)
        {
            case PrimarySlotIndex:
                primaryAttack =
                    attack;

                break;

            case SecondarySlotIndex:
                secondaryAttack =
                    attack;

                break;
        }
    }

    private void RaiseBothSlotsChanged()
    {
        RaiseSlotChanged(
            PrimarySlotIndex
        );

        RaiseSlotChanged(
            SecondarySlotIndex
        );
    }

    private void RaiseSlotChanged(
        int slotIndex)
    {
        OnAttackSlotChanged?.Invoke(
            slotIndex,
            GetAttack(
                slotIndex
            )
        );
    }

    private void RaiseActiveAttackChanged()
    {
        OnActiveAttackChanged?.Invoke(
            PrimarySlotIndex,
            primaryAttack
        );

        OnEquippedAttackChanged?.Invoke(
            primaryAttack
        );
    }

    private static bool IsValidSlotIndex(
        int slotIndex)
    {
        return
            slotIndex >= 0 &&
            slotIndex < SlotCount;
    }

    private static int GetOtherSlotIndex(
        int slotIndex)
    {
        return slotIndex ==
               PrimarySlotIndex
            ? SecondarySlotIndex
            : PrimarySlotIndex;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        learnedAttacks ??=
            new List<AbilityData>();

        for (int i =
                 learnedAttacks.Count - 1;
             i >= 0;
             i--)
        {
            AbilityData attack =
                learnedAttacks[i];

            if (attack == null)
                continue;

            if (attack.IsBaseAttack)
                continue;

            Debug.LogWarning(
                $"'{attack.abilityName}' har tagits bort från " +
                $"{nameof(PlayerBaseAttackCollection)} eftersom " +
                $"dess Usage Type inte är BaseAttack.",
                this
            );

            learnedAttacks.RemoveAt(
                i
            );
        }

        if (primaryAttack != null &&
            !primaryAttack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Primary attack " +
                $"'{primaryAttack.abilityName}' är inte " +
                $"markerad som BaseAttack och har därför " +
                $"tagits bort.",
                this
            );

            primaryAttack =
                null;
        }

        if (secondaryAttack != null &&
            !secondaryAttack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Secondary attack " +
                $"'{secondaryAttack.abilityName}' är inte " +
                $"markerad som BaseAttack och har därför " +
                $"tagits bort.",
                this
            );

            secondaryAttack =
                null;
        }

        if (primaryAttack != null &&
            primaryAttack ==
            secondaryAttack)
        {
            Debug.LogWarning(
                $"Samma base attack låg i båda slotarna. " +
                $"Secondary-slotten har tömts.",
                this
            );

            secondaryAttack =
                null;
        }
    }
#endif
}