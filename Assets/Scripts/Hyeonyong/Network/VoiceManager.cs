using Photon.Pun;
using UnityEngine;

public class VoiceManager : Singleton<VoiceManager>
{
    PhotonView pv;
    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            pv = gameObject.AddComponent<PhotonView>();
            pv.ViewID = 800;
        }
    }
}
