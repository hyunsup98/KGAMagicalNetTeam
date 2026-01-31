using Photon.Pun;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineInputAxisController axisController;

    private CinemachineInputAxisController.Controller xAxis;
    private CinemachineInputAxisController.Controller yAxis;

    [Header("추적 대상")]
    [SerializeField] private Transform target; // 추적할 플레이어

    [Header("감도 설정")]
    [SerializeField] private float sensitivity = 1f;
    public float Sensitivity
    {
        get { return  sensitivity; }
        set
        {
            sensitivity = Mathf.Max(value, 1f);
        }
    }

    [SerializeField] private bool invertX = false;
    public bool InvertX
    {
        get { return invertX; }
        set
        {
            invertX = value;
            if (xAxis != null)
            {
                xAxis.Input.Gain = !invertX ? sensitivity : -sensitivity;
            }
        }
    }

    [SerializeField] private bool invertY = false;
    public bool InvertY
    {
        get { return invertY; }
        set
        {
            invertY = value;
            if (yAxis != null)
            {
                yAxis.Input.Gain = !invertY ? -sensitivity : sensitivity;
            }
        }
    }

    public Transform CameraTransform { get; private set; }

    private void Awake()
    {
        if (cinemachineCamera == null)
            TryGetComponent(out cinemachineCamera);

        if (axisController == null)
            TryGetComponent(out axisController);


        if(axisController != null)
        {
            foreach(var control in axisController.Controllers)
            {
                if(control.Name == "Look Orbit X" || control.Name == "LookOrbitX")
                {
                    xAxis = control;
                }
                else if(control.Name == "Look Orbit Y" || control.Name == "LookOrbitY")
                {
                    yAxis = control;
                }
            }
        }

        CameraTransform = this.transform;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(CheckGameManager());
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            UIManager.Instance.onOpenUI -= CheckDisable;
            UIManager.Instance.onCloseUI -= CheckEnable;
        }
    }

    IEnumerator CheckGameManager()
    {
        yield return new WaitUntil(() => FindAnyObjectByType(typeof(GameManager)));
        
        UIManager.Instance.onOpenUI += CheckDisable;
        UIManager.Instance.onCloseUI += CheckEnable;
    }
    private void CheckEnable() => SetControl(true);
    private void CheckDisable() => SetControl(false);

    /// <summary>
    /// 플레이어 시점제어 인풋 활성 및 비활성화 커서 상태도 변화
    /// </summary>
    /// <remarks>
    /// 시점 제어 기능이 활성화되면 커서가 잠기고 숨겨져 카메라 이동이 중단되지 않습니다.
    /// 시점 제어 기능을 비활성화하면 커서가 잠금 해제되고 표시되어 사용자가 UI 요소와 상호 작용할 수 있습니다.
    /// </remarks>
    /// <param name="isActive">true로 설정하면 시점 제어 기능이 활성화되고 커서가 잠기며, false로 설정하면 시점 제어 기능이 비활성화되고 커서가 잠금 해제됩니다.</param>
    public void SetControl(bool isActive)
    {
        if (axisController == null) return;

        if (isActive)
        {
            axisController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            axisController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// 카메라가 볼 타겟 설정
    /// </summary>
    /// <param name="newTarget">새로운 플레이어</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        cinemachineCamera.Target.TrackingTarget = target;
    }

    public Transform ReturnTarget()
    {
        return target;
    }

    public void SetSensivity(float value)
    {
        sensitivity = value;
        xAxis.Input.Gain = !InvertX ? sensitivity : -sensitivity;
        yAxis.Input.Gain = !invertY ? -sensitivity : sensitivity;
    }

    //public void SetInvertX(bool invert)
    //{
    //    InvertX = invert;
    //    xAxis.Input.Gain = !InvertX ? sensitivity : -sensitivity;
    //}
    //public void SetInvertY(bool invert)
    //{
    //    InvertY = invert;
    //    yAxis.Input.Gain = !invertY ? -sensitivity : sensitivity;
    //}
}