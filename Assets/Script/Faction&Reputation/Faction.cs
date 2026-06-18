using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RPG/Faction")]
public class Faction : ScriptableObject
{

    [Header("Can be discovered")]
    public bool showInReputationWindow = true;

    public string factionName;
    [TextArea(3, 6)]
    public string loreDescription;

    public string factionCategory;

    [System.Serializable]
    public class FactionRelation
    {
        public Faction otherFaction;

        public ReputationState standing =
            ReputationState.Indifferent;
    }

    public List<FactionRelation> relations =
        new List<FactionRelation>();

    public ReputationState GetStanding(Faction other)
    {
        if (other == null)
            return ReputationState.Indifferent;

        foreach (var rel in relations)
        {
            if (rel.otherFaction == other)
            {
                return rel.standing;
            }
        }

        return ReputationState.Indifferent;
    }

}


