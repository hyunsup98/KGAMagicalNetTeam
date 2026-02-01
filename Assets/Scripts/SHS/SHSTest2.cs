using UnityEngine;

public class SHSTest2 : MonoBehaviour, IInteract
{
    public bool IsInteracted { get; private set; }

    public Transform ActorTrans => transform;

    [field: SerializeField] public InteractionDataSO interactionData { get; set; }

    public Transform Interactable => throw new System.NotImplementedException();

    public void OnInteraction()
    {

    }

    public void OnStopped()
    {

    }
}
