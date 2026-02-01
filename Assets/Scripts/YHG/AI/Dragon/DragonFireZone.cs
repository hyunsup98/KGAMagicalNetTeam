using Photon.Pun;
using UnityEngine;
using System.Collections;

public class DragonFireZone : MonoBehaviourPun
{
    [Header("설정")]
    public float lifeTime = 5.0f;      
    public float damagePerTick = 50f; 

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CoDestroy());
        }
    }
    IEnumerator CoDestroy()
    {
        yield return CoroutineManager.waitForSeconds(lifeTime);
        PhotonNetwork.Destroy(gameObject);
    }

    //장판 위에 있는동안 도트
    private void OnTriggerStay(Collider other)
    {
        //방장만수행중복방지
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damagePerTick * Time.deltaTime);
            }
        }
    }
}
