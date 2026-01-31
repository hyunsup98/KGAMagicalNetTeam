using BzKovSoft.RagdollTemplate.Scripts.Charachter;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using System.Collections;
//using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(BzRagdoll))]
public class HumanoidRagdollController : MonoBehaviourPun, IMagicInteractable
{
    [Header("컴포넌트부착")]
    [SerializeField] private BzRagdoll bzRagdoll;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    private Collider rootCollider;

    [Header("설정값")]
    [SerializeField] private float knockDownDuration = 1.5f; //최소 기절 시간
    [SerializeField] private float getUpAnimationDuration = 2.5f; //일어나는 애니메이션 길이

    //BaseAI 연결 일단 얘도 임시긴함
    [SerializeField] private BaseAI baseAI;

    //상태 관리용
    private bool isRagdollActive = false;
    private float ragdollStartTime;
    private bool isInTornado = false;

    public bool IsInTornado => isInTornado;

    private bool isRecovering = false;
    private Coroutine getUpCoroutine;


    private void Awake()
    {
        bzRagdoll = GetComponent<BzRagdoll>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        baseAI = GetComponent<BaseAI>();
        rootCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (baseAI != null && baseAI.currentNetworkState == BaseAI.AIStateID.Dead) return;

        if (isInTornado)
        {
            ragdollStartTime = Time.time;
            return;
        }

        if (isRecovering) return;

        //일단 대기
        if (Time.time - ragdollStartTime < knockDownDuration) return;

        //방장이 NPC의 기상 타이밍 결정
        //안 그러면 각자 화면에서 따로 일어나서 위치가 꼬임
        if (PhotonNetwork.IsMasterClient && isRagdollActive)
        {
            CheckGetUpCondition();
        }

        if (isRagdollActive)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null) transform.position = hips.position;
        }
    }

    //외부용 피격 메서드
    public void ApplyRagdoll(Vector3 force, bool forceReset = false)
    {
        bool isDead = (baseAI != null && baseAI.currentNetworkState == BaseAI.AIStateID.Dead); ;
        if (isRagdollActive && !forceReset && !isDead) return; // 이신빈
        //이미 다운된 상태면 무시 (누워있어도 계속 날아가게 처리?? 일단 보류)
        //무콤 필요하면 주석해제 하고 테스트

        photonView.RPC(nameof(RpcActivateRagdoll), RpcTarget.All, force);
    }

    //피격 RPC
    [PunRPC]
    private void RpcActivateRagdoll(Vector3 force)
    {
        if (getUpCoroutine != null) StopCoroutine(getUpCoroutine); // 이신빈

        isRagdollActive = true;
        isRecovering = false;
        ragdollStartTime = Time.time;

        if (baseAI != null) baseAI.IsKnockedDown = true;

        if (agent != null && agent.enabled)
        {
            agent.velocity = Vector3.zero;
            agent.updatePosition = false;
            agent.enabled = false;
        }

        if (animator != null) animator.enabled = false; // 이신빈

        if (rootCollider != null) rootCollider.enabled = false; // 이신빈

        //에셋 래그돌 시스템<- 어댑터 있음
        bzRagdoll.IsRagdolled = true;

        //위치값 가져와 물리 적용
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips != null)
        {
            Rigidbody hipsRigid = hips.GetComponent<Rigidbody>();
            if (hipsRigid != null)
            {
                hipsRigid.linearVelocity = Vector3.zero; // 이신빈
                hipsRigid.AddForce(force, ForceMode.Impulse);
            }
        }
    }


    //기상 체크(방장용)
    private void CheckGetUpCondition()
    {
        if (isRecovering) return;

        //일단 3초
        if (Time.time - ragdollStartTime < knockDownDuration) return;

        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
        {
            return; //방어 코드
        }
        Rigidbody hipsRigid = hips.GetComponent<Rigidbody>();
        if (hipsRigid == null)
        {
            return;
        }

        if (hipsRigid.linearVelocity.magnitude < 0.5f)
        {
            //위치보정, NavMesh 위 유효 좌표를 찾음
            Vector3 getUpPos = hips.position;
            if (NavMesh.SamplePosition(getUpPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                getUpPos = hit.position;
            }

            //일어나는 중
            isRecovering = true;

            //안전한 위치를 모두에게 전송하며 기상 명령(텔포 좀 할 듯)?
            photonView.RPC(nameof(RpcGetUp), RpcTarget.All, getUpPos);
        }
    }

    //기상실행 RPC -> 애니메이션 시간에 맞춰 코루틴으로?
    [PunRPC]
    private void RpcGetUp(Vector3 syncPosition)
    {
        if (baseAI != null && baseAI.currentNetworkState == BaseAI.AIStateID.Dead) return;
        getUpCoroutine = StartCoroutine(CoGetUpProcess(syncPosition));
    }

    private IEnumerator CoGetUpProcess(Vector3 pos)
    {
        //래그돌 해제 -> 기상 애니메이션 블렌딩 시작
        bzRagdoll.IsRagdolled = false;

        //애니메이션 재생 대기
        yield return CoroutineManager.waitForSeconds(getUpAnimationDuration);

        if (isInTornado) yield break;

        transform.position = pos;
        if (rootCollider != null) rootCollider.enabled = true;

        if (animator != null) animator.enabled = true;
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.Warp(pos);
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        if (PhotonNetwork.IsMasterClient) baseAI.OnRecoverFromKnockdown();
        else baseAI.IsKnockedDown = false;

        isRagdollActive = false;
        isRecovering = false;
    }

    public void OnMagicInteract(GameObject magic, MagicDataSO data, int attackerActorNr)
    {
        switch (data.magicType)
        {
            case MagicType.Fireball:
            case MagicType.Lightning:
                Vector3 dir = (transform.position - magic.transform.position).normalized;
                Vector3 force = (dir + Vector3.up * 0.5f) * data.knockbackForce;
                ApplyRagdoll(force);
                if (baseAI != null) baseAI.TakeDamage(data.damage);
                break;

            case MagicType.Tornado:
                TornadoReaction(data);
                break;
        }
    }

    public void FireballReaction(GameObject magic, MagicDataSO data, int attackerActorNr)
    {
        Vector3 dir = (transform.position - magic.transform.position).normalized;
        Vector3 force = (dir + Vector3.up * 0.5f) * data.knockbackForce;
        ApplyRagdoll(force);

        if (baseAI != null)
            baseAI.TakeDamage(data.damage);
    }

    public void LightningStrikeReaction(GameObject magic, MagicDataSO data, int attackerActorNr)
    {
        Vector3 dir = (transform.position - magic.transform.position).normalized;
        Vector3 force = (dir + Vector3.up * 0.5f) * data.knockbackForce;
        ApplyRagdoll(force);

        if (baseAI != null)
            baseAI.TakeDamage(data.damage);
    }

    public void TornadoReaction(MagicDataSO data)
    {
        StartCoroutine(CoTornadoReaction(data));
    }

    private IEnumerator CoTornadoReaction(MagicDataSO data)
    {
        if (baseAI != null) baseAI.TakeDamage(0);

        // 강제 래그돌화
        ApplyRagdoll(Vector3.zero, true);

        // 물리 엔진 초기화 대기
        yield return new WaitForFixedUpdate();

        // 힘 적용
        Rigidbody hips = GetRagdollHips();
        if (hips != null)
        {
            hips.WakeUp();
            hips.linearVelocity = Vector3.zero;
            hips.AddForce(Vector3.up * 8.0f, ForceMode.Impulse);
        }
    }

    public bool CheckInteractable(GameObject magic, MagicDataSO data, int attackerActorNr)
    {
        if (data.magicType == MagicType.Tornado) return true;

        bool isDead = (baseAI != null && baseAI.currentNetworkState == BaseAI.AIStateID.Dead);
        if (isRagdollActive && !isDead) return false;

        return true;
    }

    public void SetTornadoState(bool state)
    {
        isInTornado = state;
        if (state)
        {
            ragdollStartTime = Time.time;
            if (getUpCoroutine != null) StopCoroutine(getUpCoroutine);
        }
    }

    public Rigidbody GetRagdollHips()
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips != null) return hips.GetComponent<Rigidbody>();
        return GetComponent<Rigidbody>();
    }
}
