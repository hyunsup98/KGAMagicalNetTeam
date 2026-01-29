using UnityEngine;

public class BurningBehavior : IDebuffBehavior
{
    private float damagePerSecond; //틱뎀
    private GameObject fireEffect; //몸에 붙는 불 이펙트
    private IDamageable damageableTarget; //데미지를 줄 인터페이스

    public void OnEnter(IDebuffable target, DebuffInfo info)
    {
        //인포밸류를 데미지로
        damagePerSecond = info.Value;
        damageableTarget = target.gameObject.GetComponent<IDamageable>();

        //불 이펙트 생성
        if (info.VisualPrefab != null)
        {
            //뼈대중심가져오고
            Transform centerBone = target.GetCenterPosition();
            //파이어 생성
            fireEffect = Object.Instantiate(info.VisualPrefab, centerBone.position, Quaternion.identity);

            fireEffect.transform.SetParent(centerBone); //타겟 따라다니게
        }
    }

    public void OnExecute(IDebuffable target)
    {
        if (damageableTarget != null && damagePerSecond > 0)
        {
            damageableTarget.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }

    public void OnExit(IDebuffable target)
    {
        //불 이펙트 삭제
        if (fireEffect != null)
        {
            Object.Destroy(fireEffect);
        }
    }
}
