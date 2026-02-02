using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BossTestStarter : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.CreateRoom("TestRoom");
    }

    public override void OnJoinedRoom()
    {
        DragonAI dragon = FindFirstObjectByType<DragonAI>();
        if (dragon != null)
        {
            dragon.WakeUp();
        }
        else
        {
        }
    }
}