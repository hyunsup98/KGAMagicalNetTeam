using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//데스윙
//2.3 이륙 돌진 착륙 순서로 변경
public class DragonFlightState : BossStateBase
{
    private Vector3 targetPos;      //돌진 지
    private float flightSpeed = 20.0f; //돌진 속도
    private float maxFlightDuration = 4.0f; //시간
    private float flightTimer = 0f;

    private bool isTakingOff = false;       //이륙 중
    private float takeOffDuration = 2.0f;   //시간
    private float takeOffTimer = 0f;
    private float riseHeight = 3f;        //높이
    private Vector3 startGroundPos;         //이륙 시작점


    //장판
    private float fireDropTimer = 0f; 
    private float fireDropInterval = 0.2f;
    private string fireZonePrefabName = "EffectPrefab/DragonFireZone";

    //몸박
    private float chargeDamage = 19f;
    private HashSet<int> hitPlayerIDs = new HashSet<int>();

    public DragonFlightState(DragonAI dragon, StateMachine stateMachine) 
        : base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = true;
            dragon.agent.updatePosition = false;
            dragon.agent.updateRotation = false;
            dragon.agent.enabled = false;
        }

        //이륙 초기화
        isTakingOff = true;
        takeOffTimer = 0f;
        startGroundPos = dragon.transform.position; //현재 바닥 위치 기준점 잡기
        dragon.PlayAnimCrossFade("Fly Idle", 0.1f);

        dragon.DisableWeaponHitbox();
        hitPlayerIDs.Clear();
        flightTimer = 0f;
        fireDropTimer = 0f;
    }

    public override void Execute()
    {

        if (isTakingOff)
        {
            HandleTakeOff();
        }
        else
        {
            HandleFlight();
        }
    }

    //수직상승
    private void HandleTakeOff()
    {
        takeOffTimer += Time.deltaTime;

        float progress = takeOffTimer / takeOffDuration;
        Vector3 airPos = startGroundPos + Vector3.up * riseHeight;
        dragon.transform.position = Vector3.Lerp(startGroundPos, airPos, progress);

        //플레이어 바라보기
        if (dragon.targetPlayer != null)
        {
            Vector3 lookDir = dragon.targetPlayer.position - dragon.transform.position;
            lookDir.y = 0; 
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                dragon.transform.rotation = Quaternion.Slerp(dragon.transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }

        //이륙 시간 종료 후 돌진 목표 설정
        if (takeOffTimer >= takeOffDuration)
        {
            isTakingOff = false;
            CalculateChargeTarget(); 
        }
    }

    //돌진 목표 계산
    private void CalculateChargeTarget()
    {
        flightTimer = 0f;
        fireDropTimer = 0f;

        Vector3 dir = dragon.transform.forward;

        targetPos = dragon.transform.position + (dir * dragon.flightDistance);

        //현재 떠있는 높이 유지
        targetPos.y = dragon.transform.position.y;
    }

    private void HandleFlight()
    {
        flightTimer += Time.deltaTime;

        if (flightTimer >= maxFlightDuration)
        {
            StartLanding();
            return;
        }

        // 이동
        float step = flightSpeed * Time.deltaTime;
        Vector3 prevPos = dragon.transform.position;
        dragon.transform.position = Vector3.MoveTowards(dragon.transform.position, targetPos, step);

        dragon.transform.LookAt(targetPos);

        //충돌 체크
        CheckSweptCollision(prevPos, dragon.transform.position);
        //확인해봐야함
        SpawnFireZone();

        //도달 체크
        if (Vector3.Distance(dragon.transform.position, targetPos) < 1.0f)
        {
            StartLanding();
        }
    }


    //몸박체크
    private void CheckSweptCollision(Vector3 startPos, Vector3 endPos)
    {
        Vector3 dir = endPos - startPos;
        float dist = dir.magnitude;

        if (dist < 0.01f) return;

        //경로상 충돌체 검출
        RaycastHit[] hits = Physics.SphereCastAll(
            startPos,
            dragon.bodyCrashRadius,
            dir.normalized,
            dist,
            LayerMask.GetMask("Player")
        );

        foreach (var hit in hits)
        {
            GameObject targetObj = hit.collider.gameObject;
            int targetID = targetObj.GetInstanceID();

            //중복 데미지 방지
            if (hitPlayerIDs.Contains(targetID)) continue;

            IDamageable damageable = targetObj.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(chargeDamage);
                hitPlayerIDs.Add(targetID); //피격 목록에 추가
                Debug.Log($"로드킬 성공: {targetObj.name}");
            }
        }
    }

    //장판 생성
    private void SpawnFireZone()
    {
        fireDropTimer += Time.deltaTime;
        if (fireDropTimer >= fireDropInterval)
        {
            fireDropTimer = 0f;

            //아래로 레이 바닥 위치 확인
            if (Physics.Raycast(dragon.transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, NavMesh.AllAreas))
            {
                //마스터 클라이언트만
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Instantiate(fireZonePrefabName, hit.point + Vector3.up * 0.1f, Quaternion.identity);
                }
            }
        }
    }
    //착륙
    private void StartLanding()
    {
        if (NavMesh.SamplePosition(dragon.transform.position, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
        {
            dragon.transform.position = hit.position;
        }
        else
        {
            if (Physics.Raycast(dragon.transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground", "Default")))
            {
                dragon.transform.position = groundHit.point;
            }
        }

        //전투 복귀
        stateMachine.ChangeState(new DragonCombatState(dragon, stateMachine));
    }

    public override void Exit()
    {
        if (dragon.agent != null)
        {
            dragon.agent.enabled = true;        
            dragon.agent.Warp(dragon.transform.position); 
            dragon.agent.updatePosition = true; 
            dragon.agent.updateRotation = true;
            dragon.agent.isStopped = false;
        }

        hitPlayerIDs.Clear();
    }
}
