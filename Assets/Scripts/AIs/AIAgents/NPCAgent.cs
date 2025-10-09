using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;

//AI控制器基类，本质是一个FSM，每个节点可以是一个behavior tree
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorManager))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCAgent : MonoBehaviour
{
    BehaviorManager BehaviorMgr;
    protected NavMeshAgent m_NavMeshAgent;
    protected Rigidbody m_Rigidbody;

    public NavMeshAgent NavMeshAgent { get { return m_NavMeshAgent; } }
    public Rigidbody Rigidbody { get { return m_Rigidbody; } }

    protected virtual void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        BehaviorMgr = GetComponent<BehaviorManager>();
        BehaviorMgr.UpdateInterval = UpdateIntervalType.Manual;
    }

    protected virtual void Update()
    {
        BehaviorMgr.Tick();
    }

    public void ActivateAI() { }
    public void DeactivateAI() { }
    public void ResetState() { }
}