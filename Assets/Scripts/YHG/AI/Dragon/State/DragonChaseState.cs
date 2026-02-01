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

        //거리
        float dist = Vector3.Distance(dragon.transform.position, dragon.targetPlayer.position);

        //공격 사거리 들어오면 전투로 전환
        if (dist <= 4.0f)//하드코딩
        {
            stateMachine.ChangeState(new DragonCombatState(dragon, stateMachine));
        }
        else
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
