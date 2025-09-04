using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    public bool IsDead => currentHealth <= 0;

    void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int amount, Object source = null)
    {
        int dmg = Mathf.Max(0, amount);
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        var flash = GetComponent<EnemyFlashMulti>();
        if (flash) flash.Flash();


        Debug.Log($"[EnemyStats] {name} took {dmg} dmg from {(source ? source.name : "Unknown")} → HP {currentHealth}/{maxHealth}");

        if (currentHealth == 0)
        {
            Debug.Log($"[EnemyStats] {name} defeated.");
            // (optional) handle death later
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0, amount));
    }
}
