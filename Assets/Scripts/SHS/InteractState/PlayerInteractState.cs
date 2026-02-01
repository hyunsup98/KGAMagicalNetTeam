using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteractState : PlayerStateBase, IInteract
{
    public HashSet<IInteract> receivers = new HashSet<IInteract>();     // 상호작용을 할 타겟들

    public bool IsInteracted { get; private set; }  // IInteractable 인터페이스 필드 → 상호작용이 진행 중이면 true

    public InteractionDataSO interactionData { get; private set; }  // 암살 연출 데이터

    public Transform ActorTrans => player.currentTransform;

    public Transform Interactable => player.transform;

    public PlayerInteractState(PlayableCharacter player, StateMachine stateMachine, HashSet<IInteract> receivers, InteractionDataSO interactionData = null) 
        : base(player, stateMachine)
    {
        this.receivers = receivers;
        this.interactionData = interactionData;
    }

    public PlayerInteractState(PlayableCharacter player, StateMachine stateMachine, IInteract receiver, InteractionDataSO interactionData = null)
        : base(player, stateMachine)
    {
        receivers.Add(receiver);
        this.interactionData = interactionData;
    }

    public virtual void SetTarget(IInteract receivers)
    {
        this.receivers.Clear();
        this.receivers.Add(receivers);
    }

    public virtual void SetTarget(HashSet<IInteract> receivers)
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

        if(interactionData != null && player.InteractionManager != null)
        {
            player.InputHandler.CanInteractMotion = false;
        }
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
        player.Rigidbody.isKinematic = true;
        player.SetInvincible(true);
    }

    public virtual void OnStopped()
    {
        // 인풋시스템 o
        player.InputHandler.OnPlayerInput();
        player.StateMachine.ChangeState(player.MoveState);
        player.Rigidbody.isKinematic = false;
        player.SetInvincible(false);
    }
}
