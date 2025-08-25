using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("Wiring")] public Camera cam;
    [Header("Settings")] public float range = 3f;
    public LayerMask interactMask = ~0;
    public Key interactKey = Key.E;

    private Interactable lookingAt;

    void Update()
    {
        if (!cam) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        lookingAt = null;

        if (Physics.Raycast(ray, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide))
            lookingAt = hit.collider.GetComponentInParent<Interactable>();

        if (lookingAt != null && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
            lookingAt.Interact(gameObject);
    }

    void OnGUI()
    {
        if (lookingAt == null) return;
        string label = $"{lookingAt.prompt}  [{interactKey}]";
        var size = GUI.skin.label.CalcSize(new GUIContent(label));
        GUI.Label(new Rect((Screen.width - size.x)/2f, Screen.height*0.85f, size.x, size.y), label);
    }
}
