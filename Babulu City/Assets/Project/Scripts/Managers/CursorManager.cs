using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private RectTransform virtualCursor;

    private bool activeCursor;

    void Start()
    {
        virtualCursor.gameObject.SetActive(false);
    }

    public void EnableCursor()
    {
        activeCursor = true;

        Cursor.visible = false;          // Sembunyikan cursor Windows
        virtualCursor.gameObject.SetActive(true);
    }

    public void DisableCursor()
    {
        activeCursor = false;

        Cursor.visible = true;           // Tampilkan lagi cursor Windows
        virtualCursor.gameObject.SetActive(false);
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        if (!activeCursor) return;

        virtualCursor.position = context.ReadValue<Vector2>();
    }
}