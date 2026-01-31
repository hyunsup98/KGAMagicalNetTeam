using UnityEngine;

public class AssassinateInteract : BaseInteractSystem
{
    public AssassinateInteract(InteractionDataSO data, IInteract executer, params IInteract[] receivers) : base(data,executer, receivers)
    {

    }

    public override void Init(InteractionDataSO data, IInteract executer, params IInteract[] receivers)
    {
        base.Init(data, executer, receivers);
    }

    public override void PlayInteract()
    {
        base.PlayInteract();
    }
}
