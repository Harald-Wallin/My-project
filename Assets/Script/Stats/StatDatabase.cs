using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Stat Database")]
public class StatDatabase : ScriptableObject
{
    public List<StatDefinition> stats = new();
}
