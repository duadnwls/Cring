using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 빌드에서 Esc로 커서를 풀어 창을 벗어날 수 있게 한다.
/// 에디터에서는 Esc가 원래 커서를 풀어주지만 빌드에서는 갇히므로 반드시 필요하다.
/// </summary>
public class PauseAndCursor : MonoBehaviour
{
    bool _cursorFree;

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
#else
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
#endif
        _cursorFree = !_cursorFree;
        Cursor.lockState = _cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _cursorFree;

        // StarterAssetsInputs가 포커스 복귀 때 다시 잠그지 않도록 함께 꺼둔다
        var inputs = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.cursorLocked = !_cursorFree;
            inputs.cursorInputForLook = !_cursorFree;
        }
    }
}
