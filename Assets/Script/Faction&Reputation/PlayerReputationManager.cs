using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReputationManager : MonoBehaviour
{
    public ReputationLevelDefinition levelDefinition;
    //public event Action<FactionReputationData> OnReputationChanged;
    public event System.Action<FactionReputationData> OnReputationChanged;

    public List<FactionReputationData> reputations = new List<FactionReputationData>();

    //DEBUG FACTION
    public Faction debugFaction;

    //Temporär hostility
    private Dictionary<Faction, float> temporaryHostilityTimers = new Dictionary<Faction, float>();

    [SerializeField]
    private float defaultCrimeHostilityDuration = 300f;

    public FactionReputationData GetReputation(Faction faction)
    {
        foreach (var rep in reputations)
        {
            if (rep.faction == faction)
                return rep;
        }

        return null;
    }


    public void DiscoverFaction(Faction faction)
    {
        if (faction == null)
            return;

        var rep = GetReputation(faction);
        //int oldLevel = rep != null ? rep.level : 1;

        if (rep == null)
        {
            rep = new FactionReputationData
            {
                faction = faction,
                discovered = true,
                level = 1,
                currentXP = 0
            };

            reputations.Add(rep);
        }
        else
        {
            if (rep.discovered)
                return;

            rep.discovered = true;
        }

        if (faction.showInReputationWindow)
        {
            AnnouncementSpawner.Instance?.QueueAnnouncement(
                AnnouncementSpawner.Instance.Database.factionDiscovered,
                $"Faction discovered:\n{faction.factionName}"
            );
        }

        OnReputationChanged?.Invoke(rep);
    }



    public void AddReputation(Faction faction, int amount)
    {
        if (faction == null)
            return;

        var rep = GetReputation(faction);

        int oldLevel = rep != null ? rep.level : 1;

        if (rep == null)
        {
            rep = new FactionReputationData
            {
                faction = faction,
                discovered = false,
                level = 1,
                currentXP = 0
            };

            reputations.Add(rep);
        }

        rep.currentXP += amount;

        while (rep.currentXP >=
               levelDefinition.GetXPRequired(rep.level)
               && rep.level < levelDefinition.maxLevel)
        {
            rep.currentXP -= levelDefinition.GetXPRequired(rep.level);


            rep.level++;
        }

        while (rep.currentXP < 0 && rep.level > 1)
        {

            rep.level--;
            rep.currentXP += levelDefinition.GetXPRequired(rep.level);
        }

        if (rep.level != oldLevel)
        {
            string tierName =
                levelDefinition.GetTierName(
                    rep.level
                );

            Color tierColor =
                ReputationColorUtility.GetColor(
                    rep.level
                );

            AudioClip rankSound =
                levelDefinition.GetTierSound(
                    rep.level
                );

            string message =
                AnnouncementFormatter
                .BuildReputationAnnouncement(
                    tierName,
                    tierColor,
                    faction.factionName
                );

            AnnouncementSpawner.Instance
                ?.QueueAnnouncement(
                    AnnouncementSpawner.Instance
                        .Database
                        .reputationRankChanged,
                    message,
                    rankSound
                );
        }

        OnReputationChanged?.Invoke(rep);

    }

    public FactionReputationData GetTrackedFaction()
    {
        foreach (var rep in reputations)
        {
            if (rep.tracked)
                return rep;
        }

        return null;
    }


    public ReputationState GetReputationState(Faction faction)
    {
        var rep = GetReputation(faction);

        if (rep == null)
            return ReputationState.Indifferent;

        int level = rep.level;

        if (level <= 1)
            return ReputationState.Hated;
        else if (level == 2)
            return ReputationState.Untrusted;
        else if (level == 3)
            return ReputationState.Indifferent;
        else if (level == 4)
            return ReputationState.Favoured;
        else if (level == 5)
            return ReputationState.Renowned;
        else if (level == 6)    
            return ReputationState.Praised;
        else 
            return ReputationState.Revered;
    }

    public bool IsTracked(Faction faction)
    {
        var data = GetReputation(faction);
        return data != null && data.tracked;
    }

    public void SetTracked(Faction faction, bool value)
    {
        foreach (var rep in reputations)
            rep.tracked = false;

        if (!value)
        {
            OnReputationChanged?.Invoke(GetTrackedFaction());
            return;
        }

        var data = GetReputation(faction);
        if (data == null) return;

        data.tracked = true;

        OnReputationChanged?.Invoke(data);
    }

    public bool IsMurderEnabled(Faction faction)
    {

        if (faction==null) return false;

        var data = GetReputation(faction);

        if(data == null)
        {
            DiscoverFaction(faction);
            data =
                GetReputation(faction);
        }
        return data != null && data.murderEnabled;
    }

    public void SetMurderEnabled(Faction faction, bool value)
    {
        var data = GetReputation(faction);
        if (data == null) return;

        data.murderEnabled = value;
        OnReputationChanged?.Invoke(data);
    }

}

