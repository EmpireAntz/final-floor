using UnityEngine;

public class EquipmentStatsApplier : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;
    public PlayerStats playerStats;

    [Header("Behavior")]
    [Tooltip("Preserve current health % when Max Health changes.")]
    public bool keepHealthRatioOnMaxChange = true;
    [Tooltip("Preserve current stamina % when Max Stamina changes.")]
    public bool keepStaminaRatioOnMaxChange = true;

    // ---- Base snapshots (what you set on PlayerStats in the inspector) ----
    [SerializeField] float baseDamage;
    [SerializeField] float baseMaxHealth;
    [SerializeField] float baseMaxStamina;
    [SerializeField] float baseDefensePct;
    [SerializeField] float baseCritPct;

    // ---- Expose to UI (read-only) ----
    public float BaseDamage              => baseDamage;
    public float BaseMaxHealth           => baseMaxHealth;
    public float BaseMaxStamina          => baseMaxStamina;
    public float BaseDefensePercent      => baseDefensePct;
    public float BaseCritChancePercent   => baseCritPct;

    public float LastBonusDamage         { get; private set; }
    public float LastBonusMaxHealth      { get; private set; }
    public float LastBonusMaxStamina     { get; private set; }
    public float LastBonusDefensePercent { get; private set; }
    public float LastBonusCritPercent    { get; private set; }

    public System.Action OnRecalculated;

    void Awake()
    {
        if (!inventory)   inventory   = FindObjectOfType<Inventory>();
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats)
        {
            baseDamage      = playerStats.damage;
            baseMaxHealth   = playerStats.maxHealth;
            baseMaxStamina  = playerStats.maxStamina;
            baseDefensePct  = playerStats.defensePercent;     // make sure these exist on PlayerStats
            baseCritPct     = playerStats.critChancePercent;  // ^
        }
    }

    void OnEnable()
    {
        if (inventory) inventory.OnChanged += Recalculate;
        Recalculate();
    }
    void OnDisable()
    {
        if (inventory) inventory.OnChanged -= Recalculate;
    }

    public void Recalculate()
    {
        if (!inventory || !playerStats) return;

        // ---- Sum bonuses from EQUIPPED items (use instance values) ----
        float bDmg = 0f, bHP = 0f, bSt = 0f, bDef = 0f, bCrit = 0f;

        for (int i = 0; i < inventory.equipment.Count; i++)
        {
            var it = inventory.equipment[i];
            if (Inventory.IsEmpty(it)) continue;

            it.EnsureRolled(); // ensure this instance has its rolled values

            bDmg += it.addDamage;
            bHP  += it.addMaxHealth;
            bSt  += it.addMaxStamina;
            bDef += it.addDefensePercent;
            bCrit+= it.addCritChancePercent;
        }

        // ---- Apply to PlayerStats ----
        float prevMaxHP  = playerStats.maxHealth;
        float prevMaxSt  = playerStats.maxStamina;

        playerStats.damage            = baseDamage     + bDmg;
        playerStats.maxHealth         = baseMaxHealth  + bHP;
        playerStats.maxStamina        = baseMaxStamina + bSt;
        playerStats.defensePercent    = baseDefensePct + bDef;
        playerStats.critChancePercent = baseCritPct    + bCrit;

        // preserve ratios if requested
        if (keepHealthRatioOnMaxChange && prevMaxHP > 0f)
        {
            float r = Mathf.Clamp01(playerStats.health / prevMaxHP);
            playerStats.health = Mathf.Min(playerStats.maxHealth, playerStats.maxHealth * r);
        }
        else playerStats.health = Mathf.Min(playerStats.health, playerStats.maxHealth);

        if (keepStaminaRatioOnMaxChange && prevMaxSt > 0f)
        {
            float r = Mathf.Clamp01(playerStats.stamina / prevMaxSt);
            playerStats.stamina = Mathf.Min(playerStats.maxStamina, playerStats.maxStamina * r);
        }
        else playerStats.stamina = Mathf.Min(playerStats.stamina, playerStats.maxStamina);

        // store for UI
        LastBonusDamage         = bDmg;
        LastBonusMaxHealth      = bHP;
        LastBonusMaxStamina     = bSt;
        LastBonusDefensePercent = bDef;
        LastBonusCritPercent    = bCrit;

        OnRecalculated?.Invoke();
    }

    // If you edit PlayerStats base values at runtime and want to re-snapshot
    public void ResnapshotBaseFromPlayer()
    {
        if (!playerStats) return;
        baseDamage     = playerStats.damage;
        baseMaxHealth  = playerStats.maxHealth;
        baseMaxStamina = playerStats.maxStamina;
        baseDefensePct = playerStats.defensePercent;
        baseCritPct    = playerStats.critChancePercent;
        Recalculate();
    }
}
