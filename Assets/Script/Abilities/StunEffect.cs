using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "RPG/Effects/Stun")]
public class StunEffect : AbilityEffect
{
    public float duration = 2f;

    public override void Apply(CharacterStats caster, CharacterStats target)
    {
        if (target == null)
            return;

        BuffSystem buffs = target.GetComponent<BuffSystem>();

        if (buffs != null)
        {
            buffs.ApplyEffect(this, caster);
        }
    }

    IEnumerator RemoveAfterDuration(CharacterStats target)
    {
        yield return new WaitForSeconds(duration);
        target.RemoveStun();
    }

}
