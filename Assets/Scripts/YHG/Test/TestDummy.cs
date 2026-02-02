using UnityEngine;
using Photon.Pun;

public class TestDummy : MonoBehaviourPun, IDamageable
{

    public void TakeDamage(float damage)
    {
        Debug.Log($"피격 성공, 드래곤 {damage}  ");
    }

    [PunRPC]
    public void RpcTakeDamage(float damage)
    {
        TakeDamage(damage);
    }
}