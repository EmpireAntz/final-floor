using UnityEngine;
using TMPro;

public class StatDisplayUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerStats playerStats;                 // auto-found if left empty
    public EquipmentStatsApplier applier;           // auto-found if left empty
    public TMP_Text targetText;                     // auto-filled from this object if left empty

    [Header("Formatting")]
    [TextArea] public string format =
        "Damage: {0}(+{1})  Health: {2}(+{3})  Stamina: {4}(+{5})";
    public bool roundToInt = true;

    [Header("Bonus Appearance")]
    public bool colorBonuses = false;
    public Color bonusColor = new Color(0.2f, 1f, 0.2f);

    void Awake()
    {
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();
        if (!applier)     applier     = FindObjectOfType<EquipmentStatsApplier>();
        if (!targetText)  targetText  = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (applier) applier.OnRecalculated += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (applier) applier.OnRecalculated -= Refresh;
    }

    public void Refresh()
    {
        if (!playerStats || !targetText) return;

        float baseD  = applier ? applier.BaseDamage    : playerStats.damage;
        float baseHP = applier ? applier.BaseMaxHealth : playerStats.maxHealth;
        float baseSt = playerStats.maxStamina;

        float bD  = applier ? applier.LastBonusDamage    : 0f;
        float bHP = applier ? applier.LastBonusMaxHealth : 0f;
        float bSt = 0f; // no stamina bonuses yet

        string F(float v) => roundToInt ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##");

        string txt = string.Format(format, F(baseD), F(bD), F(baseHP), F(bHP), F(baseSt), F(bSt));

        if (colorBonuses)
        {
            string col = ColorUtility.ToHtmlStringRGB(bonusColor);
            txt = txt.Replace($"(+{F(bD)})",  $"<color=#{col}>(+{F(bD)})</color>")
                     .Replace($"(+{F(bHP)})", $"<color=#{col}>(+{F(bHP)})</color>")
                     .Replace($"(+{F(bSt)})", $"<color=#{col}>(+{F(bSt)})</color>");
        }

        targetText.text = txt;
    }
}
