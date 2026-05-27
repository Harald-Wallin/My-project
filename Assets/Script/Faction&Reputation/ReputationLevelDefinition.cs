using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RPG/Reputation Levels")]
public class ReputationLevelDefinition : ScriptableObject
{
    public int baseXPRequired = 100;
    public float scalingMultiplier = 1.5f;
    public int maxLevel = 9; // Vi kör 7 tiers

    [System.Serializable]
    public class ReputationTier
    {
        public string tierName;
    }

    public List<ReputationTier> tiers = new List<ReputationTier>();

    public int GetXPRequired(int level)
    {
        return Mathf.RoundToInt(
            baseXPRequired * Mathf.Pow(scalingMultiplier, level - 1)
        );
    }

    public string GetTierName(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, tiers.Count - 1);
        return tiers[index].tierName;
    }
}


