using UnityEngine;

public class CitizenAssassinateState : AIAssassinateState
{
    private CitizenAI citizen;

    public CitizenAssassinateState(BaseAI ai, StateMachine stateMachine, BaseAI.AIStateID stateID)
        : base(ai, stateMachine, stateID)
    {
        if (ai is CitizenAI)
        {
            citizen = ai as CitizenAI;
            interactionData = citizen.assassinateData;
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
