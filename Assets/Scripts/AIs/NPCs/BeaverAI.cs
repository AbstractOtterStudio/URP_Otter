using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeaverAI : MonoBehaviour
{
    [Header("Refs")]
    public DamManager dam;

    [Header("Patrol Points (把两个空物体拖进来)")]
    public Transform pointA;
    public Transform pointB;

    [Header("Patrol Settings")]
    public float moveSpeed = 0.6f;          // 巡逻速度
    public float waitMin = 0.8f;            // 到点停顿最短
    public float waitMax = 1.6f;            // 到点停顿最长
    public bool lockYToStart = true;        // Y 轴保持初始高度

    [Header("Build Feedback")]
    public float buildCooldown = 0.6f;
    Animator anim;

    // internal
    Vector3 startPos;
    float fixedY;
    Vector3 target;
    float waitTimer;
    bool isWaiting = false;

    bool isBuilding = false;
    float buildTimer = 0f;

    void Awake() { anim = GetComponent<Animator>(); }

    void Start()
    {
        startPos = transform.position;
        fixedY = startPos.y;
        if (!pointA || !pointB)
            Debug.LogWarning("[BeaverAI] 请在 Inspector 里指定 pointA / pointB。");

        target = (pointA ? pointA.position : startPos + Vector3.left)
               ; // 初始目标
    }

    void Update()
    {
        UpdateBuildState();
        UpdatePatrol();
    }

    void UpdateBuildState()
    {
        if (!isBuilding) return;
        buildTimer -= Time.deltaTime;
        if (buildTimer <= 0f)
        {
            isBuilding = false;
            if (anim) anim.SetBool("Building", false);
        }
    }

    void UpdatePatrol()
    {
        if (!pointA || !pointB) return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                target = (Vector3.Distance(transform.position, pointA.position) <
                          Vector3.Distance(transform.position, pointB.position))
                         ? pointB.position : pointA.position;
            }
            return;
        }

        // 朝目标移动
        Vector3 cur = transform.position;
        if (lockYToStart) { cur.y = fixedY; }
        Vector3 tgt = target; if (lockYToStart) { tgt.y = fixedY; }

        Vector3 next = Vector3.MoveTowards(cur, tgt, moveSpeed * Time.deltaTime);
        transform.position = next;

        // 面向上游或保持不转也可：这里简单朝移动方向
        Vector3 dir = (tgt - cur); dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f) transform.forward = dir.normalized;

        // 到达目标，进入等待
        if ((next - tgt).sqrMagnitude < 0.0001f)
        {
            isWaiting = true;
            waitTimer = Random.Range(waitMin, waitMax);
        }
    }

    // —— 命中接收：既支持 Trigger 也支持 Collision（避免碰撞体设置不一致） —— //
    void OnTriggerEnter(Collider other) { TryReceive(other); }
    void OnCollisionEnter(Collision col) { TryReceive(col.collider); }

    void TryReceive(Collider col)
    {
        if (!col) return;

        // 必须：有 Catchable 且被玩家“投出”
        Catchable catchable = col.GetComponent<Catchable>();
        if (!catchable) return;
        if (!catchable.WasThrownByPlayer)  // 下落路过/非玩家投出 → 忽略
            return;

        RiverObject ro = col.GetComponent<RiverObject>();
        if (!ro) return;

        switch (ro.objectType)
        {
            case RiverObjectType.Material:
                dam.AddMaterial();
                BeginBuild();
                break;
            case RiverObjectType.Hazard:
                dam.DamageDam();
                break;
            case RiverObjectType.Junk:
                // 可选：不处理或轻微惩罚
                break;
        }
        Destroy(col.gameObject);
    }

    void BeginBuild()
    {
        isBuilding = true;
        buildTimer = buildCooldown;
        if (anim) anim.SetBool("Building", true);
    }

    // 场景里可视化巡逻点
    void OnDrawGizmosSelected()
    {
        if (pointA && pointB)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawSphere(pointA.position, 0.1f);
            Gizmos.DrawSphere(pointB.position, 0.1f);
        }
    }
}
