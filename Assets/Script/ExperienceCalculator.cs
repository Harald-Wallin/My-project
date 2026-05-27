using UnityEngine;

public static class ExperienceCalculator
{
    public static int CalculateExp(int monsterLevel, int playerLevel)
    {
        float exp = 10f
            + monsterLevel * 1.5f
            - playerLevel * 1.1f;

        exp = Mathf.Max(0, Mathf.RoundToInt(exp));
        return (int)exp;
    }
}

