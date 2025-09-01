using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;   // base
    public float health = 100f;      // current

    [Header("Stamina")]
    public float maxStamina = 100f;  // base
    public float stamina = 100f;     // current
    public float staminaDrainPerSecond = 20f;
    public float staminaRegenPerSecond = 15f;
    public float staminaRegenDelay = 0.6f;
    public float sprintMinStamina = 10f;
    public float exhaustionCooldown = 2.0f;

    [Header("Offense")]
    public float damage = 10f;       // base damage
    [Range(0f, 100f)] public float critChancePercent = 0f;
    
    [Header("Defense")]
    [Range(0f,100f)] public float defensePercent = 0f;

    // ---- Runtime bonuses (from equipment, buffs, etc.) ----
    public float bonusMaxHealth = 0f;
    public float bonusDamage = 0f;
    public float bonusDefensePercent = 0f;
    public float bonusCritChancePercent = 0f;

    // Event so UI (and others) can refresh
    public System.Action OnStatsChanged;

    float _lastStaminaSpendTime;
    float _exhaustedUntil = -1f;

    public bool IsExhausted => Time.time < _exhaustedUntil;
    public float ExhaustionRemaining => Mathf.Max(0f, _exhaustedUntil - Time.time);

    void Awake()
    {
        health  = Mathf.Clamp(health  <= 0 ? maxHealth  : health,  0, TotalMaxHealth);
        stamina = Mathf.Clamp(stamina <= 0 ? maxStamina : stamina, 0, maxStamina);
        _lastStaminaSpendTime = -999f;
        _exhaustedUntil = -1f;
    }

    // --------------- Totals (base + bonus) ---------------
    public float TotalDamage        => Mathf.Max(0f, damage + bonusDamage);
    public float TotalMaxHealth     => Mathf.Max(1f, maxHealth + bonusMaxHealth);
    public float TotalDefensePct    => Mathf.Clamp(defensePercent + bonusDefensePercent, 0f, 100f);
    public float TotalCritChancePct => Mathf.Clamp(critChancePercent + bonusCritChancePercent, 0f, 100f);

    // If you change bonuses at runtime, call these (or set then CallChanged())
    public void SetBonuses(float addHP, float addDMG, float addDEFpct, float addCRITpct)
    {
        bonusMaxHealth        = addHP;
        bonusDamage           = addDMG;
        bonusDefensePercent   = addDEFpct;
        bonusCritChancePercent= addCRITpct;
        // keep current health in range if max changed
        health = Mathf.Min(health, TotalMaxHealth);
        CallChanged();
    }
    public void CallChanged() => OnStatsChanged?.Invoke();

    // -------- Stamina flow --------
    public bool CanSprint() => !IsExhausted && stamina >= sprintMinStamina;

    public void TickStamina(bool sprinting, float dt)
    {
        if (sprinting) SpendStamina(staminaDrainPerSecond * dt);
        else if (Time.time >= _lastStaminaSpendTime + staminaRegenDelay)
            GainStamina(staminaRegenPerSecond * dt);
    }

    public void SpendStamina(float amount)
    {
        float prev = stamina;
        stamina = Mathf.Max(0f, stamina - Mathf.Max(0f, amount));
        _lastStaminaSpendTime = Time.time;
        if (prev > 0f && stamina <= 0f)
            _exhaustedUntil = Time.time + exhaustionCooldown;
    }
    public void GainStamina(float amount)
    {
        stamina = Mathf.Min(maxStamina, stamina + Mathf.Max(0f, amount));
    }

    // -------- Health helpers --------
    public void TakeDamage(float amount)
    {
    
         float reduction = Mathf.Clamp01(TotalDefensePct / 100f);
         amount *= (1f - reduction);

        health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
        CallChanged();
    }
    public void Heal(float amount)
    {
        health = Mathf.Min(TotalMaxHealth, health + Mathf.Max(0f, amount));
        CallChanged();
    }
}
