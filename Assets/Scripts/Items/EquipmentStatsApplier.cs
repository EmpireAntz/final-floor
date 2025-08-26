using UnityEngine;

public class EquipmentStatsApplier : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;
    public PlayerStats playerStats;

    [Header("Behavior")]
    [Tooltip("Preserve current health % when Max Health changes.")]
    public bool keepHealthRatioOnMaxChange = true;

    // Snapshot of the player's base stats (what you set in the inspector)
    [SerializeField] float baseDamage;
    [SerializeField] float baseMaxHealth;

    // === Exposed to UI ===
    public float BaseDamage        => baseDamage;
    public float BaseMaxHealth     => baseMaxHealth;
    public float LastBonusDamage   { get; private set; }
    public float LastBonusMaxHealth{ get; private set; }
    public System.Action OnRecalculated;   // UI can subscribe

    void Awake()
    {
        if (!inventory)   inventory   = FindObjectOfType<Inventory>();
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null)
        {
            baseDamage    = playerStats.damage;
            baseMaxHealth = playerStats.maxHealth;
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

        float bonusDmg = 0f;
        float bonusHP  = 0f;

        // Sum equipped items’ bonuses
        for (int i = 0; i < inventory.equipment.Count; i++)
        {
            var it = inventory.equipment[i];
            if (Inventory.IsEmpty(it)) continue;
            var d = it.data;
            bonusDmg += d.addDamage;
            bonusHP  += d.addMaxHealth;
        }

        float prevMax = playerStats.maxHealth;

        playerStats.damage    = baseDamage    + bonusDmg;
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

        // store for UI and notify
        LastBonusDamage    = bonusDmg;
        LastBonusMaxHealth = bonusHP;
        OnRecalculated?.Invoke();

        // Debug log (optional)
        // Debug.Log($"[EquipStats] +DMG {bonusDmg}, +HP {bonusHP} → DMG {playerStats.damage}, MaxHP {playerStats.maxHealth}");
    }

    // Call this if you change base values at runtime and want to resnapshot them
    public void ResnapshotBaseFromPlayer()
    {
        if (!playerStats) return;
        baseDamage    = playerStats.damage;
        baseMaxHealth = playerStats.maxHealth;
        Recalculate();
    }
}
