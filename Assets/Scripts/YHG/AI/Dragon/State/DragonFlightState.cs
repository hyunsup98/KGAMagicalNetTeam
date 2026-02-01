using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//데스윙
public class DragonFlightState : BossStateBase
{
    private Vector3 targetPos;      //돌진 지점

    private float flightSpeed = 10.0f; //돌진 속도

    private float maxFlightDuration = 4.0f; //시간
    private float flightTimer = 0f;

    //장판
    private float fireDropTimer = 0f; 
    private float fireDropInterval = 0.2f;
    private string fireZonePrefabName = "EffectPrefab/DragonFireZone";

    //몸박
    private float chargeDamage = 300f;
    private HashSet<int> hitPlayerIDs = new HashSet<int>();

    public DragonFlightState(DragonAI dragon, StateMachine stateMachine) 
        : base(dragon, stateMachine) { }

    public override void Enter()
    {
        if (dragon.agent != null)
        {
            dragon.agent.isStopped = true;
            dragon.agent.enabled = false;
        }

        //목표 설정
        Vector3 dir = dragon.transform.forward;
        if (dragon.targetPlayer != null)
        {
            dir = (dragon.targetPlayer.position - dragon.transform.position).normalized;
        }

        targetPos = dragon.transform.position + (dir * dragon.flightDistance);
        targetPos.y = dragon.transform.position.y;

        dragon.PlayAnimTrigger("Fly Glide");
        dragon.DisableWeaponHitbox();

        //피격 리스트 초기화
        hitPlayerIDs.Clear();
        flightTimer = 0f;
        fireDropTimer = 0f;
    }

    public override void Execute()
    {
        //시간체크
        flightTimer += Time.deltaTime;
        if (flightTimer >= maxFlightDuration)
        {
            StartLanding();
            return;
        }

        //이동
        Vector3 prevPos = dragon.transform.position;
        float step = flightSpeed * Time.deltaTime;
        dragon.transform.position = Vector3.MoveTowards(dragon.transform.position, targetPos, step);
        dragon.transform.LookAt(targetPos);

        //충돌
        CheckSweptCollision(prevPos, dragon.transform.position);

        //장판 생성
        SpawnFireZone();

        //도착 체크
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
            dragon.agent.isStopped = false;
        }

        hitPlayerIDs.Clear();
    }


}
