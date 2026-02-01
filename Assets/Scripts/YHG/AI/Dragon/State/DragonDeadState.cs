using UnityEngine;
using Photon.Pun;
using System.Collections;

public class DragonDeadState : BossStateBase
{
    private float destroyDelay = 10.0f;
    public DragonDeadState(DragonAI dragon, StateMachine stateMachine)
        : base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = true;
            dragon.agent.enabled = false;
        }

        dragon.DisableWeaponHitbox();

        Collider bodyCol = dragon.GetComponent<Collider>();
        if (bodyCol != null) bodyCol.enabled = false;

        dragon.PlayAnimTrigger("Collapse");


        //사망 처리 코루틴
        if (PhotonNetwork.IsMasterClient)
        {
            dragon.StartCoroutine(CoDeathProcess());
        }
    }
    public override void Execute() { }
    private IEnumerator CoDeathProcess()
    {
        yield return CoroutineManager.waitForSeconds(destroyDelay);

        PhotonNetwork.Destroy(dragon.gameObject);

        //클리어? 보상?
    }

    public override void Exit() { }

}
