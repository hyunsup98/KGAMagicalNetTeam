using UnityEngine;
//각도 기준 공격패턴 지정
public class DragonCombatState : BossStateBase
{
    private float attackCooldown = 3.0f;
    private float lastAttackTime = 0f;

    private bool isAttacking = false;
    private float attackEndTime = 0f;

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

        if (isAttacking)
        {
            if (Time.time > attackEndTime)
            {
                isAttacking = false;
                lastAttackTime = Time.time;     
                dragon.PlayAnimTrigger("Idle");
                dragon.FindRandomTarget();
            }
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

        //쿨
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            DecideAttackPattern(absAngle, dist);
            lastAttackTime = Time.time;
        }
    }

    //패턴
    private void DecideAttackPattern(float absAngle, float dist)
    {
        isAttacking = true;

        //후방
        if (absAngle > dragon.angleBackTail)
        {
            dragon.PlayAnimTrigger("TailWhip");
            attackEndTime = Time.time + 1.5f;
        }

        //전방
        else
        {
            //멀면
            if (dist > dragon.distLongRange)
            {
                dragon.PlayAnimTrigger("Breathe Fire");
                dragon.ShootFireball();
                attackEndTime = Time.time + 1.5f;
            }
            else if (dist > 7.0f)
            {
                if (absAngle <= dragon.angleFrontWide)
                {
                    //50%
                    int rand = Random.Range(0, 2);
                    if (rand == 0)
                    {
                        dragon.PlayAnimTrigger("Fire Head 2");
                        attackEndTime = Time.time + 3.0f;
                    }
                    else
                    {
                        isAttacking = false;
                        stateMachine.ChangeState(new DragonChaseState(dragon, stateMachine));
                    }
                }
                else
                {
                    //정면 근접
                    if (absAngle <= dragon.angleFrontWide)
                    {
                        int rand = Random.Range(0, 2);
                        if (rand == 0)
                        {
                            dragon.PlayAnimTrigger("Attack1");
                            attackEndTime = Time.time + 1.2f;
                        }
                        else
                        {
                            dragon.PlayAnimTrigger("Attack2");
                            attackEndTime = Time.time + 1.2f;
                        }
                    }
                    else
                    {
                        isAttacking = false;
                        stateMachine.ChangeState(new DragonChaseState(dragon, stateMachine));
                    }
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
