using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayableCharacter : MonoBehaviourPun, IInteractable
{
    // 구독할 이벤트 정의
    public event Action<float> OnHpChanged; // 체력 변경하면 전달용
    public event Action OnDie;       // 사망하면 호출용

    // 모델
    private PlayerModel _model;
    public PlayerModel Model=>_model;

    [Header("Settings")]
    [SerializeField] private float maxHp = 100f; // 체력 인스펙터 노출
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float dodgeForce = 7f;
    [SerializeField] private float DodgeCooldown = 1.5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDist = 0.1f;

    [Header("Transformation")]
    [SerializeField] private GameObject civilianModel;
    [SerializeField] private GameObject wizardModel;

    [Header("MapIcon")]
    [SerializeField] private MapIcon playerIcon;
    [SerializeField] private MapIcon teamIcon;

    [Header("Interact Settings")]
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private LayerMask canInteractLayer;    // 상호작용이 가능한 레이어
    [SerializeField] private float checkDistance = 1f;      // 260122 신현섭: 상호작용 체크 거리

    [Header("Camera Setting")]
    [SerializeField] public int cameraIndex = -1;
    [SerializeField] private int curCameraIndex = -1;
    public int CurCameraIndex => curCameraIndex;
    [SerializeField] private Dictionary<int,int> checkPlayerActorNumBycameraIndex= new Dictionary<int,int>();// key : cameraIndex value : playerNum
    public Dictionary<int, int> CheckPlayerActorNumByCameraIndex => checkPlayerActorNumBycameraIndex;

    public enum MoveDir { Front, Back, Left, Right }

    #region 프로퍼티
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float JumpForce => jumpForce;
    public float DodgeForce => dodgeForce;
    public float LastDodgeTime { get; set; } = 0f;
    public bool CanDodge => Time.time >= LastDodgeTime + DodgeCooldown;
    public GameObject CivilianModel => civilianModel;
    public GameObject WizardModel => wizardModel;
    public InteractionManager InteractionManager => interactionManager;
    #endregion

    #region 참조
    public PlayerInputHandler InputHandler { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public Animator Animator { get; private set; }
    public ThirdPersonCamera GameCamera { get; private set; }
    public PlayerInventory Inventory { get; private set; }
    public PlayerMagicSystem MagicSystem { get; private set; }
    public PlayerController playerController { get; private set; }
    public PlayerTransformationController TransformationController { get; private set; }
    public List<Transform> otherPlayerTransform = new List<Transform>();
    #endregion

    #region 상태 머신
    public StateMachine StateMachine { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerDodgeState DodgeState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerInteractState InteractState { get; private set; }    // 260122 신현섭: 상호작용 상태로 전환


    [field: SerializeField] public InteractionDataSO assassinatedData { get; private set; }  // 암살 연출 데이터 (당하는 입장)

    [field: SerializeField] public Transform currentTransform { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    public void SetInvincible(bool isInvincible) => IsInvincible = isInvincible;
    public void SetCurrentTransform(Transform trans) => currentTransform = trans;


    #endregion

    #region 애니메이션
    public readonly int HashSpeed = Animator.StringToHash("Speed");
    public readonly int HashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    public readonly int HashAttackTrigger = Animator.StringToHash("AttackTrigger");
    public readonly int HashAttackID = Animator.StringToHash("AttackID");
    public readonly int HashJumpType = Animator.StringToHash("JumpType");
    public readonly int HashDodgeType = Animator.StringToHash("DodgeType");
    public readonly int HashTransform = Animator.StringToHash("Transform");

    public void UpdateMoveAnimation(float currentSpeed)
    {
        Animator.SetFloat(HashSpeed, currentSpeed, 0.1f, Time.deltaTime);
    }
    #endregion

    private void Awake()
    {
        Debug.Log("플레이어가 생성되었다");
        InputHandler = GetComponent<PlayerInputHandler>();
        Rigidbody = GetComponent<Rigidbody>();
        MagicSystem = GetComponent<PlayerMagicSystem>();
        playerController = GetComponent<PlayerController>();
        TransformationController = GetComponent<PlayerTransformationController>();

        // 모델 초기화
        _model = new PlayerModel(maxHp);
        _model.Init();

        Inventory = new PlayerInventory();

        StateMachine = new StateMachine();
        MoveState = new PlayerMoveState(this, StateMachine);
        JumpState = new PlayerJumpState(this, StateMachine, "IsJumping");
        DodgeState = new PlayerDodgeState(this, StateMachine, "IsDodging");
        AttackState = new PlayerAttackState(this, StateMachine);



        //260119 최정욱 local player 인스턴스 저장
        if (photonView.IsMine)
        {
            GameManager.Instance.LocalPlayer = gameObject;
            PhotonNetwork.LocalPlayer.SetProps(NetworkProperties.PLAYER_ALIVE, true);
        }

        // 260121 신현섭 : 미니맵 연동 및 아이콘 지정
        if (photonView.IsMine)
        {
            // 미니맵 카메라에 플레이어 트랜스폼 할당
            MinimapCamera camera = FindAnyObjectByType<MinimapCamera>();

            if (camera != null)
            {
                camera.SetTarget(transform);
            }

            // 플레이어 자신일 때 플레이어 아이콘을 활성화
            playerIcon.gameObject.SetActive(true);
        }
        else
        {
            // 다른 팀원일 때 팀원 아이콘 활성화
            teamIcon.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            var camScript = FindAnyObjectByType<ThirdPersonCamera>();
            if (camScript != null)
            {
                GameCamera = camScript;
                camScript.SetTarget(this.transform);
            }

            // 인풋 이벤트 구독
            SubscribeInputEvents();

            StateMachine.InitState(MoveState);
        }
    }

    private void OnDisable()
    {
        if (photonView.IsMine)
        {
            //260128 최정욱 카메라 초기화
            GameCamera = null;
            // 메모리 누수 방지용 구독 해제
            UnsubscribeInputEvents();
            if(GameManager.Instance != null)
                OnDie -= GameManager.Instance.CheckDie;
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (Inventory != null)
        {
            Inventory.HandleCooldowns(Time.deltaTime);
        }

        CanInteractMotion();

        StateMachine.CurrentState.Execute();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        StateMachine.CurrentState.FixedExecute();
    }

    private void SubscribeInputEvents()
    {
        if (InputHandler == null) return;
        InputHandler.OnJumpEvent += HandleJump;
        InputHandler.OnAttackEvent += HandleAttack;
        InputHandler.OnTransformEvent += HandleTransformation;
        InputHandler.OnInteractMotionEvent += HandleInteract;
    }

    private void UnsubscribeInputEvents()
    {
        if (InputHandler == null) return;
        InputHandler.OnJumpEvent -= HandleJump;
        InputHandler.OnAttackEvent -= HandleAttack;
        InputHandler.OnTransformEvent -= HandleTransformation;
        InputHandler.OnInteractMotionEvent -= HandleInteract;
        InputHandler.DisconnectCameraChange();
    }

    // 점프/회피 이벤트
    private void HandleJump()
    {
        // 이동 상태일때만 점프/회피 가능
        if (StateMachine.CurrentState is PlayerMoveState)
        {
            Vector2 input = InputHandler.MoveInput;
            MoveDir dir = GetMoveDir(input);

            // 좌우 이동 중이면 회피
            if (dir == MoveDir.Left || dir == MoveDir.Right)
            {
                if (CanDodge)
                {
                    LastDodgeTime = Time.time;
                    StateMachine.ChangeState(DodgeState);
                }
            }
            else
            {
                StateMachine.ChangeState(JumpState);
            }
        }
    }

    private void HandleAttack(bool isLeftHand)
    {
        // 이동 상태일때만 공격 가능
        if (!(StateMachine.CurrentState is PlayerMoveState)) return;

        ActionBase magic = MagicSystem.GetAction(isLeftHand);

        // 마법 쿨타임중인지 확인
        if (MagicSystem.IsActionReady(isLeftHand))
        {
            // 공격 상태로 전환
            var attackState = AttackState as PlayerAttackState;
            if (attackState != null)
            {
                attackState.Init(isLeftHand, magic);
                StateMachine.ChangeState(AttackState);
            }
        }
        else
        {
            Debug.Log("쿨타임 중이라 공격 불가");
        }
    }

    private void HandleTransformation(bool isPressed)
    {
        TransformationController.HandleTransformInput(isPressed);
    }

    // 20260127 신현섭: 상호작용 이벤트에 체인시킬 메서드
    private void HandleInteract()
    {
        StateMachine.ChangeState(InteractState);
    }

    // 260122 신현섭: 상호작용이 가능한 상태인지 체크 (레이캐스트)
    private void CanInteractMotion()
    {
        if (!(StateMachine.CurrentState is PlayerMoveState)) return;

        if(Physics.Raycast(transform.position + new Vector3(0, 1, 0), Camera.main.transform.forward, out RaycastHit hit, checkDistance, canInteractLayer))
        {
            if(hit.collider.TryGetComponent<IInteractable>(out var interact))
            {
                IInteract interactInfo = null;

                // 시민, 경비원
                if ((hit.collider.TryGetComponent<BaseAI>(out var ai) || hit.collider.TryGetComponent<PlayableCharacter>(out var p))
                    && transform.IsTargetInDirection(ai.transform, DirectionType.Backward, 110f)
                    && TransformationController.IsWizard)
                {
                    interactInfo = interact.GetInteractInfo(InteractionType.Assassinate);

                    if (interactInfo == null) return;

                    // todo: 상호작용 가능 UI 띄우기 등 실행
                    InputHandler.CanInteractMotion = true;

                    if (InteractState == null || !InteractState.receivers.Contains(interact.GetInteractInfo(InteractionType.Assassinate)))
                    {
                        InteractState = new PlayerAssassinateState(this, StateMachine, interactInfo, interactInfo.interactionData);
                    }
                }
            }
            else
            {
                InputHandler.CanInteractMotion = false;
                InteractState = null;
            }
        }
    }

    public void OnTimelinePlay(InteractionType type, IInteract executer, params IInteract[] receivers)
    {
        if (photonView.IsMine == false) return;

        if (!executer.Interactable.TryGetComponent<PhotonView>(out var executerID)) return;

        int[] receiversID = new int[receivers.Length];

        for(int i = 0; i < receiversID.Length; i++)
        {
            if (!receivers[i].Interactable.TryGetComponent<PhotonView>(out var receiverID)) return;

            receiversID[i] = receiverID.ViewID;
        }


        photonView.RPC(nameof(RPC_TimelinePlay), RpcTarget.All, (int)type, executerID.ViewID, receiversID);
    }

    [PunRPC]
    public void RPC_TimelinePlay(int type, int executerID, params int[] receiversID)
    {
        IInteract[] receivers = new IInteract[receiversID.Length];
        IInteract executer = null;

        for(int i = 0; i < receiversID.Length; i++)
        {
            IInteractable interactable = PhotonView.Find(receiversID[i]).GetComponent<IInteractable>();
            receivers[i] = interactable.GetInteractInfo((InteractionType)type);
        }

        switch ((InteractionType)type)
        {
            case InteractionType.Assassinate:
                executer = new PlayerAssassinateState(this, StateMachine, receivers[0], receivers[0].interactionData);
                break;
        }

        Debug.Log(executer.ActorTrans.name);
        foreach(var receiver in receivers)
        {
            Debug.Log(receiver.ActorTrans.name);
        }

        InteractionManager.RequestInteraction(photonView.IsMine, executer, receivers);
    }

    public void OnAttacked(float damage)
    {
        if (photonView.IsMine == true && !PhotonNetwork.LocalPlayer.GetProps<bool>(NetworkProperties.PLAYER_ALIVE))
            return;
        if (IsInvincible) return;   // 무적 판정일 경우 무시

        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        bool isDie = _model.TakeDamage(damage);
        OnHpChanged?.Invoke(_model.CurHp / _model.MaxHp);

        if (isDie)
        {
            //사망 액션 혹은 그냥 사망 애니메이션 실행
            Animator.Play("Die");
            if (photonView.IsMine)
            {
                CheckCameraOnDie();
                PhotonNetwork.LocalPlayer.SetProps(NetworkProperties.PLAYER_ALIVE, false);
                ChangeCameraOnDie();
            }
            OnDie?.Invoke();
            Debug.Log("캐릭터 사망");
        }
    }

    public MoveDir GetMoveDir(Vector2 input)
    {
        if (input.magnitude < 0.1f) return MoveDir.Front;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x > 0 ? MoveDir.Right : MoveDir.Left;
        }
        else
        {
            return input.y > 0 ? MoveDir.Front : MoveDir.Back;
        }
    }

    public bool CheckIsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDist + 0.1f, groundLayer);
    }

    public void SetAnimator(Animator newAnimator)
    {
        this.Animator = newAnimator;
    }

    public void ChangePlayerLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    public void RemoveLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public void CheckCameraOnDie()
    {
        if (!photonView.IsMine)
            return;
        //1. 조작권 박탈 <- 인풋핸들러 쪽으로 이양
        
        //2. 카메라 타겟 다른 플레이어로 전환
        if (otherPlayerTransform.Count <= 0)
        {
            PlayableCharacter[] otherPlayer = FindObjectsByType<PlayableCharacter>(FindObjectsSortMode.None);

            foreach (PlayableCharacter p in otherPlayer)
            {
                PhotonView pv = p.GetComponent<PhotonView>();
                if (!pv.IsMine)
                {
                    otherPlayerTransform.Add(p.transform);
                    CheckPlayerActorNumByCameraIndex.Add(otherPlayerTransform.Count - 1, pv.OwnerActorNr);
                }
            }
        }
        //3. 특정 버튼 클릭 시 다른 플레이어 확인 가능 여기서 액션 버튼 +=으로 넣고 파괴시 빼자
        if (otherPlayerTransform.Count > 0)
        {
            ChangeCameraTarget();
            InputHandler.ConnectCameraChange();
        }
    }

    public void ChangeCameraTarget()
    {
        cameraIndex++;
        if (cameraIndex >= otherPlayerTransform.Count)
            cameraIndex = 0;

        int checkCount = 0;

        while (true)
        {      
            Transform t = otherPlayerTransform[cameraIndex];
            if (t != null)
            {
                PhotonView pv = t.GetComponent<PhotonView>();
                if (pv.Owner.GetProps<bool>(NetworkProperties.PLAYER_ALIVE))
                {
                    break;
                }
            }
            cameraIndex++;
            if (cameraIndex >= otherPlayerTransform.Count)
                cameraIndex = 0;
            checkCount++;
            if (checkCount >= otherPlayerTransform.Count)
            {
                Debug.Log("생존자 없음");
                break;
            }
        }
        if (otherPlayerTransform[cameraIndex] != null) 
        {
            if (curCameraIndex == cameraIndex)
                return;
            GameCamera.SetTarget(otherPlayerTransform[cameraIndex]);
            curCameraIndex = cameraIndex;
        }

    }
    public void ChangeCameraTargetOnPlayerInput(InputAction.CallbackContext ctx)
    {
        ChangeCameraTarget();
    }

    public void ChangeCameraOnDie()
    {
        if (PhotonNetwork.LocalPlayer.GetProps<bool>(NetworkProperties.PLAYER_ALIVE))
            return;
        ThirdPersonCamera cam = GameObject.FindAnyObjectByType<ThirdPersonCamera>();


        if (cam != null && cam.ReturnTarget() == this.transform)
        {
            GameManager.Instance.LocalPlayer.GetComponent<PlayableCharacter>().ChangeCameraTarget();
        }
    }

    public IInteract GetInteractInfo(InteractionType type)
    {
        switch(type)
        {
            case InteractionType.Assassinate:

            default:
                return null;
        }
    }
}



#region 레거시 코드
//using Photon.Pun;
//using System;
//using UnityEngine;

//public class PlayableCharacter : MonoBehaviourPun
//{
//    // 구독할 이벤트 정의
//    public event Action<float> OnHpChanged; // 체력 변경하면 전달용
//    public event Action OnDie;              // 사망하면 호출용

//    // 모델
//    private PlayerModel _model;

//    [Header("Settings")]
//    [SerializeField] private float maxHp = 100f; // 체력 인스펙터 노출
//    [SerializeField] private float moveSpeed = 5f;
//    [SerializeField] private float rotationSpeed = 10f;
//    [SerializeField] private float jumpForce = 5f;
//    [SerializeField] private float dodgeForce = 7f;
//    [SerializeField] private float DodgeCooldown = 1.5f;


//    [Header("Ground Detection")]
//    [SerializeField] private LayerMask groundLayer;
//    [SerializeField] private float groundCheckDist = 0.1f;

//    public enum MoveDir { Front, Back, Left, Right }

//    #region 프로퍼티
//    public float MoveSpeed => moveSpeed;
//    public float RotationSpeed => rotationSpeed;
//    public float JumpForce => jumpForce;
//    public float DodgeForce => dodgeForce;
//    public float LastDodgeTime { get; set; } = 0f;
//    public bool CanDodge => Time.time >= LastDodgeTime + DodgeCooldown;
//    #endregion

//    #region 참조
//    public PlayerInputHandler InputHandler { get; private set; }
//    public Rigidbody Rigidbody { get; private set; }
//    public Animator Animator { get; private set; }
//    public ThirdPersonCamera GameCamera { get; private set; }
//    public PlayerInventory Inventory { get; private set; }
//    public PlayerMagicSystem MagicSystem { get; private set; }
//    public PlayerController playerController { get; private set; }
//    public PlayerTransformationController TransformationController { get; private set; }
//    #endregion

//    #region 상태 머신
//    public StateMachine StateMachine { get; private set; }
//    public PlayerMoveState MoveState { get; private set; }
//    public PlayerJumpState JumpState { get; private set; }
//    public PlayerDodgeState DodgeState { get; private set; }
//    public PlayerAttackState AttackState { get; private set; }
//    #endregion



//    private void Awake()
//    {
//        InputHandler = GetComponent<PlayerInputHandler>();
//        Rigidbody = GetComponent<Rigidbody>();
//        MagicSystem = GetComponent<PlayerMagicSystem>();
//        playerController = GetComponent<PlayerController>();
//        TransformationController = GetComponent<PlayerTransformationController>();

//        // 모델 초기화
//        _model = new PlayerModel(maxHp);
//        _model.Init();

//        Inventory = new PlayerInventory();

//        StateMachine = new StateMachine();
//        MoveState = new PlayerMoveState(this, StateMachine);
//        JumpState = new PlayerJumpState(this, StateMachine, "IsJumping");
//        DodgeState = new PlayerDodgeState(this, StateMachine, "IsDodging");
//        AttackState = new PlayerAttackState(this, StateMachine, "IsAttacking");
//    }

//    private void Start()
//    {
//        if (photonView.IsMine)
//        {
//            var camScript = FindAnyObjectByType<ThirdPersonCamera>();
//            if (camScript != null)
//            {
//                GameCamera = camScript;
//                camScript.SetTarget(this.transform);
//            }

//            StateMachine.InitState(MoveState);
//        }
//    }

//    private void Update()
//    {
//        if (!photonView.IsMine) return;

//        if (Inventory != null)
//        {
//            Inventory.HandleCooldowns(Time.deltaTime);
//        }

//        StateMachine.CurrentState.Execute();
//    }

//    private void FixedUpdate()
//    {
//        if (!photonView.IsMine) return;
//        StateMachine.CurrentState.FixedExecute();
//    }


//    // 데미지 처리 로직 여기로 옮김
//    public void OnAttacked(float damage)
//    {
//        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage);
//    }

//    // 데미지 적용 이벤트 호출
//    [PunRPC]
//    public void RPC_TakeDamage(float damage)
//    {
//        bool isDie = _model.TakeDamage(damage);

//        // 데이터 변경 사실을 Presenter에게 알림
//        OnHpChanged?.Invoke(_model.CurHp / _model.MaxHp);

//        if (isDie)
//        {
//            OnDie?.Invoke();
//            Debug.Log("캐릭터 사망 (Logic)");
//        }
//    }


//    /// <summary>
//    /// 움직이는 방향 구하는용도
//    /// </summary>
//    /// <param name="input"></param>
//    /// <returns></returns>
//    public MoveDir GetMoveDir(Vector2 input)
//    {
//        if (input.magnitude < 0.1f) return MoveDir.Front;

//        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
//        {
//            return input.x > 0 ? MoveDir.Right : MoveDir.Left;
//        }
//        else
//        {
//            return input.y > 0 ? MoveDir.Front : MoveDir.Back;
//        }
//    }

//    /// <summary>
//    /// 그라운드 체크
//    /// </summary>
//    /// <returns>땅인지</returns>
//    public bool CheckIsGrounded()
//    {
//        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDist + 0.1f, groundLayer);
//    }

//    public void SetAnimator(Animator newAnimator)
//    {
//        this.Animator = newAnimator;
//    }

//    public void ChangePlayerLayer()
//    {
//        gameObject.layer = LayerMask.NameToLayer("Player");
//    }

//    public void RemoveLayer()
//    {
//        gameObject.layer = LayerMask.NameToLayer("Default");
//    }
//}
#endregion