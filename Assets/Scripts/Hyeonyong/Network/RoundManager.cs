using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;
    public Transform[] spawnPos;
    [SerializeField] AudioClip roundAudio;
    [SerializeField] AudioClip onGameAudio;
    [SerializeField] int requireMoneyCount=5;
    public int RequireMoenyCount => requireMoneyCount;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        SoundManager.Instance.PlayBGM(roundAudio);
    }
    public void StartOnGameBGM()
    {
        SoundManager.Instance.PlayBGM(onGameAudio);
    }
}
