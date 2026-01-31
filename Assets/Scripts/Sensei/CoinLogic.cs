using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class CoinLogic : MonoBehaviour
{
    [SerializeField] float _pickupRange = 10f;
    [SerializeField] AudioClip _purchaseSound;

    private Vector3 _debugRayStart;
    private Vector3 _debugRayEnd;


    void Start()
    {
     
        //InputSystem.actions.FindActionMap("Player").FindAction("Interact").performed += PickupCoin;

        if (GetComponent<PlayerInputHandler>() != null)
        {
            Debug.Log(GetComponent<PlayerInputHandler>());
            GetComponent<PlayerInputHandler>().OnInteractEvent += PickupOrSpendCoin;
        }

    }

    void OnDestroy()
    {
        if (GetComponent<PlayerInputHandler>() != null)
        {
            GetComponent<PlayerInputHandler>().OnInteractEvent -= PickupOrSpendCoin;
        }
        // InputSystem.actions.FindActionMap("Player").FindAction("Interact").performed -= PickupCoin;
    }

    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(_debugRayStart, _debugRayEnd);
        Gizmos.DrawSphere(_debugRayEnd, 0.1f);
    }



    void PickupOrSpendCoin()
    {
        if (GetComponent<PhotonView>().IsMine)
        {
            _debugRayStart = Camera.main.transform.position;
            _debugRayEnd = _debugRayStart + Camera.main.transform.forward * _pickupRange;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit[] hits = Physics.RaycastAll(ray, _pickupRange);

            int index = 0;
            foreach (var hit in hits)
            {
                index++;
                if (index > 3)
                {
                    Debug.Log("Too many hits, breaking out of loop.");
                    break;
                }

                //Debug.Log("Raycast Hit All: " + hit.transform.name);



                if (hit.transform.gameObject.GetComponent<CoinItself>() != null)
                {

                    var coin = hit.transform.gameObject.GetComponent<CoinItself>();

                    GameManager.Instance.LocalPlayer.GetComponent<PlayableCharacter>().Inventory.AddItem(coin.CoinData);
                    //Debug.Log("Coin Picked Up! Really");

                    coin.RequestDestroy();
                }
                else if (hit.transform.gameObject.GetComponent<PurchasableItem>() != null)
                {

                    PurchasableItem shopItem = hit.transform.gameObject.GetComponent<PurchasableItem>();

                    if (GameManager.Instance.CurTeamMoney() >= shopItem.Cost)
                    {
                        SoundManager.Instance.PlaySFX(_purchaseSound, 1f, 100f, transform.position);
                        #region Legacy Code
                        //Hashtable customProperties = new Hashtable();
                        //customProperties["MoneyCount"] = (int)PhotonNetwork.CurrentRoom.CustomProperties["MoneyCount"] - (hit.transform.gameObject.GetComponent<PurchasableScriptableObject>().Cost);
                        //PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties); // = (int)PhotonNetwork.CurrentRoom.CustomProperties["MoneyCount"] - (hit.transform.gameObject.GetComponent<PurchasableScriptableObject>().Cost);
                        #endregion
                        GameManager.Instance.UseTeamMoney(shopItem.Cost);

                        GameManager.Instance.LocalPlayer.GetComponent<PlayableCharacter>().Inventory.AddItem(shopItem.ItemData);

                        shopItem.RequestDestroy();

                        Debug.Log($"구매 성공: {shopItem.ItemData.itemName} (-{shopItem.Cost} Gold)");
                    }
                    else
                    {
                        Debug.Log($"돈이 부족합니다. (필요: {shopItem.Cost}, 보유: {GameManager.Instance.CurTeamMoney()})");
                    }
                    #region Legacy Code
                    //PurchasableScriptableObject purchasable = hit.transform.gameObject.GetComponent<PurchasableScriptableObject>();
                    //InventoryDataSO itemData = purchasable.InventoryData;
                    //if (GameManager.Instance.LocalPlayer.GetComponent<PlayableCharacter>().Inventory.GetItemCount(itemData) > 0)
                    //{
                    //    // Spend the coin
                    //    GameManager.Instance.LocalPlayer.GetComponent<PlayableCharacter>().Inventory.RemoveItem(itemData, 1);
                    //    Debug.Log("Purchased item: " + itemData.name);
                    //    // Here you can add logic to grant the purchased item to the player
                    //}
                    //else
                    //{
                    //    Debug.Log("Not enough coins to purchase: " + itemData.name);
                    //}
                    #endregion
                }
            }
        }
        #region Legacy Code
        //EventSystem.Screen
        //Debug.Log("Interaction F Clicked");

        // Add coin to player's inventory or increase coin count
        // Example: playerInventory.AddCoins(1);
        //Destroy(gameObject); // Remove the coin from the scene
        #endregion
    }
}
