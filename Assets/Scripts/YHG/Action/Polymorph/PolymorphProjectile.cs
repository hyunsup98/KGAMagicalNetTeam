using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class PolymorphProjectile : MonoBehaviourPun
{
    [SerializeField] private PolymorphMagicSO magicData; //인스펙터에서 SO 연결

    private Rigidbody rb;
    private bool hasExploded = false;
    private int shooterActorNumber;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (photonView.IsMine)
        {
            rb.useGravity = false;
            rb.linearVelocity = transform.forward * magicData.projectileSpeed;
            Invoke(nameof(DestroySelf), magicData.maxLifetime);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        //주인 x, 터짐 x, 트리거끼리 충돌이면 무시
        if (!photonView.IsMine || hasExploded) return;

        //발사자 본인 충돌 방지
        PhotonView targetPv = other.GetComponent<PhotonView>();
        if (targetPv != null && targetPv.OwnerActorNr == shooterActorNumber) return;

        hasExploded = true;

        photonView.RPC(nameof(RPC_PolyExplode), RpcTarget.All, transform.position);

        PhotonNetwork.Destroy(gameObject);
    }


    [PunRPC]
    private void RPC_PolyExplode(Vector3 explosionPos)
    { 
        if (magicData.hitEffectPrefab != null)
        {
            Instantiate(magicData.hitEffectPrefab, explosionPos, Quaternion.identity);
        }

        //광역으로수정
        Collider[] colliders = Physics.OverlapSphere(explosionPos, magicData.radius);

        foreach (Collider hit in colliders)
        {
            //IDebuffable 찾기
            IDebuffable target = hit.GetComponent<IDebuffable>();
            if (target == null) target = hit.GetComponentInParent<IDebuffable>();

            if (target != null)
            {
                target.ApplyDebuff(magicData.debuffInfo);
            }
        }
    }



    private void DestroySelf()
    {
        if (gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void SetShooterActorNumber(int actorNumber)
    {
        shooterActorNumber = actorNumber;
    }
}
