using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class PolymorphProjectile : MonoBehaviourPun
{
    [SerializeField] private PolymorphMagicSO magicData; //인스펙터에서 SO 연결

    private Rigidbody rb;
    private bool hasHit = false;
    private int shooterActorNumber;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        //if (PhotonView.IsMine)
        {

        }
    }
}
