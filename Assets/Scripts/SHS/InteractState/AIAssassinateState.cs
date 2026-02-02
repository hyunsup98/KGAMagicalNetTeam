using UnityEngine;

public class AIAssassinateState : AIStateBase, IInteract
{
    public bool IsInteracted { get; private set; }

    public Transform ActorTrans => ai.transform;
    [field: SerializeField] public InteractionDataSO interactionData { get; set; }

    public Transform Interactable => ai.transform;

    public AIAssassinateState(BaseAI ai, StateMachine stateMachine, BaseAI.AIStateID stateID)
        : base(ai, stateMachine, stateID)
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
        ai.TakeDamage(999f);
    }
}
