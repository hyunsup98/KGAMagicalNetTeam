using UnityEngine;

public class DragonChaseState : BossStateBase
{

    public DragonChaseState(DragonAI dragon, StateMachine stateMachine) : base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = false;
            dragon.agent.speed = dragon.runSpeed;
        }

        dragon.PlayAnimTrigger("Walk");
    }
    public override void Execute()
    {
        //타겟재탐색
        if (dragon.targetPlayer == null)
        {
            dragon.FindClosestTarget();
            if (dragon.targetPlayer == null) return;
        }

        Vector3 myPos = dragon.transform.position;
        Vector3 targetPos = dragon.targetPlayer.position;

        myPos.y = 0;
        targetPos.y = 0;

        float dist = Vector3.Distance(myPos, targetPos);


        //공격 사거리 들어오면 전투로 전환
        if (dist <= dragon.attackRange)
        {
            stateMachine.ChangeState(new DragonCombatState(dragon, stateMachine));
        }
        if (dragon.agent != null && dragon.agent.enabled)
        {
            //계속 추격
            if (dragon.agent != null)
            {
                dragon.agent.SetDestination(dragon.targetPlayer.position);
            }
        }
    }

    public override void Exit() { }
}
