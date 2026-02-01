using Photon.Pun;
using UnityEngine;

public class MagicBlackHole : MagicAction
{
    private BlackHoleSO blackHoleData;

    public MagicBlackHole(BlackHoleSO data) : base(data)
    {
        this.blackHoleData = data;
    }

    public override void OnCast(Vector3 spawnPos, Vector3 targetPos, bool isLeftHand, int shooterID)
    {
        if (blackHoleData.itemPrefab != null)
        {
            Vector3 finalSpawnPos = targetPos + blackHoleData.spawnOffset;

            GameObject obj = PhotonNetwork.Instantiate("EffectPrefab/" + blackHoleData.itemPrefab.name, finalSpawnPos, blackHoleData.itemPrefab.transform.rotation);

            BlackHole bhLogic = obj.GetComponent<BlackHole>();
            if (bhLogic != null)
            {
                bhLogic.photonView.RPC(nameof(BlackHole.RPC_Setup), RpcTarget.All, shooterID);
            }
        }
    }
}