using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    
    [SerializeField]
    private Color lineColor;

    Vector3 _startPos { get; set; }
    Vector3 _initVecl { get; set; }
    float _timestep { get; set; }

    public float Strength { get; private set; }
    public float FlightTime { get; private set; }

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

    public void MakeTrajectory(Vector3 startPos, Vector3 fwd, float strength, float mass)
    {
        Strength = strength;

        _initVecl = fwd * Strength / mass;
        lineRenderer.positionCount = 50;

        FlightTime = 2 * _initVecl.y / -Physics.gravity.y;
        var timestep = FlightTime / lineRenderer.positionCount;

        for (int i = 0; i < lineRenderer.positionCount; ++i)
        {
            var t = i * timestep;
            var p = startPos + t * _initVecl;
            p.y = startPos.y + _initVecl.y * t + Physics.gravity.y * 0.5f * t * t;
            lineRenderer.SetPosition(i, p);
        }
    }

    public void FuckOff()
    {
        lineRenderer.positionCount = 0;
    }

    public IEnumerable<Vector3> TrajectoryPoints()
    {
        if (lineRenderer.positionCount == 0)
        {
            yield break;
        }

        for (int i = 0; i < lineRenderer.positionCount; ++i)
        {
            var t = i * _timestep;
            var p = _startPos + t * _initVecl;
            p.y = _startPos.y + _initVecl.y * t + Physics.gravity.y * 0.5f * t * t;
            yield return p;
        }
    }
}