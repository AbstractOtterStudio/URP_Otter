using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
/**
 * Based on the old input system. Not recommended for production use.
 */
namespace FluffyExample {
    public class SimpleCharacterAssetsInputs : MonoBehaviour {
        [HideInInspector] public Vector2 move;
        [HideInInspector] public bool jump;
        [HideInInspector] public bool sprint;

        private void Update() {
#if ENABLE_LEGACY_INPUT_MANAGER
            var moveX = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
            var moveZ = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
            move = new Vector2(moveX, moveZ);
            if (Input.GetKeyDown(KeyCode.Space)) {
                jump = true;
            }

            if (Input.GetKeyUp(KeyCode.Space)) {
                jump = false;
            }

            sprint = Input.GetKey(KeyCode.LeftShift);

#else
            var moveX = Keyboard.current.aKey.isPressed ? -1f : Keyboard.current.dKey.isPressed ? 1f : 0f;
            var moveZ = Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f;
            move = new Vector2(moveX, moveZ);
            jump = Keyboard.current.spaceKey.isPressed;
            sprint = Keyboard.current.shiftKey.isPressed;
#endif
        }
    }
}