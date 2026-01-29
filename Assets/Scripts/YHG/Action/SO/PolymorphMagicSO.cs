using UnityEngine;

[CreateAssetMenu(fileName = "New Fireball", menuName = "Game/Polymorph")]
public class PolymorphMagicSO : MagicDataSO
{
    public float projectileSpeed = 15f;
    public float maxLifetime = 3.0f;
    public GameObject hitEffectPrefab;


    [Header("디버프 정보")]
    public DebuffInfo debuffInfo;

    public override ActionBase CreateInstance()
    {
        return new MagicPolymorph(this);
    }
}
