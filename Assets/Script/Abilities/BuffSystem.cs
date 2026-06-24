using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : MonoBehaviour
{
    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private CharacterStats stats;
    public System.Action<ActiveBuff, BuffSystem> OnBuffAdded;
    public System.Action<ActiveBuff, BuffSystem> OnBuffRemoved;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    // Expose active buffs snapshot for UI to query (safe copy)
    public System.Collections.Generic.List<ActiveBuff> GetActiveBuffs()
    {
        return new System.Collections.Generic.List<ActiveBuff>(activeBuffs);
    }

    void Update()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {

            if (i >= activeBuffs.Count)
            {
                Debug.LogError("Buff list changed during update!");
                continue;
            }


            activeBuffs[i].Update(Time.deltaTime, stats);

            if (activeBuffs[i].IsFinished)
            {
                var removed = activeBuffs[i];
                activeBuffs.RemoveAt(i);
                OnBuffRemoved?.Invoke(removed, this);
            }
        }
    }

    public bool HasBuff(ActiveBuff buff)
    {
        return activeBuffs.Contains(buff);
    }

    // 🔥 NY GENERISK ENTRY POINT
    public void ApplyEffect(
    AbilityEffect effect,
    CharacterStats source = null,
    float? overrideDuration = null)
    {
        ActiveBuff existing = activeBuffs.Find(b => b.SourceEffect == effect);

        if (existing != null && effect.stackable)
        {
            if (existing.stacks < effect.maxStacks)
            {
                existing.stacks++;
                existing.OnStackChanged(stats);
            }

            if (effect.refreshDurationOnStack)
                existing.SetDuration(overrideDuration ?? existing.duration);
            existing.ResetDuration();

            return;
        }

        ActiveBuff buff = CreateBuff(effect, source);

        if (buff == null)
            return;

        // 🔥 NYTT
        if (overrideDuration.HasValue)
        {
            buff.SetDuration(overrideDuration.Value);
        }

        activeBuffs.Add(buff);

        // Fire event for any UI listeners (include owner)
        OnBuffAdded?.Invoke(buff, this);

        //Debug.Log($"BuffSystem: Added buff '{buff.Name}' on '{gameObject.name}' (owner={this.GetType().Name})");

        // UI: listeners should subscribe to OnBuffAdded to receive updates (we now use the event instead of direct calls)

        NameplateUI nameplate = GetComponentInChildren<NameplateUI>();
        if (nameplate != null)
        {
            nameplate.AddBuff(buff);
        }
    }

    // 🔥 FACTORY
    private ActiveBuff CreateBuff(
    AbilityEffect effect,
    CharacterStats source)
    {
        if (effect is BleedEffect bleed)
            return new ActiveBleed(bleed, source);

        if (effect is StrengthBuffEffect strength)
            return new ActiveStrengthBuff(strength);

        if (effect is InjuryEffect injury)
            return new ActiveInjury(injury);

        if (effect is StunEffect stun)
            return new ActiveStun(stun, stats);

        return null;
    }

    public void ClearAll()
    {
        // 🔥 ta bort stat modifiers också
        foreach (var buff in activeBuffs)
        {
            stats.RemoveModifiersFromSource(buff.SourceEffect);
            OnBuffRemoved?.Invoke(buff, this);
        }

        activeBuffs.Clear();
    }

    public void RemoveDeathRemovableBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];

            if (!buff.RemoveOnDeath)
                continue;

            stats.RemoveModifiersFromSource(buff.SourceEffect);

            activeBuffs.RemoveAt(i);
            OnBuffRemoved?.Invoke(buff, this);
        }
    }
}