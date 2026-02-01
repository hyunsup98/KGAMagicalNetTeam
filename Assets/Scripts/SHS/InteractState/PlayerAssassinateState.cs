using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAssassinateState : PlayerInteractState
{
    public PlayerAssassinateState(PlayableCharacter player, StateMachine stateMachine, HashSet<IInteract> receivers, InteractionDataSO interactionData = null) 
        : base(player, stateMachine, receivers, interactionData)
    {

    }

    public PlayerAssassinateState(PlayableCharacter player, StateMachine stateMachine, IInteract receiver, InteractionDataSO interactionData = null)
        : base(player, stateMachine, receiver, interactionData)
    {

    }

    public override void Enter()
    {
        base.Enter();

        if (interactionData != null && player.InteractionManager != null)
        {
            Debug.Log("상태 진입");
            player.OnTimelinePlay(InteractionType.Assassinate, this, receivers.ToArray());
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

    public override void OnInteraction()
    {
        base.OnInteraction();

        foreach(var receiver in receivers)
        {

        }
    }

    public override void OnStopped()
    {
        base.OnStopped();
    }
}
