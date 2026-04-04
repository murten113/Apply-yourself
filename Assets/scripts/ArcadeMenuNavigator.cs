using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Arcade-style menu: uses the same Input System actions as gameplay — typically
/// <b>Player / LookBinding</b> (Vector2, Y axis) for up/down and <b>Player / Interact</b> to confirm.
/// Highlights the active <see cref="Button"/> via the EventSystem.
/// Put this on the menu panel (or child) that activates when the menu opens.
/// Uses <see cref="Time.unscaledTime"/> so it works when pause sets timeScale to 0.
/// </summary>
[DisallowMultipleComponent]
public class ArcadeMenuNavigator : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Player/LookBinding (Vector2). Uses Y: positive = move up the list, negative = move down.")]
    [SerializeField] private InputActionReference lookBindingAction;
    [Tooltip("Player/Interact — same as tools / nodes; confirms the highlighted button.")]
    [SerializeField] private InputActionReference interactAction;

    [SerializeField] private Button[] menuItems;

    [Header("Stick / repeat")]
    [SerializeField] private float stickDeadzone = 0.55f;
    [SerializeField] private float stickRepeatCooldown = 0.22f;

    [Header("Submit")]
    [Tooltip("Invoke the selected button when Interact is pressed. If the UI Input Module also submits, turn off to avoid double-firing.")]
    [SerializeField] private bool invokeOnSubmit = true;

    private int _index;
    private float _nextNavTime;
    private float _lastLookY;

    private void OnEnable()
    {
        lookBindingAction?.action?.Enable();
        interactAction?.action?.Enable();

        _index = 0;
        _nextNavTime = 0f;
        _lastLookY = 0f;
        if (menuItems == null || menuItems.Length == 0)
            return;
        _index = FindFirstInteractableIndex(0, 1);
        ApplySelection();
    }

    private void Update()
    {
        if (menuItems == null || menuItems.Length == 0)
            return;

        var look = lookBindingAction?.action;
        if (look != null)
        {
            float y = look.ReadValue<Vector2>().y;

            if (Mathf.Abs(y) >= stickDeadzone)
            {
                bool firstEnter = Mathf.Abs(_lastLookY) < stickDeadzone;
                if (firstEnter || Time.unscaledTime >= _nextNavTime)
                {
                    // LookBinding: up on stick / keys = +Y → earlier item in list
                    MoveSelection(y > 0f ? -1 : 1);
                    _nextNavTime = Time.unscaledTime + stickRepeatCooldown;
                }
            }
            else
                _nextNavTime = 0f;

            _lastLookY = y;
        }

        if (invokeOnSubmit && InteractPressedThisFrame())
        {
            var btn = menuItems[_index];
            if (btn != null && btn.interactable)
                btn.onClick.Invoke();
        }
    }

    private bool InteractPressedThisFrame()
    {
        var a = interactAction?.action;
        return a != null && a.WasPressedThisFrame();
    }

    private void MoveSelection(int delta)
    {
        if (menuItems.Length == 0)
            return;

        int count = menuItems.Length;
        int start = _index;
        for (int i = 0; i < count; i++)
        {
            _index = (_index + delta + count) % count;
            var b = menuItems[_index];
            if (b != null && b.interactable)
            {
                ApplySelection();
                return;
            }
            if (_index == start)
                break;
        }
    }

    private int FindFirstInteractableIndex(int start, int dir)
    {
        int count = menuItems.Length;
        int idx = Mathf.Clamp(start, 0, count - 1);
        for (int i = 0; i < count; i++)
        {
            var b = menuItems[idx];
            if (b != null && b.interactable)
                return idx;
            idx = (idx + dir + count) % count;
        }
        return 0;
    }

    private void ApplySelection()
    {
        if (EventSystem.current == null)
            return;
        var btn = menuItems[_index];
        if (btn != null && btn.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
    }
}
