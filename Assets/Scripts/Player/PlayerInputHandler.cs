using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector3 MovementInput { get; private set; }
    public bool IsInteracting { get; private set; }
    public bool IsEatingOrKnocking { get; private set; }
    public bool IsDiving { get; private set; }
    public bool IsAddingSpeed { get; private set; }

    private void Update()
    {
        HandleMovementInput();
        HandleActionInput();
    }

    private void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MovementInput = new Vector3(h, 0, v).normalized;
    }

    private void HandleActionInput()
    {
        IsInteracting = Input.GetKeyDown(GlobalSetting.InterectKey);
        IsEatingOrKnocking = Input.GetKeyDown(GlobalSetting.EatOrKnockKey);
        IsDiving = Input.GetKeyDown(GlobalSetting.DiveKey);
        IsAddingSpeed = Input.GetKey(GlobalSetting.AddSpeedKey);
    }
}
