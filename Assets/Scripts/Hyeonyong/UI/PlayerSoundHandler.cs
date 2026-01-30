using Photon.Pun;
using Photon.Voice.Unity.UtilityScripts;
using Photon.Voice.Unity;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.PUN;

public class PlayerSoundHandler : MonoBehaviour
{
    PhotonView pv;
    MicAmplifier mic;
    AudioSource myAudio;
    Recorder recorder;
    static List<AudioSource> otherPlayerAudio = new List<AudioSource>();

    private void Start()
    {

        pv = GetComponent<PhotonView>();
        myAudio = GetComponent<AudioSource>();
        mic = GetComponent<MicAmplifier>();
        recorder = GetComponent<Recorder>();

        if (pv.IsMine)
        {
            //PunVoiceClient.Instance.ConnectAndJoinRoom();
            PunVoiceClient.Instance.PrimaryRecorder = recorder;
            SetSoundEvent();
        }
        else
        {
            recorder.enabled = false;
            mic.enabled = false;
            otherPlayerAudio.Add(myAudio);
        }
    }
    private void OnDisable()
    {
        otherPlayerAudio.Remove(myAudio);
    }

    //private void OnDestroy()
    //{
    //    if (pv.IsMine && PunVoiceClient.Instance != null)
    //    {
    //        if (PunVoiceClient.Instance.Client.InRoom)
    //        {
    //            PunVoiceClient.Instance.Client.OpLeaveRoom(false);
    //        }
    //    }
    //}
    private void SetSoundEvent()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.MicSound.onValueChanged.AddListener((value) =>
                {
                    SetMicSound(value);
                });
            UIManager.Instance.VoiceChatSound.onValueChanged.AddListener((value) =>
            {
                SetVoiceSound(value);
            });
            UIManager.Instance.VoiceChatSoundMute.onValueChanged.AddListener(isOn =>
            {
                SetVoiceMute(isOn);
            });
            UIManager.Instance.MicSoundMute.onValueChanged.AddListener(isOn =>
            {
                SetMicMute(isOn);
            });

            SetMicSound(UIManager.Instance.MicSound.value);
            SetVoiceSound(UIManager.Instance.VoiceChatSound.value);
            SetVoiceMute(UIManager.Instance.VoiceChatSoundMute.isOn);
            SetMicMute(UIManager.Instance.MicSoundMute.isOn);
        }
        else
        {
            SetMicSound(PlayerPrefsDataManager.PlayerMic);
            SetVoiceSound(PlayerPrefsDataManager.PlayerVoice);
            SetVoiceMute(PlayerPrefsDataManager.PlayerVoiceMute);
            SetMicMute(PlayerPrefsDataManager.PlayerMicMute);
        }

    }

    private void SetMicSound(float value)
    {
        mic.AmplificationFactor = value;
    }
    private void SetVoiceSound(float value)
    {
        foreach (AudioSource source in otherPlayerAudio)
        {
            source.volume = value;
        }
    }

    private void SetVoiceMute(bool check)
    {
        foreach (AudioSource source in otherPlayerAudio)
        {
            source.mute = check;
        }
    }
    private void SetMicMute(bool check)
    {
        if (check)
        {
            mic.AmplificationFactor = 0;
        }
        else
        {
            mic.AmplificationFactor = UIManager.Instance.MicSound.value;
        }
    }
}
