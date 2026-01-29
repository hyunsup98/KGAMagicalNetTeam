using Photon.Pun;
using UnityEngine;

public class MagicPolymorph : MagicAction
{
    private PolymorphMagicSO polymorphData;

    public MagicPolymorph(MagicDataSO data) : base(data)
    {
        //this.polymorphData = data;
    }

    public override void OnCast(Vector3 spawnPos, Vector3 targetPos, bool isLeftHand, int shooterID)
    {
        Vector3 finalSpawnPos = spawnPos;

        Vector3 direction = (targetPos - finalSpawnPos).normalized;

        if (polymorphData.itemPrefab != null)
        {
            //투사체 생성
            GameObject obj = PhotonNetwork.Instantiate("EffectPrefab/" + polymorphData.itemPrefab.name, 
                finalSpawnPos, Quaternion.LookRotation(direction));

            //투사체 스크립트 초기화
            PolymorphProjectile projectile = obj.GetComponent<PolymorphProjectile>();
            if (projectile != null)
            {
                //projectile.SetShooterActorNumber(shooterID);
            }
        }
    }
}
