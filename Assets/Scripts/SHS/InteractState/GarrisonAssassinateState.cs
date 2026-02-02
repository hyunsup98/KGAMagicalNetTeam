using UnityEngine;

public class GarrisonAssassinateState : AIAssassinateState
{
    private GarrisonGuardAI garrison;

    public GarrisonAssassinateState(BaseAI ai, StateMachine stateMachine, BaseAI.AIStateID stateID)
        : base(ai, stateMachine, stateID)
    {
        if (ai is GuardAI)
        {
            garrison = ai as GarrisonGuardAI;
            interactionData = garrison.assassinateData;
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
