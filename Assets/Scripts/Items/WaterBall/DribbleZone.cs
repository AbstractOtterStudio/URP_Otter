using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class DribbleZone : MonoBehaviour
{
    [Header("Owner (auto or manual)")]
    public WaterPlayer ownerWP;                       // 可为空（人类玩家通常没有）
    public Transform manualOwnerRoot;
    public bool manualIsTeammate = true;

    [Header("Anchor / Radius")]
    public Transform dribbleAnchor;                   // 球吸附点（放在角色前方）
    [Min(0.2f)] public float radius = 2.2f;          // 默认调大，便于抢断/吸附

    [Header("Ring UI")]
    public LineRenderer ring;                         // 可留空，运行时自动创建
    [Range(12, 128)] public int ringSegments = 64;
    public float ringWidth = 0.06f;

    [Header("Colors")]
    public Color colorIdle = new Color(1f, 1f, 1f, 0.75f);
    public Color colorCarrier = new Color(0.2f, 1f, 0.2f, 0.95f);
    public Color colorStealFlash = new Color(0.2f, 1f, 0.2f, 1f);
    public float stealFlashDuration = 0.18f;

    [Header("Debug")]
    public bool drawGizmo = false;

    public bool isTeammate => ownerWP ? ownerWP.isTeammate : manualIsTeammate;

    Material _matRuntime;
    Coroutine _flashCR;
    bool _isCarrier;

    void Awake()
    {
        if (!ownerWP) ownerWP = GetComponentInParent<WaterPlayer>();
        EnsureRing();
        RedrawRing();
        SetIdleColor();
    }

    void OnValidate()
    {
        if (!ownerWP) ownerWP = GetComponentInParent<WaterPlayer>();
        if (ringSegments < 12) ringSegments = 12;
        if (Application.isPlaying) { RedrawRing(); ApplyColor(); }
    }

    public Vector3 AnchorXZ => dribbleAnchor ? dribbleAnchor.position : transform.position;

    public Vector3 OwnerPosXZ
    {
        get
        {
            if (ownerWP) return ownerWP.Pos;
            Transform t = manualOwnerRoot ? manualOwnerRoot : transform;
            return new Vector3(t.position.x, 0, t.position.z);
        }
    }

    public void EnsureRing()
    {
        if (!ring)
        {
            var go = new GameObject("DribbleRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.alignment = LineAlignment.View;
            ring.textureMode = LineTextureMode.Stretch;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.loop = true;
            ring.useWorldSpace = false;
            ring.widthMultiplier = ringWidth;

            _matRuntime = new Material(Shader.Find("Sprites/Default"));
            _matRuntime.renderQueue = 3000;
            _matRuntime.color = colorIdle;
            ring.material = _matRuntime;
        }
        else
        {
            _matRuntime = new Material(Shader.Find("Sprites/Default"));
            _matRuntime.renderQueue = 3000;
            _matRuntime.color = colorIdle;
            ring.material = _matRuntime;
        }
    }

    public void RedrawRing()
    {
        if (!ring) return;
        ring.widthMultiplier = ringWidth;
        ring.positionCount = ringSegments;

        float step = Mathf.PI * 2f / ringSegments;
        for (int i = 0; i < ringSegments; i++)
        {
            float a = step * i;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            ring.SetPosition(i, new Vector3(x, 0.05f, z)); // 稍微抬起防 Z-fighting
        }
    }

    void ApplyColor()
    {
        if (_matRuntime != null)
        {
            _matRuntime.color = _isCarrier ? colorCarrier : colorIdle;
        }
        else if (ring) ring.startColor = ring.endColor = _isCarrier ? colorCarrier : colorIdle;
    }

    public void SetCarrier(bool v)
    {
        _isCarrier = v;
        ApplyColor();
    }

    public void SetIdleColor()
    {
        _isCarrier = false;
        ApplyColor();
    }

    public void FlashSteal()
    {
        if (_flashCR != null) StopCoroutine(_flashCR);
        _flashCR = StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        if (_matRuntime != null) _matRuntime.color = colorStealFlash;
        else if (ring) ring.startColor = ring.endColor = colorStealFlash;
        yield return new WaitForSeconds(stealFlashDuration);
        ApplyColor();
        _flashCR = null;
    }
}
