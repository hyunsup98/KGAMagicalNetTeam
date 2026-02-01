using UnityEngine;

public class DragonSleepState : BossStateBase
{
    public DragonSleepState(DragonAI dragon, StateMachine stateMachine) 
        : base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = true;
            dragon.agent.velocity = Vector3.zero;
        }

        dragon.PlayAnimTrigger("Idle");

        dragon.DisableWeaponHitbox();
    }
    public override void Execute()
    {
        //wakeUp 호출 대기
    }
    public override void Exit()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = false;
        }

        dragon.PlayAnimTrigger("Breathe Fire");
    }
}

