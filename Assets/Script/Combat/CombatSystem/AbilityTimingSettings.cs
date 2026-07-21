using System;
using UnityEngine;

[Serializable]
public sealed class AbilityTimingSettings
{
    [SerializeField]
    private ActionTimingType timingType =
        ActionTimingType.Instant;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Tiden innan en Cast-action exekveras."
    )]
    private float castDuration;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Total varaktighet för en Channel-action."
    )]
    private float channelDuration;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Intervall mellan channel-ticks. " +
        "Används först när channel-execution implementeras."
    )]
    private float channelTickInterval = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Tiden som krävs för att nå full charge."
    )]
    private float maximumChargeDuration = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Tid efter execution innan karaktären kan börja " +
        "en ny action."
    )]
    private float recoveryDuration;

    public ActionTimingType TimingType =>
        timingType;

    public float CastDuration =>
        Mathf.Max(0f, castDuration);

    public float ChannelDuration =>
        Mathf.Max(0f, channelDuration);

    public float ChannelTickInterval =>
        Mathf.Max(0.01f, channelTickInterval);

    public float MaximumChargeDuration =>
        Mathf.Max(0f, maximumChargeDuration);

    public float RecoveryDuration =>
        Mathf.Max(0f, recoveryDuration);

    public bool HasPreparationPhase =>
        timingType == ActionTimingType.Cast ||
        timingType == ActionTimingType.Charge;

    public bool HasChannelPhase =>
        timingType == ActionTimingType.Channel;
}
