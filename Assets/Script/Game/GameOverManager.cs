using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI;

public class GameOverManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject gameOverPanel;
    public Text titleText;
    public Text finalScoreText;
    public Button restartButton;
    public Button quitButton;
    public Button resumeButton;

    [Header("Configuración")]
    public bool useTimeScalePause = false;
    public bool unlockCursorOnPause = true; 

    [Header("Textos Personalizables")]
    public string gameOverTitle = "GAME OVER";
    public string pauseTitle = "PAUSA";

    private bool isGameOver = false;
    private PauseManager pauseManager;
    private bool wasCursorLocked = false;
    private CursorLockMode previousCursorLockState;

    // Variables para almacenar estados de pausa manual
    private GameObject[] npcs;
    private bool[] npcScriptStates;
    private bool[] navMeshAgentStates;

    void Start()
    {
        // Buscar PauseManager para compatibilidad
        pauseManager = FindObjectOfType<PauseManager>();
        
        // Asegurar que el tiempo esté corriendo al inicio
        EnsureTimeIsRunning();

        // Inicializar estado del cursor para juego
        InitializeCursorForGameplay();

        // Inicializar UI
        InitializeUI();

        // Configurar botones
        ConfigureButtons();
        
        Debug.Log("GameOverManager inicializado");
    }

    void InitializeCursorForGameplay()
    {
        // Bloquear y ocultar cursor durante el juego
        if (unlockCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("Cursor bloqueado para gameplay");
        }
    }

    void InitializeUI()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.FindGameObjectWithTag("GameOverPanel");
            if (gameOverPanel == null)
            {
                gameOverPanel = GameObject.Find("GameOverPanel");
            }
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            
            if (titleText == null)
            {
                titleText = gameOverPanel.GetComponentInChildren<Text>();
            }
            
            if (finalScoreText == null)
            {
                finalScoreText = gameOverPanel.GetComponentInChildren<Text>();
            }
        }
        else
        {
            Debug.LogError("GameOverPanel no encontrado en GameOverManager!");
        }
    }

    void ConfigureButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            restartButton.interactable = true;
            
            Text restartText = restartButton.GetComponentInChildren<Text>();
            if (restartText != null) restartText.text = "REINICIAR";
        }
        else
        {
            Debug.LogError("RestartButton no asignado en GameOverManager!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            quitButton.interactable = true;
        }
        else
        {
            Debug.LogError("QuitButton no asignado en GameOverManager!");
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeFromPause);
            resumeButton.interactable = true;
            
            Text resumeText = resumeButton.GetComponentInChildren<Text>();
            if (resumeText != null) resumeText.text = "REANUDAR";
        }
    }

    void Update()
    {
        // Input manual para game over (testing)
        if (Input.GetKeyDown(KeyCode.G) && !isGameOver && !IsGamePaused())
        {
            Debug.Log("Tecla G presionada - Activando Game Over (TEST)");
            ShowGameOver();
        }

        // Forzar desbloqueo del cursor si el panel está activo y el cursor debería estar desbloqueado
        if (gameOverPanel != null && gameOverPanel.activeInHierarchy && unlockCursorOnPause)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                UnlockCursor();
            }
        }
    }

    // MÉTODO PARA DESBLOQUEAR CURSOR
    void UnlockCursor()
    {
        wasCursorLocked = (Cursor.lockState == CursorLockMode.Locked);
        previousCursorLockState = Cursor.lockState;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Cursor desbloqueado para interactuar con UI");
    }

    // MÉTODO PARA REBLOQUEAR CURSOR
    void RelockCursor()
    {
        if (wasCursorLocked)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = false;
            Debug.Log("Cursor rebloqueado para gameplay");
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ShowPauseMenu()
    {
        if (isGameOver)
        {
            Debug.LogWarning("No se puede mostrar menú de pausa durante Game Over");
            return;
        }

        if (gameOverPanel != null)
        {
            // DESBLOQUEAR CURSOR para pausa
            if (unlockCursorOnPause)
            {
                UnlockCursor();
            }

            // Configurar como menú de pausa
            if (titleText != null)
                titleText.text = pauseTitle;
            
            if (finalScoreText != null)
                finalScoreText.gameObject.SetActive(false);
            
            if (resumeButton != null)
                resumeButton.gameObject.SetActive(true);
            
            gameOverPanel.SetActive(true);
            Debug.Log("Menú de pausa activado - Cursor desbloqueado");
        }
    }

    public void HidePauseMenu()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            
            // REBLOQUEAR CURSOR al salir de pausa
            if (unlockCursorOnPause)
            {
                RelockCursor();
            }
            
            Debug.Log("Menú de pausa ocultado - Cursor rebloqueado");
        }
    }

    public void ShowGameOver()
    {
        if (IsGamePaused())
        {
            Debug.LogWarning("No se puede mostrar Game Over mientras el juego está pausado");
            return;
        }

        if (isGameOver)
        {
            Debug.LogWarning("Game Over ya está activo");
            return;
        }

        isGameOver = true;
        
        // DESBLOQUEAR CURSOR para game over
        if (unlockCursorOnPause)
        {
            UnlockCursor();
        }

        if (titleText != null)
            titleText.text = gameOverTitle;

        if (finalScoreText != null && GameManager.Instance != null)
        {
            finalScoreText.text = $"PUNTUACIÓN FINAL: {GameManager.Instance.CurrentScore}";
            finalScoreText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("FinalScoreText o GameManager.Instance es null");
        }

        if (resumeButton != null)
            resumeButton.gameObject.SetActive(false);
        
        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("GameOverPanel activado como Game Over - Cursor desbloqueado");
        }
        else
        {
            Debug.LogError("GameOverPanel es null en ShowGameOver!");
            return;
        }

        if (useTimeScalePause)
        {
            Time.timeScale = 0f;
            Debug.Log("Juego pausado con Time.timeScale = 0 (Game Over)");
        }
        else
        {
            PauseGameElements();
            Debug.Log("Juego pausado manualmente (Game Over)");
        }

        StartCoroutine(DelayedButtonConfiguration());
    }

    void ResumeFromPause()
    {
        if (pauseManager != null)
        {
            pauseManager.ResumeGame();
        }
        else
        {
            Debug.LogWarning("PauseManager no encontrado para reanudar");
            HidePauseMenu();
            EnsureTimeIsRunning();
        }
    }

    IEnumerator DelayedButtonConfiguration()
    {
        yield return null;
        
        ConfigureButtons();
        
        // Asegurar que los botones sean seleccionables
        if (restartButton != null && isGameOver)
        {
            restartButton.Select();
            Debug.Log("Botón Reiniciar seleccionado");
        }
        else if (resumeButton != null && !isGameOver)
        {
            resumeButton.Select();
            Debug.Log("Botón Reanudar seleccionado");
        }
    }

    void PauseGameElements()
    {
        Debug.Log("Pausando elementos del juego (Game Over)...");

        npcs = GameObject.FindGameObjectsWithTag("NPC");
        Debug.Log($"Encontrados {npcs.Length} NPCs para pausar");

        npcScriptStates = new bool[npcs.Length];
        navMeshAgentStates = new bool[npcs.Length];

        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] != null)
            {
                MonoBehaviour[] npcScripts = npcs[i].GetComponents<MonoBehaviour>();
                npcScriptStates[i] = npcScripts.Length > 0;
                
                foreach (MonoBehaviour script in npcScripts)
                {
                    if (script != null && script != this && script.enabled)
                    {
                        script.enabled = false;
                    }
                }

                NavMeshAgent agent = npcs[i].GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    navMeshAgentStates[i] = agent.isStopped;
                    agent.isStopped = true;
                }
            }
        }

        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null) 
        {
            playerMovement.enabled = false;
            Debug.Log("Jugador pausado");
        }

        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();
        if (playerShooting != null) 
        {
            playerShooting.enabled = false;
            Debug.Log("Disparos pausados");
        }

        Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null && !rb.CompareTag("Player"))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        Debug.Log(" Todos los elementos del juego pausados manualmente (Game Over)");
    }

    void ResumeGameElements()
    {
        Debug.Log("Reanudando elementos del juego (Game Over)...");

        if (npcs != null)
        {
            for (int i = 0; i < npcs.Length; i++)
            {
                if (npcs[i] != null)
                {
                    if (i < npcScriptStates.Length && npcScriptStates[i])
                    {
                        MonoBehaviour[] npcScripts = npcs[i].GetComponents<MonoBehaviour>();
                        foreach (MonoBehaviour script in npcScripts)
                        {
                            if (script != null && script != this)
                            {
                                script.enabled = true;
                            }
                        }
                    }

                    NavMeshAgent agent = npcs[i].GetComponent<NavMeshAgent>();
                    if (agent != null && i < navMeshAgentStates.Length)
                    {
                        agent.isStopped = navMeshAgentStates[i];
                    }
                }
            }
        }

        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null) 
        {
            playerMovement.enabled = true;
            Debug.Log("Jugador reanudado");
        }

        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();
        if (playerShooting != null) 
        {
            playerShooting.enabled = true;
            Debug.Log("Disparos reanudados");
        }

        Debug.Log("Todos los elementos del juego reanudados (Game Over)");
    }

    void EnsureTimeIsRunning()
    {
        if (Time.timeScale != 1f)
        {
            Debug.LogWarning($"TimeScale estaba en {Time.timeScale}. Corrigiendo a 1");
            Time.timeScale = 1f;
        }
    }

    public void RestartGame()
    {
        Debug.Log("INICIANDO REINICIO DEL JUEGO DESDE GAME OVER");
        
        EnsureTimeIsRunning();
        
        // REBLOQUEAR CURSOR al reiniciar
        if (unlockCursorOnPause)
        {
            RelockCursor();
        }
        
        isGameOver = false;

        if (!useTimeScalePause)
        {
            ResumeGameElements();
        }

        MapGenerator mapGen = FindObjectOfType<MapGenerator>();
        if (mapGen != null)
        {
            mapGen.CompleteGameReset();
        }
        else
        {
            Debug.LogWarning("MapGenerator no encontrado al reiniciar desde Game Over");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance es null al reiniciar desde Game Over");
        }

        Debug.Log("Cargando escena desde Game Over...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego desde Game Over...");
        
        EnsureTimeIsRunning();
        
        isGameOver = false;

        PlayerPrefs.DeleteKey("EnemyDifficulty");
        PlayerPrefs.Save();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    bool IsGamePaused()
    {
        if (pauseManager != null)
        {
            return pauseManager.IsGamePaused();
        }
        
        pauseManager = FindObjectOfType<PauseManager>();
        return pauseManager != null && pauseManager.IsGamePaused();
    }

    public void TriggerGameOver()
    {
        if (!isGameOver && !IsGamePaused())
        {
            ShowGameOver();
        }
        else if (IsGamePaused())
        {
            Debug.LogWarning(" No se puede activar Game Over: El juego está en pausa");
        }
        else
        {
            Debug.LogWarning("No se puede activar Game Over: Ya está activo");
        }
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    void OnDestroy()
    {
        if (isGameOver)
        {
            EnsureTimeIsRunning();
        }
    }
}