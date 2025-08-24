using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;   // starts at max in Awake

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float stamina = 100f;  // starts at max in Awake
    [Tooltip("How fast stamina drains while sprinting (per second).")]
    public float staminaDrainPerSecond = 20f;
    [Tooltip("How fast stamina regenerates while not sprinting (per second).")]
    public float staminaRegenPerSecond = 15f;
    [Tooltip("Delay (seconds) after you stop sprinting before regen begins.")]
    public float staminaRegenDelay = 0.6f;
    [Tooltip("You must have at least this much stamina to (re)start sprinting.")]
    public float sprintMinStamina = 10f;
    [Tooltip("When stamina reaches 0, you must wait this many seconds before sprinting again.")]
    public float exhaustionCooldown = 2.0f;    

    [Header("Offense")]
    public float damage = 10f;

    float _lastStaminaSpendTime;
    float _exhaustedUntil = -1f;    

    public bool IsExhausted => Time.time < _exhaustedUntil; 
    public float ExhaustionRemaining =>
        Mathf.Max(0f, _exhaustedUntil - Time.time);

    void Awake()
    {
        health = Mathf.Clamp(health <= 0 ? maxHealth : health, 0, maxHealth);
        stamina = Mathf.Clamp(stamina <= 0 ? maxStamina : stamina, 0, maxStamina);
        _lastStaminaSpendTime = -999f;
        _exhaustedUntil = -1f;  
    }


    public bool CanSprint()
    {
      // Block sprint during cooldown, and require minimum stamina to (re)start
        return !IsExhausted && stamina >= sprintMinStamina;
    }

    public void TickStamina(bool sprinting, float dt)
    {
        if (sprinting)
        {
            SpendStamina(staminaDrainPerSecond * dt);
        }
        else if (Time.time >= _lastStaminaSpendTime + staminaRegenDelay)
        {
            GainStamina(staminaRegenPerSecond * dt);
        }
    }

    public void SpendStamina(float amount)
    {
        float prev = stamina;                                 
        stamina = Mathf.Max(0f, stamina - Mathf.Max(0f, amount));
        _lastStaminaSpendTime = Time.time;

        // Start cooldown the moment we hit 0 from a positive value
        if (prev > 0f && stamina <= 0f)                      
            _exhaustedUntil = Time.time + exhaustionCooldown; 
    }

    public void GainStamina(float amount)
    {
        stamina = Mathf.Min(maxStamina, stamina + Mathf.Max(0f, amount));
    }

    // Health helpers for future use
    public void TakeDamage(float amount)  => health  = Mathf.Max(0f, health  - Mathf.Max(0f, amount));
    public void Heal(float amount)        => health  = Mathf.Min(maxHealth, health + Mathf.Max(0f, amount));
}
