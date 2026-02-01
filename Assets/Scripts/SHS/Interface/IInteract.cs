using Photon.Pun;
using UnityEngine;

public interface IInteract
{
    public bool IsInteracted { get; }
    public InteractionDataSO interactionData { get; }
    public Transform ActorTrans { get; }        // 상호작용을 담당할 트랜스폼
    public Transform Interactable { get; }      // IInteractable 인터페이스를 상속받고 있는 트랜스폼
    public void OnInteraction();    // 상호작용 호출자가 실행할 메서드
    public void OnStopped();        // 상호작용이 끝날 때 실행할 메서드
}

public interface IInteractable
{
    public IInteract GetInteractInfo(InteractionType type);
}
