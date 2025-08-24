using UnityEngine;
using UnityEngine.UI;
// If you don't use TextMeshPro, delete the next line and the TMP fields/uses.
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Source")]
    public PlayerStats stats;                     // drag your Player with PlayerStats here

    [Header("Health Bar")]
    public Image healthFill;                      // Image.type = Filled (Horizontal)
    public Gradient healthGradient;               // optional color gradient
    public TextMeshProUGUI healthText;            // optional; can be null

    [Header("Stamina Bar")]
    public Image staminaFill;                     // Image.type = Filled (Horizontal)
    public Gradient staminaGradient;              // optional
    public Image staminaCooldownOverlay;          // optional faint overlay shown during cooldown
    public TextMeshProUGUI staminaText;           // optional

    [Header("Look & Feel")]
    [Tooltip("Lerp speed for bar smoothing (higher = snappier).")]
    public float smooth = 8f;

    float _hVis;   // visual (smoothed) 0..1
    float _sVis;   // visual (smoothed) 0..1

    void Reset()
    {
        // Try to auto-find PlayerStats in the scene when you add the component
        if (!stats) stats = FindFirstObjectByType<PlayerStats>();
    }

    void LateUpdate()
    {
        if (!stats) return;

        // Normalized values 0..1
        float h = Mathf.Clamp01(stats.health  / Mathf.Max(1f, stats.maxHealth));
        float s = Mathf.Clamp01(stats.stamina / Mathf.Max(1f, stats.maxStamina));

        // Smooth the visual fill
        _hVis = Mathf.Lerp(_hVis, h, Time.deltaTime * smooth);
        _sVis = Mathf.Lerp(_sVis, s, Time.deltaTime * smooth);

        // HEALTH
        if (healthFill)
        {
            healthFill.fillAmount = _hVis;
            if (healthGradient.colorKeys.Length > 0)
                healthFill.color = healthGradient.Evaluate(_hVis);
        }
        if (healthText) healthText.text = Mathf.RoundToInt(h * 100f) + "%";

        // STAMINA
        if (staminaFill)
        {
            staminaFill.fillAmount = _sVis;
            if (staminaGradient.colorKeys.Length > 0)
                staminaFill.color = staminaGradient.Evaluate(_sVis);
        }
        if (staminaText)
        {
            // If you added exhaustion to PlayerStats, show a tag; otherwise remove the IsExhausted bit
            string extra = stats.IsExhausted ? " (cooldown)" : "";
            staminaText.text = Mathf.RoundToInt(s * 100f) + "%" + extra;
        }

        // Optional cooldown overlay (pulse while exhausted)
        if (staminaCooldownOverlay)
        {
            bool show = stats.IsExhausted; // remove/ignore if you didn’t add exhaustion
            staminaCooldownOverlay.enabled = show;
            if (show)
            {
                Color c = staminaCooldownOverlay.color;
                c.a = 0.35f + 0.25f * Mathf.PingPong(Time.time * 2f, 1f);
                staminaCooldownOverlay.color = c;
            }
        }
    }
}
