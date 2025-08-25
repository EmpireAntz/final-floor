using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Wiring")]
    public Camera cam;

    [Header("Settings")]
    public float range = 3f;
    public LayerMask interactMask = ~0;
    public Key interactKey = Key.E;

    [Header("Prompt UI")]
    public Font promptFont;
    [Range(8, 96)] public int promptFontSize = 28;
    public Color promptColor = Color.white;
    public Color promptOutlineColor = new Color(0,0,0,0.8f);
    [Range(0, 8)] public int promptOutline = 2;
    [Tooltip("Normalized (0..1). (0.5, 0.85) = centered near bottom")]
    public Vector2 promptAnchor = new Vector2(0.5f, 0.85f);
    public Vector2 promptPixelOffset = Vector2.zero;
    [Tooltip("{0} = prompt text, {1} = key")]
    public string promptFormat = "{0}  [{1}]";

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

        string keyName = interactKey.ToString();
        string label = string.Format(promptFormat, lookingAt.prompt, keyName);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            font = promptFont ? promptFont : GUI.skin.font,
            fontSize = promptFontSize,
            wordWrap = false,
            richText = false
        };

        Vector2 size = style.CalcSize(new GUIContent(label));
        float x = Screen.width  * promptAnchor.x + promptPixelOffset.x;
        float y = Screen.height * promptAnchor.y + promptPixelOffset.y;
        Rect r = new Rect(x - size.x * 0.5f, y - size.y * 0.5f, size.x, size.y);

        if (promptOutline > 0)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = promptOutlineColor;
            for (int dx = -promptOutline; dx <= promptOutline; dx++)
            for (int dy = -promptOutline; dy <= promptOutline; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                GUI.Label(new Rect(r.x + dx, r.y + dy, r.width, r.height), label, style);
            }
            style.normal.textColor = old;
        }

        style.normal.textColor = promptColor;
        GUI.Label(r, label, style);
    }
}
