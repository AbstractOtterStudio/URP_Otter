using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    
    [SerializeField]
    private Color lineColor;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.startColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.5f);
        lineRenderer.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.5f);
    }

    public void MakeTrajectory(Vector3 startPosition, Vector3 fwd, float strength, float mass)
    {
        var initVelocity = fwd * strength / mass;
        var timestep = Time.fixedDeltaTime / Physics.defaultSolverVelocityIterations;
        lineRenderer.positionCount = 50;
        for (int i = 0; i < lineRenderer.positionCount; ++i)
        {
            var t = i * timestep;
            var p = startPosition + t * initVelocity;
            p.y = startPosition.y + initVelocity.y * t + Physics.gravity.y * 0.5f * t * t;
            lineRenderer.SetPosition(i, p);
        }
    }
}