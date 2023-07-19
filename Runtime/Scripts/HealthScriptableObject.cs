using UnityEngine;

[CreateAssetMenu(menuName = "Health")]
public class HealthScriptableObject : ScriptableObject
{

    public int maxHealth = 10;
    public int currentHealth;

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void OnEnable()
    {
        // Reset health when loaded
        currentHealth = maxHealth;
    }

}