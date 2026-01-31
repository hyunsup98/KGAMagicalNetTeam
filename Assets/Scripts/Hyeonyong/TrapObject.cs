using Photon.Pun;
using System.Collections;
using UnityEngine;

public class TrapObject : MonoBehaviour
{
    //[SerializeField] LayerMask playerLayerMask;
    //[SerializeField] string playerLayerName = "Player";
    //int playerLayer = -1;
    [SerializeField] float damage;
    //[SerializeField] float damageCoolTime = 0f;
    //WaitForSeconds damageCoolTimeWait;
    //bool onDamage=false;
    //Coroutine checkOnDamageCoroutine;
    //private void Awake()
    //{
    //    damageCoolTimeWait = new WaitForSeconds(damageCoolTime);
    //    //playerLayer = LayerMask.NameToLayer(playerLayerName);
    //}
    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            return;
        damageable.TakeDamage(damage);
        //변신 이전엔 레이어가 달라서 그냥 pc 여부로 판단
        //PlayerController pc =other.GetComponent<PlayerController>();
        //if (pc != null)
        //{
        //    Debug.Log("플레이어 접촉 pc 보유");
        //    if (pc.photonView.IsMine)
        //    {
        //        Debug.Log("플레이어 접촉 pc 보유 내 포톤 뷰");
        //        if (!onDamage)
        //        {
        //            pc.TakeDamage(damage);
        //            Debug.Log("플레이어 접촉 pc 보유 내 포톤 뷰 맞았다");
        //            checkOnDamageCoroutine = StartCoroutine(CheckOnDamage());
        //        }
        //    }
        //}
        //else
        //{
            
        //}
    }

    //private void OnDisable()
    //{
    //    if (checkOnDamageCoroutine != null)
    //    {
    //        StopCoroutine(checkOnDamageCoroutine);
    //    }
    //}
    //IEnumerator CheckOnDamage()
    //{
    //    onDamage = true;
    //    yield return damageCoolTimeWait;
    //    onDamage = false;
    //    checkOnDamageCoroutine = null;
    //}
}
