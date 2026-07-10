using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputFocusManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private InputAction navigateAction;
    private InputAction submitAction;
    private GameObject lastKeyboardSelected;

    void Awake()
    {
        var uiMap = inputActions.FindActionMap("UI");
        navigateAction = uiMap.FindAction("UICtrol");
        submitAction = uiMap.FindAction("UISelect");
    }

    void OnEnable()
    {
        navigateAction.Enable();
        submitAction.Enable();
        navigateAction.performed += OnKeyboardInput;
        submitAction.performed += OnKeyboardInput;
    }

    void OnDisable()
    {
        navigateAction.performed -= OnKeyboardInput;
        submitAction.performed -= OnKeyboardInput;
        navigateAction.Disable();
        submitAction.Disable();
    }

    void Update()
    {
        // Simpan selection terakhir dari keyboard
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
            lastKeyboardSelected = current;
    }

    private void OnKeyboardInput(InputAction.CallbackContext ctx)
    {
        // Kalau tidak ada yang ter-select, kembalikan ke last selected
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (lastKeyboardSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(lastKeyboardSelected);
            }
        }
    }
}