using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

//보스 본체
public class DragonAI : MonoBehaviourPunCallbacks, IDamageable
{
    public StateMachine stateMachine;
    [Header("보스 스탯")]

    public float maxHP = 4000f;
    public float currentHP;
    public bool isPhaseTwo = false; //2페

    public float runSpeed = 8.0f;
    public float rotSpeed = 5.0f;

    [Header("컴포넌트")]
    public Animator anim;
    public NavMeshAgent agent;

    [Header("공격 판정 MeleeWeapon 필요")]
    public MeleeWeapon jawWeapon;   //이빨
    public MeleeWeapon leftClaw;    //앞발
    public MeleeWeapon rightClaw;    //앞발
    public MeleeWeapon tailWeapon;  //꼬리

    [Header("데스윙")]
    public float bodyCrashRadius = 5.0f;
    public float flightDistance = 40.0f;

    //가장 가까운?
    public Transform targetPlayer;

    [Header("전투 설정")]
    [Tooltip("물기/앞발 각도")]
    public float angleFrontNarrow = 30.0f;

    [Tooltip("광역 브레스 각")]
    public float angleFrontWide = 90.0f;

    [Tooltip("꼬리치기 각도")]
    public float angleBackTail = 90.0f; // 90이면 90도~180도(완전 뒤) = 후방 180도

    [Tooltip("직선 브레스 거리")]
    public float distLongRange = 7.0f;

    [Tooltip("전투 이탈")]
    public float distCombatExit = 12.0f;


    private void Awake()
    {
        stateMachine = new StateMachine();
        currentHP = maxHP;

        //데미지 주입
        if (jawWeapon) jawWeapon.SetDamage(15);
        if (leftClaw) leftClaw.SetDamage(10);
        if (rightClaw) rightClaw.SetDamage(10);
        if (tailWeapon) tailWeapon.SetDamage(12);
    }

    private void Start()
    {
        stateMachine.InitState(new DragonSleepState(this, stateMachine));
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        stateMachine.CurrentState?.Execute();
    }

    public void WakeUp()
    {
        photonView.RPC(nameof(RpcOnWakeUp), RpcTarget.All);
    }
    void RpcOnWakeUp()
    {
        //연출(포효?정도 근데 시네머신 쓰는 게 나을 거 같음)
        if (PhotonNetwork.IsMasterClient)
        {
            stateMachine.ChangeState(new DragonChaseState(this, stateMachine));
        }
    }

    //가까운 플레이어
    public void FindClosestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = float.MaxValue;
        Transform bestTarget = null;

        foreach (var p in players)
        {
            //플레이어 죽었는 지 체크
            if (p == null) continue;

            //커스텀프로퍼티 사망체크
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null)
            {
                bool isAlive = pv.Owner.GetProps<bool>(NetworkProperties.PLAYER_ALIVE);
                if (!isAlive) continue;
            }

            //거리
            float d = Vector3.SqrMagnitude(p.transform.position - transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                bestTarget = p.transform;
            }
        }
        targetPlayer = bestTarget;
    }

    //피격로직
    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;
        photonView.RPC(nameof(RpcSyncHP), RpcTarget.All, damage);
    }

    [PunRPC]
    void RpcSyncHP(float damage)
    {
        currentHP -= damage;
        //보스체력바 갱신 로직~~
        //없어도 되긴 함(2페 떄문에)

        //방장만 상태 판단
        if (PhotonNetwork.IsMasterClient)
        {
            //사망 체크
            if (currentHP <= 0)
            {
                stateMachine.ChangeState(new DragonDeadState(this, stateMachine));
                return;
            }

            //2phase
            if (!isPhaseTwo && currentHP <= maxHP * 0.5f)
            {
                isPhaseTwo = true;
                stateMachine.ChangeState(new DragonPhaseState(this, stateMachine));
            }
        }
    }

    public void PlayAnimTrigger(string triggerName)
    {
        photonView.RPC(nameof(RpcPlayAnimTrigger), RpcTarget.All, triggerName);
    }

    [PunRPC]
    void RpcPlayAnimTrigger(string triggerName)
    {
        if (anim != null) anim.SetTrigger(triggerName);
    }

    //이벤트호출용
    public void EnableWeaponHitbox(string weaponType)
    {
        switch (weaponType)
        {
            case "Jaw": if (jawWeapon) jawWeapon.EnableHitbox(); break;
            case "Claw": 
                if (leftClaw) leftClaw.EnableHitbox();
                if (leftClaw) leftClaw.EnableHitbox(); 
                break;
            case "Tail": if (tailWeapon) tailWeapon.EnableHitbox(); break;
        }
    }

    public void DisableWeaponHitbox()
    { 
        if (jawWeapon) jawWeapon.DisableHitbox(); 
        if (leftClaw) leftClaw.DisableHitbox();
        if (rightClaw) rightClaw.DisableHitbox();
        if (tailWeapon) tailWeapon.DisableHitbox();
    }


    private void OnDrawGizmosSelected()
    {
        //몸통 박치기 범위
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, bodyCrashRadius);
    }
}
