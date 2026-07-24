using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Objectives/Kill Objective"
)]
public sealed class KillObjectiveData :
    FavourObjectiveData
{
    [Header("Target")]

    [SerializeField]
    private CreatureDefinition creature;

    [SerializeField]
    [Min(1)]
    private int requiredKills = 1;

    [Header("Credit")]

    [SerializeField]
    [Range(0f, 1f)]
    private float minimumDamageShare = 0.5f;

    public CreatureDefinition Creature =>
        creature;

    public int RequiredKills =>
        Mathf.Max(
            1,
            requiredKills
        );

    public float MinimumDamageShare =>
        Mathf.Clamp01(
            minimumDamageShare
        );

    public override FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour)
    {
        return new KillObjectiveRuntime(
            this,
            favour
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        requiredKills =
            Mathf.Max(
                1,
                requiredKills
            );

        minimumDamageShare =
            Mathf.Clamp01(
                minimumDamageShare
            );

        if (creature == null)
        {
            Debug.LogWarning(
                $"KillObjective '{name}' saknar CreatureDefinition.",
                this
            );
        }
    }
#endif
}
