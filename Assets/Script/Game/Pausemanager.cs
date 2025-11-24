using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameOverManager gameOverManager;

    [Header("Configuración")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool pauseTimeScale = true;

    private bool isPaused = false;

    void Start()
    {
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<GameOverManager>();
        }
        
        Debug.Log("PauseManager inicializado");
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                if (gameOverManager == null || !gameOverManager.IsGameOver())
                {
                    PauseGame();
                }
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        Debug.Log("⏸️ Juego pausado");

        if (gameOverManager != null)
        {
            gameOverManager.ShowPauseMenu();
        }
        else
        {
            Debug.LogError("GameOverManager no encontrado!");
        }

        if (pauseTimeScale)
        {
            Time.timeScale = 0f;
        }
        else
        {
            PauseGameElements();
        }
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Debug.Log(" Juego reanudado");

        if (gameOverManager != null)
        {
            gameOverManager.HidePauseMenu();
        }

        if (pauseTimeScale)
        {
            Time.timeScale = 1f;
        }
        else
        {
            ResumeGameElements();
        }
    }

    void PauseGameElements()
    {
        // Tu implementación existente de pausa manual
    }

    void ResumeGameElements()
    {
        // Tu implementación existente de reanudar manual
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }

    void OnDestroy()
    {
        if (isPaused && pauseTimeScale)
        {
            Time.timeScale = 1f;
        }
    }
}