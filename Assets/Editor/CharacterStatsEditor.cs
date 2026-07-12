using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterStats))]
public class CharacterStatsEditor : Editor
{
    SerializedProperty statsProperty;

    void OnEnable()
    {
        statsProperty = serializedObject.FindProperty("stats");
        EnsureAllStatsExist();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspectorExceptStats();

        EditorGUILayout.Space(10);

        DrawCategory("Primary Stats",
            StatType.Strength,
            StatType.Swiftness,
            StatType.Armor,
            StatType.Spirit,
            StatType.Intellect);

        DrawCategory("Sub Stats",
            StatType.MaxHP,
            StatType.BaseMeleeDamage,
            StatType.BaseRangedDamage,
            StatType.BaseMagicDamage,
            StatType.WeaponDamage,
            StatType.AttackPower,
            StatType.RangedPower,
            StatType.SpellPower,
            StatType.DamageReduction,
            StatType.AttackSpeed,
            StatType.Haste,
            StatType.MovementSpeed,
            StatType.HitChance,
            StatType.CritChance,
            StatType.CritMultiplier,
            StatType.Evasion,
            StatType.BlockChance,
            StatType.BlockValue);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawDefaultInspectorExceptStats()
    {
        DrawPropertiesExcluding(serializedObject, "stats");
    }

    void DrawCategory(string title, params StatType[] statTypes)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (var stat in statTypes)
        {
            DrawStat(stat);
        }
    }

    void DrawStat(StatType stat)
    {
        SerializedProperty entry = FindEntry(stat);

        if (entry == null)
            return;

        SerializedProperty value =
            entry.FindPropertyRelative("value");

        EditorGUILayout.PropertyField(
            value,
            new GUIContent(stat.ToString())
        );
    }

    SerializedProperty FindEntry(StatType stat)
    {
        for (int i = 0; i < statsProperty.arraySize; i++)
        {
            SerializedProperty entry =
                statsProperty.GetArrayElementAtIndex(i);

            SerializedProperty statProp =
                entry.FindPropertyRelative("stat");

            if ((StatType)statProp.enumValueIndex == stat)
                return entry;
        }

        return null;
    }

    void EnsureAllStatsExist()
    {
        serializedObject.Update();

        HashSet<StatType> existing = new();

        for (int i = 0; i < statsProperty.arraySize; i++)
        {
            var entry = statsProperty.GetArrayElementAtIndex(i);

            existing.Add(
                (StatType)entry
                .FindPropertyRelative("stat")
                .enumValueIndex);
        }

        foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
        {
            if (existing.Contains(stat))
                continue;

            int index = statsProperty.arraySize;

            statsProperty.InsertArrayElementAtIndex(index);

            SerializedProperty newEntry =
                statsProperty.GetArrayElementAtIndex(index);

            newEntry.FindPropertyRelative("stat").enumValueIndex =
                (int)stat;

            newEntry.FindPropertyRelative("value").floatValue = 0;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
