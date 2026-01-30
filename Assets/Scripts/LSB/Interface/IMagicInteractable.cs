using UnityEngine;

public interface IMagicInteractable
{
    bool CheckInteractable(GameObject magic, MagicDataSO data, int attackerActorNr);
    void OnMagicInteract(GameObject magic, MagicDataSO data, int attackerActorNr);
}
