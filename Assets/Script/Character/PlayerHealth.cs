using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Vidas")]
    public int maxLives = 3;
    public int currentLives;

    [Header("Daño e Inmunidad")]
    public float invulnerabilityTime = 1.5f; // segundos de invulnerabilidad
    private bool isInvulnerable = false;
    private bool isDead = false;

    private GameOverManager gameOverManager;
    private CharacterController controller;
    private Renderer[] renderers;

    void Awake()
    {
        currentLives = maxLives;
        controller = GetComponent<CharacterController>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Start()
    {
        gameOverManager = FindObjectOfType<GameOverManager>();
        if (gameOverManager == null)
            Debug.LogWarning("No se encontró GameOverManager en la escena.");
    }

    public void TakeDamage(int amount = 1)
    {
        if (isDead || isInvulnerable) return;

        currentLives -= amount;
        Debug.Log($"Jugador perdió una vida. Vidas restantes: {currentLives}");

        if (currentLives <= 0)
        {
            currentLives = 0;
            Die();
        }
        else
        {
            StartCoroutine(TemporaryInvulnerability());
        }
    }

    private System.Collections.IEnumerator TemporaryInvulnerability()
    {
        isInvulnerable = true;

        // Parpadeo visual durante la inmunidad
        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            SetRenderersEnabled(false);
            yield return new WaitForSeconds(0.1f);
            SetRenderersEnabled(true);
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }

        isInvulnerable = false;
    }

    private void SetRenderersEnabled(bool value)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = value;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Jugador ha muerto.");

        // Desactivar control de movimiento
        if (controller != null)
            controller.enabled = false;

        // Liberar el mouse y pausar el juego
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;

        if (gameOverManager != null)
            gameOverManager.ShowGameOver();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
