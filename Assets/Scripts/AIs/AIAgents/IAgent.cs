using UnityEngine;
using UnityEngine.AI;

//AI控制器基類
[RequireComponent(typeof(Rigidbody))]
public abstract class IAgent : MonoBehaviour
{             

    protected Rigidbody m_Rigidbody;
    protected Collider m_Collider;
    [SerializeField] protected bool isActivated;

    #region Player Detect
    public float m_DistanceToPlayer { get; private set; }
    public Transform m_DetectedPlayerTrans { get; private set; }
    [SerializeField] float m_DetectPlayerRange = 5f;
    [SerializeField] float m_FleeRange = 10f;
    [SerializeField] LayerMask m_PlayerLayerMask = 0;
    #endregion

    #region Obstacle Avoid Check    
    [SerializeField] float checkWallDistance = 2f;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] protected float wallAvoidingTime = 3f;    
    protected bool isAvoidingWall;
    protected float wallAvoidTimer = 0f;
    protected RaycastHit wallInfo;
    Vector3 leaveWallDir;
    #endregion

    protected virtual void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<Collider>();
        MakeFsm();
    }

    protected abstract void MakeFsm();
    protected virtual void Update()
    {
        CheckingPlayerInRange();
        CheckingPlayerDistance();
    }        


    #region Common Action
    void CheckingPlayerInRange()
    {        
        Collider[] playerCollider = Physics.OverlapSphere(transform.position, m_DetectPlayerRange, m_PlayerLayerMask);        
        if(playerCollider.Length > 0)
        {
            m_DetectedPlayerTrans = playerCollider[0].transform;            
        }        
    }
    void CheckingPlayerDistance()
    {
        if(m_DetectedPlayerTrans == null) { return; }
        m_DistanceToPlayer = Vector3.Distance(m_DetectedPlayerTrans.position, 
            transform.position);
        if(m_DistanceToPlayer > m_FleeRange)
        {
            m_DetectedPlayerTrans = null;
            m_DistanceToPlayer = float.MaxValue;
        }        
    }

    protected void WallDetect()
    {
        if (Physics.Raycast(transform.position, transform.forward, out wallInfo, checkWallDistance, wallLayer))
        {
            if (wallInfo.collider != null)
            {
                isAvoidingWall = true;
            }
        }
    }
    protected void GetOutFromWall(float speed)
    {
        leaveWallDir = (Vector3.Reflect(transform.forward, wallInfo.normal).Y(transform.position.y) - transform.position.Y(transform.position.y)).normalized;
        m_Rigidbody.velocity = leaveWallDir * speed;
        transform.rotation = Quaternion.LookRotation(leaveWallDir);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.X(0).Z(0));
    }
    public abstract void ActivateAI();
    public abstract void DeactivateAI();
    #endregion
    

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.red.A(0.2f);
        Gizmos.DrawSphere(transform.position, m_DetectPlayerRange);    

        Gizmos.color = Color.blue.A(0.2f);
        Gizmos.DrawSphere(transform.position, m_FleeRange);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * checkWallDistance);
    }
}