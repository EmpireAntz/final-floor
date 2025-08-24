using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    Animator animator;
    static readonly int HashPunch = Animator.StringToHash("Punch");
    static readonly int HashKick = Animator.StringToHash("Kick");

    [HideInInspector] public bool isPunching = false; // <— public flag

    void Awake()
    {
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    void Update()
    {
         if (!animator) return;

        // Ignore clicks when over UI (optional)
        bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();

        // Mouse / Keyboard
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !overUI)
            animator.SetTrigger(HashPunch);

        if (Keyboard.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            animator.SetTrigger(HashKick);

        // Gamepad
        var gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.rightTrigger.wasPressedThisFrame) animator.SetTrigger(HashPunch); 
            if (gp.rightShoulder.wasPressedThisFrame) animator.SetTrigger(HashKick);      
        }
    }
}
