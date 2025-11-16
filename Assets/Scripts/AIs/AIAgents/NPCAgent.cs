using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using System.Collections;

//AI控制器基类，本质是一个使用behavior tree的navmesh agent
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorTree))]
[RequireComponent(typeof(BehaviorManager))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCAgent : MonoBehaviour
{
    BehaviorTree[] behaviorTrees;
    BehaviorManager behaviorManager;

    protected NavMeshAgent navMeshAgent;
    protected Rigidbody rb;

    public NavMeshAgent NavMeshAgent { get { return navMeshAgent; } }
    public Rigidbody Rigidbody { get { return rb; } }

    bool isActive = true;
    IEnumerator waterSwitchCoroutine = null;

    bool? isUnderwater = false;

    public bool IsUnderwater
    {
        get
        {
            if (isUnderwater == null)
            {
                isUnderwater = navMeshAgent.SamplePathPosition(NavMesh.AllAreas, 0f, out var navMeshHit) &&
                    navMeshHit.mask == WaterNavmesh.UnderwaterLayerMask;
            }
            return isUnderwater.Value;
        }
    }

    public bool IsSwitchingUnderwater
    {
        get
        {
            return waterSwitchCoroutine != null;
        }
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        behaviorTrees = GetComponents<BehaviorTree>();
        behaviorManager = GetComponent<BehaviorManager>();
        behaviorManager.UpdateInterval = UpdateIntervalType.Manual;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance.GetGameAction() && isActive)
        {
            behaviorManager.Tick();
        }
    }

    public void ActivateAI()
    {
        if (!isActive)
        {
            foreach (var tree in behaviorTrees)
                tree.EnableBehavior();
        }
        isActive = true;
    }
    public void DeactivateAI()
    {
        if (isActive)
        {
            foreach (var tree in behaviorTrees)
                tree.DisableBehavior(); // pause is false -- the tree will end
        }
        isActive = false;
    }
    public void ResetState() { }

    /**
    * 转移NPC到水下，并使用水下Navmesh
    * @param isUnderwater 是否在水下
    */
    public void SetUnderwater(bool targetIsUnderwater)
    {
        if (IsUnderwater == targetIsUnderwater)
        {
            return;
        }

        IEnumerator WaterSwitchCoroutine(Vector3 targetPosition, float maxSpeed)
        {
            NavMeshAgent.enabled = false;
            while (Vector3.Distance(transform.position, targetPosition) >= Mathf.Epsilon)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, maxSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPosition;
            NavMeshAgent.enabled = true;
            isUnderwater = targetIsUnderwater;
            waterSwitchCoroutine = null;
        }

        if (waterSwitchCoroutine != null)
        {
            StopCoroutine(waterSwitchCoroutine);
            waterSwitchCoroutine = null;
        }

        int layerMask = targetIsUnderwater ? WaterNavmesh.UnderwaterLayerMask : WaterNavmesh.WaterSurfaceLayerMask;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 100f, layerMask))
        {
            Debug.LogError("Failed to sample position for water switch");
            return;
        }
        waterSwitchCoroutine = WaterSwitchCoroutine(hit.position, NavMeshAgent.speed);
        StartCoroutine(waterSwitchCoroutine);
    }

    public int GetLayerMask()
    {
        return IsUnderwater ? WaterNavmesh.UnderwaterLayerMask : WaterNavmesh.WaterSurfaceLayerMask;
    }
}