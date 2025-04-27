using UnityEngine;
using Shapes;
using UnityEngine.Events;


public class QTEMiniGame : UIBase
{
    public enum Zone
    {
        None = 0,
        TargetZone = 1,
        BullseyeZone = 2,
    }

    public class Result
    {
        public Zone zoneHit;
        public float timeTaken;
    }

    public interface IHandler
    {
        /**
        * Called when the mini game is about to start. This is a good time to configure the mini game.
        */
        void OnMiniGameStart(QTEMiniGame miniGame);

        /**
        * Called when the user inputs a key
        * The result of the key input is passed in
        * The callback should return true if the mini game should continue, else the mini game will end automatically
        */
        void OnMiniGameUserInput(Result result, out bool shouldContinue);

        /**
        * Called when the mini game ends for any reason, e.g. time runs out, user quits, etc.
        * The result of the mini game is passed in
        */
        void OnMiniGameEnd();
    }

    // public properties
    public KeyCode InputKey
    {
        get => inputKey;
        set => inputKey = value;
    }
    public KeyCode ExitKey
    {
        get => exitKey;
        set => exitKey = value;
    }
    public float CursorSpeedDegPerSec
    {
        get => cursorSpeedDegPerSec;
        set
        {
            cursorSpeedDegPerSec = value;
            FixFields();
        }
    }
    public float TargetZoneStart
    {
        get => targetZoneStart;
        set
        {
            targetZoneStart = value;
            FixFields();
        }
    }
    public float TargetZoneEnd
    {
        get => targetZoneEnd;
        set
        {
            targetZoneEnd = value;
            FixFields();
        }
    }
    public float BullseyeZoneStart
    {
        get => bullseyeZoneStart;
        set
        {
            bullseyeZoneStart = value;
            FixFields();
        }
    }
    public float BullseyeZoneEnd
    {
        get => bullseyeZoneEnd;
        set
        {
            bullseyeZoneEnd = value;
            FixFields();
        }
    }


    // serializable fields
    [SerializeField] Transform shapes;

    [Header("Gameplay")]
    [SerializeField][Tooltip("If true, the mini game will end automatically after a certain time")] bool isTimed = false;
    [SerializeField][Tooltip("The duration of the mini game in seconds, only applicable if isTimed is true")] float gameDuration = 10f;

    [Header("Input")]
    [SerializeField] KeyCode inputKey = KeyCode.Space;
    [SerializeField] KeyCode exitKey = KeyCode.Escape;

    [Header("Cursor")]
    [SerializeField] Shapes.Line cursorLine;
    [SerializeField] Transform cursorAnchor;

    [Range(0f, 180f)]
    [SerializeField][Tooltip("The speed of the cursor (degrees per second)")] float cursorSpeedDegPerSec = 1f;
    [SerializeField][Tooltip("The starting and ending Z rotation of the cursor")] Vector2 cursorRotZRange = new Vector2(90f, 270f);

    [Range(0.1f, 3f)]
    [SerializeField] float cursorLineThickness = 0.4f;
    [Range(0f, 3f)]
    [SerializeField] float cursorLineStartOffset = 1.9f;
    [Range(0f, 3f)]
    [SerializeField] float cursorLineLength = 2f;

    [Header("Arcs")]
    [SerializeField] Shapes.Disc backgroundArc; // Full sector
    [SerializeField] Shapes.Disc targetZoneArc; // Target Zone
    [SerializeField] Shapes.Disc bullseyeZoneArc; // Bullseye Zone

    [Range(0.1f, 3f)]
    [SerializeField] float arcThickness = 0.4f;
    [Range(0, 360)]
    [SerializeField][Tooltip("The angle subtended by the background arc (degrees)")] float backgroundArcAngle = 160f;

    [Range(0, 1)]
    [SerializeField][Tooltip("Target Zone Start (Percentage of the background arc length)")] float targetZoneStart = 0.25f;
    [Range(0, 1)]
    [SerializeField][Tooltip("Target Zone End (Percentage of the background arc length)")] float targetZoneEnd = 0.75f;

    [Range(0, 1)]
    [SerializeField][Tooltip("Bullseye Zone Start (Percentage of the target zone arc length)")] float bullseyeZoneStart = 0.1f;
    [Range(0, 1)]
    [SerializeField][Tooltip("Bullseye Zone End (Percentage of the target zone arc length)")] float bullseyeZoneEnd = 0.9f;

    // internal states
    private bool _isActive
    {
        get
        {
            return shapes.gameObject.activeSelf;
        }
        set
        {
            shapes.gameObject.SetActive(value);
        }
    }
    private Vector3 _initialCursorAnchorPosition;
    private float _initialCursorAnchorRotZ;

    private float _curCursorRotZ; // in degrees
    private bool _curCursorDirClockwise = true;
    private float _timer;
    private IHandler _curHandler;

    [DebugDisplay]
    private Zone _curZoneHit = Zone.None;

    void ResetState()
    {
        _isActive = false;

        cursorAnchor.localPosition = _initialCursorAnchorPosition;
        cursorAnchor.localRotation = Quaternion.Euler(0f, 0f, _initialCursorAnchorRotZ);

        _curCursorRotZ = cursorRotZRange.x;
        _curCursorDirClockwise = true;
        _curZoneHit = Zone.None;
        _timer = 0f;

        _curHandler = null;
    }


    public override void Init()
    {
        _isActive = false;

        _initialCursorAnchorPosition = cursorAnchor.localPosition;
        _initialCursorAnchorRotZ = cursorAnchor.localRotation.eulerAngles.z;
        _curCursorRotZ = cursorRotZRange.x;
        _timer = 0f;
    }

