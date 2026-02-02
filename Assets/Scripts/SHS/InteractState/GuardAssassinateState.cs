using UnityEngine;

public class GuardAssassinateState : AIAssassinateState
{
    private GuardAI guard;

    public GuardAssassinateState(BaseAI ai, StateMachine stateMachine, BaseAI.AIStateID stateID)
        : base(ai, stateMachine, stateID)
    {
        if(ai is GuardAI)
        {
            guard = ai as GuardAI;
            interactionData = guard.assassinateData;
        }
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Execute() { }

    public override void FixedExecute()
    {
        base.FixedExecute();
    }

    public override void OnInteraction()
    {
        base.OnInteraction();
    }

    public override void OnStopped()
    {
        base.OnStopped();
    }
}
