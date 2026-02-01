using UnityEngine;

[CreateAssetMenu(fileName = "New BlackHole", menuName = "Game/BlackHole")]
public class BlackHoleSO : MagicDataSO
{
    [Header("BlackHole Settings")]
    public float duration = 5.0f;
    public float pullForce = 20f;
    public float rotationForce = 5f;
    public float explosionForce = 50f;
    public float destroyDelay = 0.5f;
    public LayerMask hitLayer;

    public override ActionBase CreateInstance()
    {
        return new MagicBlackHole(this);
    }
}