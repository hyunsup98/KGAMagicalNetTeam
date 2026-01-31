using NUnit.Framework.Interfaces;
using Photon.Pun;
using UnityEngine;


[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Collider))]
public class PurchasableItem : MonoBehaviourPun
{
    [Header("Shop Settings")]
    [SerializeField] InventoryDataSO _itemData;
    [SerializeField] int _cost;


    public InventoryDataSO ItemData => _itemData;

    public int Cost => _cost;

    public void RequestDestroy()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
        else
        {
            //PhotonView photonView = PhotonView.Get(this);
            photonView.RPC("RPC_Destroy", RpcTarget.MasterClient);
        }

    }
    [PunRPC]
    public void RPC_Destroy()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
