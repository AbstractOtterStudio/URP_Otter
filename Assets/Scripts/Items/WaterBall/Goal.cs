using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Goal : MonoBehaviour
{
    [Header("Team ID")] public string teamTag = "Blue"; // "Blue" or "Red"
    [Header("Goal Mouth Points")] public Transform goalLineCenter; public Transform bottomLeft; public Transform bottomRight;
    [Header("Shot Ref (optional)")] public Transform shotTargetRef;
    WaterBallGameManager gm;
    void Awake(){ GetComponent<BoxCollider>().isTrigger = true; gm = FindObjectOfType<WaterBallGameManager>(); }
    void OnTriggerEnter(Collider other){ if(other.GetComponent<Ball>()) gm?.GoalScored(teamTag); }
    public Vector3 Position => goalLineCenter? goalLineCenter.position : transform.position;
    public Vector3 ShotTargetReferencePoint => shotTargetRef? shotTargetRef.position : Position;
    public Vector3 BottomLeftRelativePosition  => bottomLeft?  transform.InverseTransformPoint(bottomLeft.position)  : Vector3.left;
    public Vector3 BottomRightRelativePosition => bottomRight? transform.InverseTransformPoint(bottomRight.position) : Vector3.right;
}