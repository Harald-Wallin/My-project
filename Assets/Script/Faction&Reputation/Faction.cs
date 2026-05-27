using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RPG/Faction")]
public class Faction : ScriptableObject
{
    public string factionName;
    [TextArea(3, 6)]
    public string loreDescription;

    public string factionCategory;

    [System.Serializable]

    public class FactionRelation
    {
        public Faction otherFaction;
        [Range(-100, 100)] public int relation;
        // -100 = Hat
        // 0 = Neutral
        // 100 = Allierad
    }

    public List<FactionRelation> relations = new List<FactionRelation>();

    public int GetRelation(Faction other)
    {
        if (other == null)
            return 0;

        foreach (var rel in relations)
        {
            if (rel.otherFaction == other)
                return rel.relation;
        }

        return 0; // Default neutral
    }
}


