using UnityEngine;
//각도 기준 공격패턴 지정
public class DragonCombatState : BossStateBase
{
    private float attackCooldown = 3.0f;
    private float trackingDuration = 2.0f;

    private float lastAttackTime = 0f;

    public DragonCombatState(DragonAI dragon, StateMachine stateMachine) 
        : base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = true;
            dragon.agent.velocity = Vector3.zero;
            dragon.agent.updateRotation = false;
        }

        lastAttackTime = Time.time - 1.0f;
        dragon.PlayAnimTrigger("Idle");
    }

    public override void Execute()
    {
        if (dragon.targetPlayer == null)
        {
            stateMachine.ChangeState(new DragonChaseState(dragon, stateMachine));
            return;
        }

        Vector3 toTarget = dragon.targetPlayer.position - dragon.transform.position;
        float dist = toTarget.magnitude;
        float angle = Vector3.SignedAngle(dragon.transform.forward, toTarget, Vector3.up);
        float absAngle = Mathf.Abs(angle);

        //멀면 추격
        if (dist > dragon.distCombatExit)
        {
            stateMachine.ChangeState(new DragonChaseState(dragon, stateMachine));
            return;
        }

        //쿨타임은 3초 회전 2초
        if (Time.time < lastAttackTime + trackingDuration)
        {
            RotateTowardsTarget();
        }


        //쿨
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            DecideAttackPattern(absAngle, dist);
            lastAttackTime = Time.time;
        }
    }

    private void RotateTowardsTarget()
    {
        if (dragon.targetPlayer == null) return;

        Vector3 direction = (dragon.targetPlayer.position - dragon.transform.position).normalized;
        direction.y = 0; //y축 회전 방지
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            //부드럽게 회전 (Time.deltaTime * 회전속도)
            dragon.transform.rotation = Quaternion.Slerp(dragon.transform.rotation, lookRotation, Time.deltaTime * dragon.rotSpeed);
        }
    }
    //패턴
    private void DecideAttackPattern(float absAngle, float dist)
    {
        //후방
        if (absAngle > dragon.angleBackTail)
        {
            dragon.PlayAnimTrigger("TailWhip");
        }
        //전방
        else
        {
            //멀면
            if (dist > dragon.distLongRange)
            {
                dragon.PlayAnimTrigger("Breathe Fire");
            }

            else
            {
                if (absAngle <= dragon.angleFrontNarrow)
                {
                    int rand = Random.Range(0, 2);
                    if (rand == 0) dragon.PlayAnimTrigger("Attack1"); //물기
                    else dragon.PlayAnimTrigger("Attack2"); //앞발
                }
                //정면 사이드 브레스
                else if (absAngle <= dragon.angleFrontWide)
                {
                    dragon.PlayAnimTrigger("Fire Head 2");
                }
                else
                {
                    stateMachine.ChangeState(new DragonChaseState(dragon, stateMachine));
                }
            }
        }
    }
    public override void Exit()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = false;
            dragon.agent.updateRotation = true;
        }
    }
}
