using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BlackHole : MonoBehaviourPun
{
    [SerializeField] private BlackHoleSO data;

    private int shooterID;
    private HashSet<Rigidbody> activeTargets = new HashSet<Rigidbody>();
    private SphereCollider triggerCollider;

    List<Rigidbody> toRelease = new List<Rigidbody>(128);
    List<Rigidbody> toSwapAdd = new List<Rigidbody>(128);
    List<Rigidbody> toSwapRemove = new List<Rigidbody>(128);

    private void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
    }

    [PunRPC]
    public void RPC_Setup(int shooterID)
    {
        this.shooterID = shooterID;

        if (triggerCollider != null && data != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.radius = data.radius;
        }

        Debug.Log("블랙홀 생성됨");

        if (photonView.IsMine)
        {
            StartCoroutine(LifeCycleRoutine());
        }

        if (data != null && data.magicSound != null)
            SoundManager.Instance.PlaySFX(data.magicSound, 1f, 100f, transform.position);
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            PullTargets();
        }
    }

    private void PullTargets()
    {
        if (data == null) return;

        toRelease.Clear();
        toSwapAdd.Clear();
        toSwapRemove.Clear();

        foreach (var rb in activeTargets)
        {
            if (rb == null)
            {
                toRelease.Add(rb);
                continue;
            }

            float distance = Vector3.Distance(transform.position, rb.position);
            if (distance > data.radius * 2.0f)
            {
                toRelease.Add(rb);
                continue;
            }

            if (rb.isKinematic)
            {
                var chunk = rb.GetComponent<ChunkNode>();
                if (chunk != null && chunk.IsFrozen && !chunk.IsIndestructible)
                    chunk.Unfreeze();

                var ctrl = rb.GetComponent<HumanoidRagdollController>();
                if (ctrl == null) ctrl = rb.GetComponentInParent<HumanoidRagdollController>();

                if (ctrl != null)
                {
                    Rigidbody hips = ctrl.GetRagdollHips();
                    if (hips != null && hips != rb && !hips.isKinematic)
                    {
                        toSwapRemove.Add(rb);
                        toSwapAdd.Add(hips);
                        continue;
                    }
                }
            }

            Vector3 offset = transform.position - rb.position;
            Vector3 direction = offset.normalized;

            Vector3 tangent = Vector3.Cross(Vector3.up, direction).normalized;

            Vector3 targetVelocity = (direction * data.pullForce) + (tangent * data.rotationForce);

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 2f);
        }

        foreach (var r in toSwapRemove) activeTargets.Remove(r);
        foreach (var a in toSwapAdd) activeTargets.Add(a);
        foreach (var r in toRelease) activeTargets.Remove(r);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (data == null) return;

        if (TryHandleChunk(other)) return;

        IMagicInteractable interactableObj = other.GetComponentInParent<IMagicInteractable>();
        if (interactableObj == null) return;

        if (!TryValidateTarget(other)) return;

        Rigidbody targetRb = other.GetComponent<Rigidbody>();
        if (targetRb == null) targetRb = other.GetComponentInParent<Rigidbody>();
        if (targetRb == null) return;

        if (interactableObj.CheckInteractable(gameObject, data, shooterID))
        {
            HumanoidRagdollController ragdollCtrl = other.GetComponentInParent<HumanoidRagdollController>();
            if (ragdollCtrl != null)
            {
                ragdollCtrl.SetTornadoState(true);
                Rigidbody hips = ragdollCtrl.GetRagdollHips();
                if (hips != null) targetRb = hips;
            }

            if (!activeTargets.Contains(targetRb))
            {
                activeTargets.Add(targetRb);
                interactableObj.OnMagicInteract(gameObject, data, shooterID);
            }
        }
    }

    private bool TryHandleChunk(Collider other)
    {
        ChunkNode chunk = other.GetComponentInParent<ChunkNode>();
        if (chunk == null) return false;

        if (chunk.CheckInteractable(gameObject, data, shooterID))
        {
            chunk.OnMagicInteract(gameObject, data, shooterID);
            Rigidbody chunkRb = chunk.GetComponent<Rigidbody>();
            if (chunkRb == null) chunkRb = other.GetComponent<Rigidbody>();

            if (chunkRb != null && !activeTargets.Contains(chunkRb))
            {
                activeTargets.Add(chunkRb);
                return true;
            }
        }
        return false;
    }

    private bool TryValidateTarget(Collider other)
    {
        PhotonView targetPV = other.GetComponentInParent<PhotonView>();
        if (targetPV == null) return true;

        bool isPlayer = other.CompareTag("Player") || targetPV.gameObject.CompareTag("Player");

        if (isPlayer)
        {
            if (targetPV.OwnerActorNr == shooterID) return false;
            if (!PhotonNetwork.CurrentRoom.GetProps<bool>(NetworkProperties.FRIENDLYFIRE)) return false;
        }
        return true;
    }

    private IEnumerator LifeCycleRoutine()
    {
        yield return new WaitForSeconds(data.duration);
        ExplodeAndFinish();
    }

    private void ExplodeAndFinish()
    {
        foreach (var rb in activeTargets)
        {
            if (rb != null)
            {
                var ragdollCtrl = rb.GetComponentInParent<HumanoidRagdollController>();
                if (ragdollCtrl != null) ragdollCtrl.SetTornadoState(false);

                rb.useGravity = true;

                Vector3 explodeDir = (rb.position - transform.position).normalized + Vector3.up * 0.5f;
                rb.AddForce(explodeDir * data.explosionForce, ForceMode.Impulse);

                var damageable = rb.GetComponentInParent<IDamageable>();
                if (damageable != null) damageable.TakeDamage(data.damage);
            }
        }
        activeTargets.Clear();

        StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine()
    {
        yield return new WaitForSeconds(data.destroyDelay);
        if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
    }
}