using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteractState : PlayerStateBase, IInteractable
{
    public HashSet<IInteractable> receivers = new HashSet<IInteractable>();     // 상호작용을 할 타겟들

    public bool IsInteracted { get; private set; }  // IInteractable 인터페이스 필드 → 상호작용이 진행 중이면 true

    public InteractionDataSO interactionData { get; private set; }  // 암살 연출 데이터

    public Transform ActorTrans => player.currentTransform;

    public PlayerInteractState(PlayableCharacter player, StateMachine stateMachine, InteractionDataSO interactionData = null) 
        : base(player, stateMachine)
    {
        this.receivers = receivers;
    }

    public virtual void SetTarget(IInteractable receivers)
    {
        this.receivers.Clear();
        this.receivers.Add(receivers);
    }

    public virtual void SetTarget(HashSet<IInteractable> receivers)
    {
        this.receivers.Clear();
        this.receivers = receivers;
    }

    public virtual void Init(InteractionDataSO data)
    {
        interactionData = data;
    }

    public override void Enter()
    {
        base.Enter();

        player.InputHandler.CanInteractMotion = false;
        InteractionManager.Instance.RequestInteraction(interactionData, this, receivers.ToArray());
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void Execute()
    {
        base.Execute();
    }

    public override void FixedExecute()
    {
        base.FixedExecute();
    }

    // 상호작용 시 진행할 것들
    public virtual void OnInteraction()
    {
        // 인풋시스템 x
        player.InputHandler.OffPlayerInput();
    }

    public virtual void OnStopped()
    {
        // 인풋시스템 o
        player.InputHandler.OnPlayerInput();
    }
}
