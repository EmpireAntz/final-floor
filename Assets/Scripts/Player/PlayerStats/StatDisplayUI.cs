using UnityEngine;
using TMPro;

public class StatDisplayUI : MonoBehaviour
{
    public PlayerStats stats;
    public EquipmentStatsApplier applier;   // optional
    public TMP_Text text;

    [Header("Formatting")]
    public bool roundToInt = true;
    public string lineSeparator = "\n";

    [Header("Per-Stat Value Colors")]
    public bool colorize = true;
    public Color damageColor  = new Color(1f, 0.5f, 0.5f);   // red-ish
    public Color healthColor  = new Color(0.5f, 1f, 0.5f);   // green-ish
    public Color staminaColor = new Color(0.7f, 0.9f, 1f);   // blue-ish
    public Color defenseColor = new Color(1f, 0.85f, 0.4f);  // gold-ish
    public Color critColor    = new Color(1f, 0.7f, 1f);     // magenta-ish

    void Reset()  { text = GetComponent<TMP_Text>(); }
    void Awake()
    {
        if (!stats) stats = FindObjectOfType<PlayerStats>();
        if (!text)  text  = GetComponent<TMP_Text>();
    }
    void OnEnable()
    {
        if (stats)   stats.OnStatsChanged += Refresh;
        if (applier) applier.OnRecalculated += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        if (stats)   stats.OnStatsChanged -= Refresh;
        if (applier) applier.OnRecalculated -= Refresh;
    }

    string F(float v) => roundToInt ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##");
    static string Hex(Color c) { var c32 = (Color32)c; return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}"; }
    string ColorWrap(string s, Color c) => colorize ? $"<color={Hex(c)}>{s}</color>" : s;

    public void Refresh()
    {
        if (!stats || !text) return;

        // Base + bonus (prefer applier for DMG/HP)
        float baseD  = applier ? applier.BaseDamage         : stats.damage;
        float baseHP = applier ? applier.BaseMaxHealth      : stats.maxHealth;
        float bD     = applier ? applier.LastBonusDamage    : stats.bonusDamage;
        float bHP    = applier ? applier.LastBonusMaxHealth : stats.bonusMaxHealth;

        float totalD  = baseD  + bD;
        float totalHP = baseHP + bHP;
        float totalSt = stats.maxStamina;
        float totalDf = stats.defensePercent    + stats.bonusDefensePercent;
        float totalCr = stats.critChancePercent + stats.bonusCritChancePercent;

        // Labels plain, values colored
        string dmg = $"Damage: {ColorWrap(F(totalD), damageColor)}";
        string hp  = $"Health: {ColorWrap(F(totalHP), healthColor)}";
        string st  = $"Stamina: {ColorWrap(F(totalSt), staminaColor)}";
        string df  = $"Defense: {ColorWrap(F(totalDf) + "%", defenseColor)}";
        string cr  = $"Crit Chance: {ColorWrap(F(totalCr) + "%", critColor)}";

        text.text = string.Join(lineSeparator, new[] { dmg, hp, st, df, cr });
    }
}
