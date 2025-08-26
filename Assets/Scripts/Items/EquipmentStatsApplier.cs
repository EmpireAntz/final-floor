using UnityEngine;

public class EquipmentStatsApplier : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;          // your existing Inventory
    public PlayerStats playerStats;       // your PlayerStats from the message

    [Header("Behavior")]
    [Tooltip("Preserve current health % when Max Health changes.")]
    public bool keepHealthRatioOnMaxChange = true;

    // Snapshot of base stats (what you set on PlayerStats in the Inspector)
    float baseDamage;
    float baseMaxHealth;

    void Awake()
    {
        if (!inventory)   inventory   = FindObjectOfType<Inventory>();
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null)
        {
            baseDamage   = playerStats.damage;
            baseMaxHealth = playerStats.maxHealth;
        }
    }

    void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += Recalculate;
        Recalculate(); // compute once on enable
    }

    void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Recalculate;
    }

    public void Recalculate()
    {
        if (!inventory || !playerStats) return;

        float bonusDmg = 0f;
        float bonusHP  = 0f;

        // Sum ONLY equipped items
        for (int i = 0; i < inventory.equipment.Count; i++)
        {
            var it = inventory.equipment[i];
            if (Inventory.IsEmpty(it)) continue;
            var d = it.data;
            bonusDmg += d.addDamage;
            bonusHP  += d.addMaxHealth;
        }

        // Apply to PlayerStats
        float prevMax = playerStats.maxHealth;

        playerStats.damage    = baseDamage   + bonusDmg;
        playerStats.maxHealth = baseMaxHealth + bonusHP;

        if (keepHealthRatioOnMaxChange && prevMax > 0f)
        {
            float ratio = Mathf.Clamp01(playerStats.health / prevMax);
            playerStats.health = Mathf.Min(playerStats.maxHealth, playerStats.maxHealth * ratio);
        }
        else
        {
            playerStats.health = Mathf.Min(playerStats.health, playerStats.maxHealth);
        }

        Debug.Log($"[EquipStats] +DMG {bonusDmg}, +HP {bonusHP}  →  Totals: DMG {playerStats.damage}, MaxHP {playerStats.maxHealth}");
    }

    // Optional: call this if you ever change PlayerStats base values at runtime
    public void ResnapshotBaseFromPlayer()
    {
        if (!playerStats) return;
        baseDamage    = playerStats.damage;
        baseMaxHealth = playerStats.maxHealth;
        Recalculate();
    }
}
