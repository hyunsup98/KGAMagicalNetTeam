using UnityEngine;

[CreateAssetMenu(fileName = "New Purchasable Item", menuName = "Game/PurchsableSO")]
public class PurchasableItemSO:ItemDataSO
{
    public override ActionBase CreateInstance()
    {
        return new ItemCoin(this);
    }
}




