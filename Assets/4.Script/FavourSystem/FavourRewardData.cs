using UnityEngine;

public abstract class FavourRewardData :
    ScriptableObject
{
    public abstract void Grant(
        PlayerFavourManager manager);
}
