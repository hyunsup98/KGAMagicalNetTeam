using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tornado : MonoBehaviourPun
{
    [Header("Data")]
    [SerializeField] private TornadoSO data;

    private HashSet<Rigidbody> activeTargets = new HashSet<Rigidbody>();
    private int shooterID;
    private Vector3 moveDirection;

    [PunRPC]
    public void RPC_Setup(int shooterID)
    {
        this.shooterID = shooterID;

        if (photonView.IsMine)
        {
            moveDirection = transform.forward;
            moveDirection.y = 0;
            moveDirection.Normalize();
            if (moveDirection == Vector3.zero) moveDirection = Vector3.forward;

            StartCoroutine(LifetimeRoutine());
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * data.rotationSpeed * Time.deltaTime, Space.World);

        if (photonView.IsMine)
        {
            MoveTornado();
        }
    }

    private void LateUpdate()
    {
        if (!photonView.IsMine) return;
        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, currentEuler.y, 0);
    }

    private void FixedUpdate()
    {
        ControlSatellites();
    }

    private void MoveTornado()
    {
        Vector3 nextPosition = transform.position + (moveDirection * data.moveSpeed * Time.deltaTime);

        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("Ground");
        if (layerMask == 0) layerMask = ~0;

        if (Physics.Raycast(nextPosition + Vector3.up * 5.0f, Vector3.down, out hit, 20.0f, layerMask))
        {
            nextPosition.y = hit.point.y;
        }
        transform.position = nextPosition;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!IsValidTarget(other)) return;
        if (!ChunkNodeCheck(other)) return;
        if (!other.TryGetComponent<IMagicInteractable>(out IMagicInteractable obj)) return;

        // 기본적으로는 부딪힌 녀석의 리지드바디를 가져옴
        Rigidbody targetRb = other.GetComponent<Rigidbody>();
        if (targetRb == null) return;

        if(obj.CheckInteractable(gameObject, data, shooterID))
        {
            // 대상이 래그돌 컨트롤러를 가지고 있다면, Root 대신 Hips를 타겟으로 교체
            HumanoidRagdollController ragdollCtrl = other.GetComponent<HumanoidRagdollController>();

            if (ragdollCtrl != null)
            {
                // 토네이도 상태 알림
                ragdollCtrl.SetTornadoState(true);

                // hip을 가져와서 타겟 변경
                Rigidbody hips = ragdollCtrl.GetRagdollHips();
                if (hips != null)
                {
                    targetRb = hips;
                }
            }

            // 중복이 아니면 타겟 리스트에 추가
            if (!activeTargets.Contains(targetRb))
            {
                activeTargets.Add(targetRb);

                obj.OnMagicInteract(gameObject, data, shooterID);
            }
        }
    }

    private bool IsValidTarget(Collider other)
    {
        PhotonView targetPV = other.GetComponentInParent<PhotonView>();

        if (targetPV == null) return true;

        if (targetPV.CompareTag("Player") || other.CompareTag("Player"))
        {
            // 자해인지 판단
            if (targetPV.OwnerActorNr == shooterID)
            {
                return false;
            }

            // 다른 플레이어라면 아군 오사판정 체크
            bool isFriendlyFireOn = PhotonNetwork.CurrentRoom.GetProps<bool>(NetworkProperties.FRIENDLYFIRE);
            return isFriendlyFireOn;
        }

        return true;
    }

    private bool ChunkNodeCheck(Collider other)
    {
        ChunkNode node = other.GetComponent<ChunkNode>();
        if (node == null) return true;
        else if (node.IsIndestructible) return false;

        return true;
    }

    private void OnTriggerExit(Collider other)
    {
        HumanoidRagdollController ragdollCtrl = other.GetComponent<HumanoidRagdollController>();
        if (ragdollCtrl != null)
        {
            Rigidbody hips = ragdollCtrl.GetRagdollHips();
            if (hips != null && activeTargets.Contains(hips))
            {
                ReleaseTarget(hips, false);
            }
        }
        else
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && activeTargets.Contains(rb))
            {
                ReleaseTarget(rb, false);
            }
        }
    }

    private void ControlSatellites()
    {
        List<Rigidbody> toRelease = new List<Rigidbody>();

        foreach (var rb in activeTargets)
        {
            if (rb == null) continue;

            Vector3 objectPos = rb.position;
            Vector3 offset = objectPos - transform.position;
            float distance = offset.magnitude;

            // 너무 높이 올라가면 놓아줌
            if (offset.y > data.releaseHeight)
            {
                toRelease.Add(rb);
                continue;
            }

            // 회전 및 인력 계산
            Vector3 dirToCenter = -offset.normalized;
            Vector3 horizontalDir = new Vector3(dirToCenter.x, 0, dirToCenter.z).normalized;
            Vector3 tangentDir = Vector3.Cross(horizontalDir, Vector3.up).normalized; // 회전 방향

            float distFactor = Mathf.Clamp01(distance / data.maxDistance);
            float currentSuction = Mathf.Lerp(data.suctionSpeed, data.suctionSpeed * 2.5f, distFactor);

            // 바닥에 끌리지 않게 높이 보정
            float heightFactor = (offset.y < 2.0f) ? 2.0f : 1.0f;
            float currentLift = data.liftSpeed * heightFactor;

            Vector3 targetVelocity = (tangentDir * data.orbitSpeed) + (horizontalDir * currentSuction);
            Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * data.captureStrength);
            newVelocity.y = currentLift; // Y축 힘 적용

            rb.linearVelocity = newVelocity;
        }

        foreach (var rb in toRelease)
        {
            ReleaseTarget(rb, true);
        }
    }

    private void ReleaseTarget(Rigidbody rb, bool applyForce)
    {
        if (activeTargets.Contains(rb))
        {
            activeTargets.Remove(rb);

            if (rb != null)
            {
                var ragdollCtrl = rb.GetComponentInParent<HumanoidRagdollController>();
                if (ragdollCtrl != null)
                {
                    ragdollCtrl.SetTornadoState(false);
                }

                rb.useGravity = true;
                rb.GetComponent<IPhysicsObject>()?.OnStatusChange(false);

                if (applyForce)
                {
                    Vector3 explodeDir = (rb.position - transform.position).normalized + Vector3.up;
                    rb.AddForce(explodeDir * data.ejectForce, ForceMode.Impulse);
                }
            }
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(data.duration);

        List<Rigidbody> finalTargets = new List<Rigidbody>(activeTargets);
        foreach (var rb in finalTargets) ReleaseTarget(rb, true);
        activeTargets.Clear();

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}