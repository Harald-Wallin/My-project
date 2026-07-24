using UnityEngine;

public abstract class FavourRequirementData :
    ScriptableObject
{
    public abstract bool IsMet(
        PlayerFavourManager manager);
}
