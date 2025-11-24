using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI;

public class GameOverManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;
    public Text finalScoreText;

    [Header("Configuración")]
    public bool useTimeScalePause = false;

    private bool isGameOver = false;
    private static bool gameWasPaused = false;

    // Variables para almacenar estados de pausa
    private GameObject[] npcs;
    private bool[] npcScriptStates;
    private bool[] navMeshAgentStates;

    void Start()
    {
        // ASEGURAR que el tiempo esté corriendo al inicio
        EnsureTimeIsRunning();

        // Asegurarse de que las referencias estén asignadas
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
            
            if (finalScoreText == null)
            {
                finalScoreText = gameOverPanel.GetComponentInChildren<Text>();
            }
        }

        ConfigureButtons();
        Debug.Log("GameOverManager inicializado - TimeScale: " + Time.timeScale);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Escena cargada: {scene.name}. Asegurando TimeScale = 1");
        EnsureTimeIsRunning();
        isGameOver = false;
        gameWasPaused = false;
    }

    void EnsureTimeIsRunning()
    {
        if (Time.timeScale != 1f)
        {
            Debug.LogWarning($" TimeScale estaba en {Time.timeScale}. Corrigiendo a 1");
            Time.timeScale = 1f;
        }
    }

    void ConfigureButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            restartButton.interactable = true;
        }
        else
        {
            Debug.LogError("RestartButton no asignado!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            quitButton.interactable = true;
        }
        else
        {
            Debug.LogError("QuitButton no asignado!");
        }
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Tecla R presionada - Reiniciando juego");
                RestartGame();
            }
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("Tecla Q/Escape presionada - Saliendo del juego");
                QuitGame();
            }
        }
    }

    public void ShowGameOver()
    {
        isGameOver = true;
        
        // Actualizar texto de puntuación final
        if (finalScoreText != null && GameManager.Instance != null)
        {
            finalScoreText.text = $"PUNTUACIÓN FINAL: {GameManager.Instance.CurrentScore}";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log(" GameOverPanel activado");
        }

        if (useTimeScalePause)
        {
            gameWasPaused = true;
            Time.timeScale = 0f;
            Debug.Log("Juego pausado con Time.timeScale = 0");
        }
        else
        {
            PauseGameElements();
            Debug.Log(" Juego pausado manualmente");
        }

        StartCoroutine(DelayedButtonConfiguration());
    }

    IEnumerator DelayedButtonConfiguration()
    {
        yield return null;
        ConfigureButtons();
        
        if (restartButton != null)
        {
            restartButton.Select();
        }
    }

    void PauseGameElements()
    {
        Debug.Log(" Pausando elementos del juego...");

        
        // 1. Encontrar todos los NPCs por tag
        npcs = GameObject.FindGameObjectsWithTag("NPC");
        Debug.Log($"Encontrados {npcs.Length} NPCs para pausar");

        // Inicializar arrays para almacenar estados
        npcScriptStates = new bool[npcs.Length];
        navMeshAgentStates = new bool[npcs.Length];

        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] != null)
            {
                // Deshabilitar scripts del NPC
                MonoBehaviour[] npcScripts = npcs[i].GetComponents<MonoBehaviour>();
                npcScriptStates[i] = npcScripts.Length > 0;
                
                foreach (MonoBehaviour script in npcScripts)
                {
                    if (script != null && script != this && script.enabled)
                    {
                        script.enabled = false;
                    }
                }

                // Pausar NavMeshAgent
                NavMeshAgent agent = npcs[i].GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    navMeshAgentStates[i] = agent.isStopped;
                    agent.isStopped = true;
                }
            }
        }

        // 2. Pausar jugador
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

        // 3. Pausar cualquier otro objeto que pueda moverse
        Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null && !rb.CompareTag("Player")) // No pausar al jugador físicamente
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        Debug.Log(" Todos los elementos del juego pausados manualmente");
    }

    void ResumeGameElements()
    {
        Debug.Log(" Reanudando elementos del juego...");


        // 1. Reanudar NPCs
        if (npcs != null)
        {
            for (int i = 0; i < npcs.Length; i++)
            {
                if (npcs[i] != null)
                {
                    // Rehabilitar scripts del NPC
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

                    // Reanudar NavMeshAgent
                    NavMeshAgent agent = npcs[i].GetComponent<NavMeshAgent>();
                    if (agent != null && i < navMeshAgentStates.Length)
                    {
                        agent.isStopped = navMeshAgentStates[i];
                    }
                }
            }
        }

        // 2. Reanudar jugador
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

        Debug.Log(" Todos los elementos del juego reanudados");
    }

    public void RestartGame()
    {
        Debug.Log(" INICIANDO REINICIO DEL JUEGO");
        
        EnsureTimeIsRunning();
        isGameOver = false;
        gameWasPaused = false;

        if (!useTimeScalePause)
        {
            ResumeGameElements();
        }

        MapGenerator mapGen = FindObjectOfType<MapGenerator>();
        if (mapGen != null)
        {
            mapGen.CompleteGameReset();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }

        Debug.Log(" Cargando escena...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log(" Saliendo del juego");
        
        EnsureTimeIsRunning();
        isGameOver = false;
        gameWasPaused = false;

        PlayerPrefs.DeleteKey("EnemyDifficulty");
        PlayerPrefs.Save();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}