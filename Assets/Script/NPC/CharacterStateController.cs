using System;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class CharacterStateController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float combatDuration = 5f;

    private CharacterStats stats;

    private float combatTimer;
    private bool isPlayer;

    public bool InCombat { get; private set; }

    public event Action OnEnteredCombat;
    public event Action OnLeftCombat;

    public event Action<bool> OnCombatStateChanged;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        isPlayer = GetComponent<PlayerStats>() != null;
    }

    void Update()
    {
        UpdateCombatTimer();
    }

    void UpdateCombatTimer()
    {
        if (!InCombat)
            return;

        combatTimer -= Time.deltaTime;

        if (combatTimer <= 0f)
        {
            LeaveCombat();
        }
    }

    public void EnterCombat()
    {

        if (InCombat)
        {
            combatTimer = combatDuration;
            return;
        }

        InCombat = true;
        combatTimer = combatDuration;

        OnEnteredCombat?.Invoke();
        OnCombatStateChanged?.Invoke(true);

        if (isPlayer)
        {
            NotificationSpawner.Instance?.Show(
                NotificationSpawner.Instance.Database.enteringCombat);
        }
    }

    public void RefreshCombat()
    {
        if (!InCombat)
        {
            EnterCombat();
            return;
        }

        combatTimer = combatDuration;
    }

    public void ForceLeaveCombat()
    {
        LeaveCombat();
    }

    private void LeaveCombat()
    {
        if (!InCombat)
            return;

        InCombat = false;

        OnLeftCombat?.Invoke();
        OnCombatStateChanged?.Invoke(false);

        if (isPlayer)
        {
            NotificationSpawner.Instance?.Show(
                NotificationSpawner.Instance.Database.leavingCombat);
        }
    }

    public void NotifyCombatActivity()
    {
        RefreshCombat();
    }

    //---------------------------------------------------
    // STATE WRAPPERS
    //---------------------------------------------------

    public bool IsDead =>
        stats.currentHP <= 0;

    public bool IsAlive =>
        !IsDead;

    public bool IsStunned =>
        stats.IsStunned;

    //---------------------------------------------------
    // PERMISSIONS
    //---------------------------------------------------

    public bool CanMove =>
        IsAlive &&
        !IsStunned;

    public bool CanRotate =>
        IsAlive &&
        !IsStunned;

    public bool CanAttack =>
        IsAlive &&
        !IsStunned;

    public bool CanUseAbilities =>
        IsAlive &&
        !IsStunned;

    public bool CanInteract =>
        IsAlive;

    public bool CanTalk =>
        IsAlive &&
        !InCombat;

    public bool CanEat =>
        IsAlive &&
        !InCombat;

    public bool CanLoot =>
        IsAlive;

    public bool CanOpenVendor =>
        IsAlive &&
        !InCombat;

    public bool CanOpenChest =>
        IsAlive;

    private void ShowEnterCombatNotification()
    {
        if (NotificationSpawner.Instance == null)
            return;

        NotificationSpawner.Instance.Show(
            NotificationSpawner.Instance.Database.enteringCombat
        );
    }

    private void ShowLeaveCombatNotification()
    {
        if (NotificationSpawner.Instance == null)
            return;

        NotificationSpawner.Instance.Show(
            NotificationSpawner.Instance.Database.leavingCombat
        );
    }
}