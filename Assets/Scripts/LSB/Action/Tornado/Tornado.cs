using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tornado : MonoBehaviourPun
{
    [Header("Data Settings")]
    [SerializeField] private TornadoSO data; // 토네이도 데이터

    // 현재 토네이도에 잡혀있는 물리 객체들
    private HashSet<Rigidbody> activeTargets = new HashSet<Rigidbody>();

    private int shooterID;         // 스킬 시전자의 ActorNumber (자해 방지용)
    private Vector3 moveDirection; // 토네이도 진행 방향

    List<Rigidbody> toRelease = new List<Rigidbody>(128);    // 놓아줄 목록
    List<Rigidbody> toSwapAdd = new List<Rigidbody>(128);    // 교체 후 추가할 목록
    List<Rigidbody> toSwapRemove = new List<Rigidbody>(128); // 교체 후 제거할 목록

    /// <summary>
    /// 토네이도 생성 시 호출되는 초기화 RPC 함수
    /// 발사 정보를 설정하고 수명을 관리하는 코루틴을 시작
    /// </summary>
    [PunRPC]
    public void RPC_Setup(int shooterID)
    {
        this.shooterID = shooterID;

        // 소유자인 경우에만 이동 방향 설정 및 수명 관리 시작
        if (photonView.IsMine)
        {
            moveDirection = transform.forward;
            moveDirection.y = 0; // 수평 이동 고정
            moveDirection.Normalize();

            // 방향 벡터가 0일 경우 기본값 설정
            if (moveDirection == Vector3.zero) moveDirection = Vector3.forward;

            StartCoroutine(LifetimeRoutine());
        }
    }

    private void Update()
    {
        // 토네이도 모델 자체를 빙글빙글 돌림
        transform.Rotate(Vector3.up * data.rotationSpeed * Time.deltaTime, Space.World);

        // 소유자 권한이 있는 경우에만 이동 로직 수행
        if (photonView.IsMine)
        {
            MoveTornado();
        }
    }

    private void LateUpdate()
    {
        // 네트워크 동기화 시, 소유자가 아닌 클라이언트에서도 회전값 보정을 위해 사용
        if (!photonView.IsMine) return;

        // Y축 회전만 반영함
        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, currentEuler.y, 0);
    }

    private void FixedUpdate()
    {
        ControlSatellites();
    }

    /// <summary>
    /// 토네이도를 전방으로 이동시키며, 지형 높낮이에 맞춰 Y축 위치를 보정
    /// </summary>
    private void MoveTornado()
    {
        Vector3 nextPosition = transform.position + (moveDirection * data.moveSpeed * Time.deltaTime);

        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("Ground");
        if (layerMask == 0) layerMask = ~0; // Ground 레이어가 없으면 모든 레이어 검사

        // 위에서 아래로 레이를 쏴서 바닥 높이를 감지 (지형 굴곡 따라가기)
        if (Physics.Raycast(nextPosition + Vector3.up * 5.0f, Vector3.down, out hit, 20.0f, layerMask))
        {
            nextPosition.y = hit.point.y;
        }
        transform.position = nextPosition;
    }

    /// <summary>
    /// 물체가 토네이도 범위에 들어왔을 때 호출됩니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 파편 처리
        if (TryChunkEnter(other)) return;

        // 상호작용 가능한 객체인지 확인
        IMagicInteractable interactableObj = other.GetComponentInParent<IMagicInteractable>();
        if (interactableObj == null) return;

        // 타겟 유효성 검사
        if (!TryValidateTarget(other, out bool isPlayer)) return;

        // 물리 타겟 가져오기
        Rigidbody targetRb = other.GetComponent<Rigidbody>();
        if (targetRb == null) targetRb = other.GetComponentInParent<Rigidbody>();
        if (targetRb == null) return;

        // 상호작용 가능 여부 확인
        if (interactableObj.CheckInteractable(gameObject, data, shooterID))
        {
            HumanoidRagdollController ragdollCtrl = other.GetComponentInParent<HumanoidRagdollController>();

            if (ragdollCtrl != null)
            {
                ragdollCtrl.SetTornadoState(true); // 토네이도에 잡힘 상태 설정

                // Root RigidBody 대신 실제 물리 연산이 일어나는 Hips로 교체
                Rigidbody hips = ragdollCtrl.GetRagdollHips();
                if (hips != null)
                {
                    targetRb = hips;
                }
            }

            // 타겟 리스트 등록 및 효과 실행
            if (!activeTargets.Contains(targetRb))
            {
                activeTargets.Add(targetRb);
                interactableObj.OnMagicInteract(gameObject, data, shooterID);
            }
        }
    }

    /// <summary>
    /// 파편인지 확인하고 처리했다면 true를 반환
    /// </summary>
    private bool TryChunkEnter(Collider other)
    {
        ChunkNode chunk = other.GetComponentInParent<ChunkNode>();
        if (chunk == null) return false;

        // 앵커 여부 등 확인
        if (chunk.CheckInteractable(gameObject, data, shooterID))
        {
            chunk.OnMagicInteract(gameObject, data, shooterID);

            Rigidbody chunkRb = chunk.GetComponent<Rigidbody>();
            if (chunkRb == null) chunkRb = other.GetComponent<Rigidbody>();

            if (chunkRb != null && !activeTargets.Contains(chunkRb))
            {
                activeTargets.Add(chunkRb);
                return true; // 처리 완료
            }
        }
        return false;
    }

    /// <summary>
    /// 공격 가능한 대상인지 검사합
    /// </summary>
    /// <param name="other">충돌한 콜라이더</param>
    /// <param name="isPlayer">대상이 플레이어인지 여부를 반환</param>
    /// <returns>공격 가능하면 true</returns>
    private bool TryValidateTarget(Collider other, out bool isPlayer)
    {
        isPlayer = false;

        // PV가 없으면 네트워크 객체가 아님
        PhotonView targetPV = other.GetComponentInParent<PhotonView>();
        if (targetPV == null) return true;

        // 플레이어인지 체크
        bool tagIsPlayer = other.CompareTag("Player") || targetPV.gameObject.CompareTag("Player");

        if (tagIsPlayer)
        {
            isPlayer = true; // 플래그 설정

            // 자해 방지
            if (targetPV.OwnerActorNr == shooterID) return false;

            // 아군 사격 옵션 확인
            bool isFriendlyFireOn = PhotonNetwork.CurrentRoom.GetProps<bool>(NetworkProperties.FRIENDLYFIRE);
            if (!isFriendlyFireOn) return false;
        }

        // 플레이어가 아니고 PV가 있다면 AI거나 마법

        return true;
    }

    /// <summary>
    /// 잡힌 물체들을 회전시키고(Orbit), 끌어당기고(Suction), 띄우는(Lift) 물리 연산을 수행
    /// </summary>
    private void ControlSatellites()
    {
        toRelease.Clear();
        toSwapAdd.Clear();
        toSwapRemove.Clear();

        foreach (var rb in activeTargets)
        {
            if (rb == null) continue;

            Vector3 offset = rb.position - transform.position;
            float distance = offset.magnitude;

            // 방생 조건 체크: 너무 멀어지거나 너무 높이 올라가면 놓아줌
            if (distance > data.maxDistance * 2.0f || offset.y > data.releaseHeight)
            {
                toRelease.Add(rb);
                continue;
            }

            // Kinematic 상태가 들어왔다면 Dynamic으로 교체 시도
            if (rb.isKinematic)
            {
                // 파편일 경우 Unfreeze
                var chunk = rb.GetComponent<ChunkNode>();
                if (chunk != null && chunk.IsFrozen && !chunk.IsIndestructible)
                    chunk.Unfreeze();

                // 래그돌일 경우 Hips를 추가
                var ctrl = rb.GetComponent<HumanoidRagdollController>();
                if (ctrl == null) ctrl = rb.GetComponentInParent<HumanoidRagdollController>();

                if (ctrl != null)
                {
                    Rigidbody hips = ctrl.GetRagdollHips();
                    if (hips != null && hips != rb && !hips.isKinematic)
                    {
                        toSwapRemove.Add(rb);
                        toSwapAdd.Add(hips);
                        continue; // 교체했으므로 이번 루프는 건너뜀
                    }
                }
            }

            // 벡터 계산
            Vector3 dirToCenter = -offset.normalized;                                        // 중심으로 향하는 방향
            Vector3 horizontalDir = new Vector3(dirToCenter.x, 0, dirToCenter.z).normalized; // 수평 방향
            Vector3 tangentDir = Vector3.Cross(horizontalDir, Vector3.up).normalized;        // 회전 방향

            // 힘의 세기 계산
            float distFactor = Mathf.Clamp01(distance / data.maxDistance);
            // 거리가 멀수록 더 강하게 빨아들임
            float currentSuction = Mathf.Lerp(data.suctionSpeed, data.suctionSpeed * 2.5f, distFactor);

            // 바닥에 끌리지 않게 최소 높이 보정
            float heightFactor = (offset.y < 2.0f) ? 2.0f : 1.0f;
            float currentLift = data.liftSpeed * heightFactor;

            // 최종 속도 적용 Lerp를 사용하여 부드럽게 가속
            Vector3 targetVelocity = (tangentDir * data.orbitSpeed) + (horizontalDir * currentSuction);
            Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * data.captureStrength);
            newVelocity.y = currentLift; // Y축은 강제로 띄움

            rb.linearVelocity = newVelocity;
        }

        // 리스트 갱신 처리
        foreach (var r in toSwapRemove) activeTargets.Remove(r);
        foreach (var a in toSwapAdd) activeTargets.Add(a);
        foreach (var rb in toRelease) ReleaseTarget(rb, true);
    }

    /// <summary>
    /// 타겟을 토네이도에서 해방시킴 마지막 튕겨나가는 힘을 적용
    /// </summary>
    private void ReleaseTarget(Rigidbody rb, bool applyForce)
    {
        if (activeTargets.Contains(rb))
        {
            activeTargets.Remove(rb);
            if (rb != null)
            {
                // 래그돌 컨트롤러에 상태 알려줌
                var ragdollCtrl = rb.GetComponentInParent<HumanoidRagdollController>();
                if (ragdollCtrl != null) ragdollCtrl.SetTornadoState(false);

                // 물리 상태 복구
                rb.useGravity = true;
                rb.GetComponent<IPhysicsObject>()?.OnStatusChange(false);

                // 튕겨나가는 물리 효과 적용
                if (applyForce)
                {
                    Vector3 explodeDir = (rb.position - transform.position).normalized + Vector3.up;
                    rb.AddForce(explodeDir * data.ejectForce, ForceMode.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// 토네이도의 수명을 관리 시간이 다 되면 모든 타겟을 놓고 소멸
    /// </summary>
    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(data.duration);

        // 잡고 있던 모든 타겟 방생
        List<Rigidbody> finalTargets = new List<Rigidbody>(activeTargets);
        foreach (var rb in finalTargets) ReleaseTarget(rb, true);
        activeTargets.Clear();

        // 오브젝트 파괴
        if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
    }
}