    /**
     * Starts the QTE mini game
     */
    public void Activate(IHandler handler)
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        handler.OnMiniGameStart(this);
    }


    /**
     * Stops and closes the QTE mini game
     */
    public void Deactivate()
    {
        _isActive = false;
        ResetState();
    }

    /**
     * Get the angle range of the arc in degrees, converted to the space expected by the transform
     */
    Vector2 GetArcAngleRangeDegrees(Disc arc)
    {
        Vector2 arcAngles = new Vector2(
            arc.AngRadiansStart * Mathf.Rad2Deg - 90f,
            arc.AngRadiansEnd * Mathf.Rad2Deg - 90f
        );

        return arcAngles;
    }

    void Update()
    {
        if (!_isActive)
        {
            return;
        }

        _timer += Time.deltaTime;

        if (_curCursorDirClockwise)
        {
            _curCursorRotZ += cursorSpeedDegPerSec * Time.deltaTime;
        }
        else
        {
            _curCursorRotZ -= cursorSpeedDegPerSec * Time.deltaTime;
        }

        if (_curCursorRotZ >= cursorRotZRange.y)
        {
            _curCursorDirClockwise = false;
        }
        else if (_curCursorRotZ <= cursorRotZRange.x)
        {
            _curCursorDirClockwise = true;
        }

        cursorAnchor.localRotation = Quaternion.Euler(0f, 0f, _curCursorRotZ);

        var targetZoneArcAngles = GetArcAngleRangeDegrees(targetZoneArc);
        var bullseyeZoneArcAngles = GetArcAngleRangeDegrees(bullseyeZoneArc);

        // this assumes that the cursor is in the same space as the arcs

        if (bullseyeZoneArcAngles.x <= _curCursorRotZ && _curCursorRotZ <= bullseyeZoneArcAngles.y)
        {
            _curZoneHit = Zone.BullseyeZone;
        }
        else if (targetZoneArcAngles.x <= _curCursorRotZ && _curCursorRotZ <= targetZoneArcAngles.y)
        {
            _curZoneHit = Zone.TargetZone;
        }
        else
        {
            _curZoneHit = Zone.None;
        }

        ProcessInput();

        if (isTimed)
        {
            if (_timer >= gameDuration)
            {
                Deactivate();
                _curHandler.OnMiniGameEnd();
            }
        }
    }

    void ProcessInput()
    {
        bool shouldContinue = true;
        if (Input.GetKeyDown(inputKey))
        {
            Debug.Log($"Input key pressed, zone hit: {_curZoneHit}");
            _curHandler.OnMiniGameUserInput(new Result { zoneHit = _curZoneHit, timeTaken = _timer }, out shouldContinue);
        }

        if (Input.GetKeyDown(exitKey) || !shouldContinue)
        {
            Deactivate();
            _curHandler.OnMiniGameEnd();
        }
    }

    void FixFields()
    {
        if (shapes != null)
        {
            shapes.transform.localPosition = Vector3.zero;
            // this is to center the arc angle space, see GetArcAngleRangeDegrees
            shapes.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);
        }

        if (!backgroundArc || !targetZoneArc || !bullseyeZoneArc)
        {
            return;
        }

        // game duration must be at least the time it takes to complete a full circle
        gameDuration = Mathf.Max(gameDuration, backgroundArcAngle * Mathf.Deg2Rad / cursorSpeedDegPerSec);

        // arcs
        targetZoneStart = Mathf.Clamp(targetZoneStart, 0f, 1f);
        targetZoneEnd = Mathf.Clamp(targetZoneEnd, targetZoneStart, 1f);
        bullseyeZoneStart = Mathf.Clamp(bullseyeZoneStart, 0f, 1f);
        bullseyeZoneEnd = Mathf.Clamp(bullseyeZoneEnd, bullseyeZoneStart, 1f);

        float backgroundArcAngleRadians = backgroundArcAngle * Mathf.Deg2Rad;
        float halfBackgroundArcAngleRadians = backgroundArcAngleRadians / 2f;

        backgroundArc.AngRadiansStart = -halfBackgroundArcAngleRadians;
        backgroundArc.AngRadiansEnd = halfBackgroundArcAngleRadians;

        targetZoneArc.AngRadiansStart = -halfBackgroundArcAngleRadians + targetZoneStart * backgroundArcAngleRadians;
        targetZoneArc.AngRadiansEnd = -halfBackgroundArcAngleRadians + targetZoneEnd * backgroundArcAngleRadians;

        float targetZoneArcAngleRadians = targetZoneArc.AngRadiansEnd - targetZoneArc.AngRadiansStart;
        bullseyeZoneArc.AngRadiansStart = targetZoneArc.AngRadiansStart + bullseyeZoneStart * targetZoneArcAngleRadians;
        bullseyeZoneArc.AngRadiansEnd = targetZoneArc.AngRadiansStart + bullseyeZoneEnd * targetZoneArcAngleRadians;

        backgroundArc.Thickness = targetZoneArc.Thickness = bullseyeZoneArc.Thickness = arcThickness;

        // cursor line
        if (!cursorLine)
        {
            return;
        }

        cursorLineLength = Mathf.Max(cursorLineLength, 0f);
        cursorLineThickness = Mathf.Max(cursorLineThickness, 0f);
        cursorLineStartOffset = Mathf.Max(cursorLineStartOffset, 0f);

        cursorLine.Start = Vector3.up * cursorLineStartOffset;
        cursorLine.End = cursorLine.Start + Vector3.up * cursorLineLength;
        cursorLine.Thickness = cursorLineThickness;
    }

    void OnValidate()
    {
        FixFields();
    }
}
