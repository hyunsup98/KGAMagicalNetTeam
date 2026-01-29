using System.Collections.Generic;
using UnityEngine;

public class PlayerAssassinateState : PlayerInteractState
{
    public PlayerAssassinateState(PlayableCharacter player, StateMachine stateMachine, InteractionDataSO interactionData = null, HashSet<IInteractable> receivers = null) : base(player, stateMachine, interactionData, receivers)
    {

    }

    public override void Enter()
    {
        base.Enter();
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

    public override void OnInteraction()
    {
        base.OnInteraction();
    }

    public override void OnStopped()
    {
        base.OnStopped();
    }
}
