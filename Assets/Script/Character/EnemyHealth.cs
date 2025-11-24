using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Salud")]
    public int maxHealth = 3;
    public int scoreValue = 10;
    public GameObject deathEffect;

    private int currentHealth;
    private NavMeshAgent agent;
    private Animator animator;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // DEBUG: Mostrar información del enemigo
        Debug.Log($"ENEMIGO CREADO: {gameObject.name}, Salud: {currentHealth}, Capa: {LayerMask.LayerToName(gameObject.layer)}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) 
        {
            Debug.Log($"Intento de daño a {gameObject.name} pero YA ESTÁ MUERTO");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"ENEMIGO {gameObject.name} RECIBIÓ DAÑO: {damage}. Salud restante: {currentHealth}");

        if (animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log($"ENEMIGO {gameObject.name} MURIENDO");

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
            animator.SetBool("IsDead", true);

        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
            Debug.Log($"PUNTOS OTORGADOS: {scoreValue} por eliminar {gameObject.name}");
        }
        else
        {
            Debug.LogError("GameManager.Instance es NULL - no se pueden otorgar puntos");
        }

        Destroy(gameObject, 3f);
    }

    public bool IsDead()
    {
        return isDead;
    }
}