using UnityEngine;
using System.Collections;
public class DragonPhaseState : BossStateBase
{
    private float phaseDuration = 4.5f; 
    private float timer = 0f;

    public DragonPhaseState(DragonAI dragon, StateMachine stateMachine) : 
        base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = true;
            dragon.agent.velocity = Vector3.zero;
        }

        dragon.DisableWeaponHitbox();
        Collider bodyCol = dragon.GetComponent<Collider>();
        if (bodyCol != null) bodyCol.enabled = false;

        //2페진입
        dragon.PlayAnimTrigger("Reassemble");
        dragon.isPhaseTwo = true;

        dragon.StartCoroutine(CoPhaseProcess());
    }

    public override void Execute() { }
    private IEnumerator CoPhaseProcess()
    {
        yield return CoroutineManager.waitForSeconds(phaseDuration);

        //무적 해제
        Collider bodyCol = dragon.GetComponent<Collider>();
        if (bodyCol != null) bodyCol.enabled = true;
    }
    public override void Exit()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = false;
        }
    }
}
