using UnityEngine;

public class PlayerAssassinatedState : PlayerStateBase
{
    public bool IsInteracted { get; private set; }

    public Transform ActorTrans => player.currentTransform.transform;
    [field: SerializeField] public InteractionDataSO interactionData { get; set; }

    public PlayerAssassinatedState(PlayableCharacter player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        IsInteracted = true;
    }

    public override void Exit()
    {
        base.Exit();

        IsInteracted = false;
    }

    public override void Execute() { }

    public override void FixedExecute()
    {
        base.FixedExecute();
    }

    public virtual void OnInteraction()
    {

    }

    public virtual void OnStopped()
    {

    }
}
