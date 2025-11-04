using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estados del Juego")]
    public GameState currentGameState = GameState.MainMenu;

    [Header("Referencias UI")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject hudPanel;

    [Header("Referencias del Juego")]
    public MapGenerator mapGenerator;

    [Header("Configuración")]
    public bool debugMode = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (debugMode) Debug.Log("GameManager inicializado");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Buscar referencias automáticamente
        FindReferences();
    }

    private void Start()
    {
        // Verificar configuración
        if (!VerifyGameSetup())
        {
            Debug.LogError("Configuración del juego incompleta en Start!");
            return;
        }

        ShowMainMenu();
    }

    private void Update()
    {
        // Atajos de teclado para testing
        if (Input.GetKeyDown(KeyCode.Return) && currentGameState == GameState.MainMenu)
        {
            StartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentGameState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentGameState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    void FindReferences()
    {
        // Buscar MapGenerator si no está asignado
        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator != null && debugMode)
                Debug.Log("MapGenerator encontrado automáticamente");
        }

        // Buscar paneles UI
        FindUIPanels();
    }

    void FindUIPanels()
    {
        if (mainMenuPanel == null)
        {
            mainMenuPanel = GameObject.Find("MainMenuPanel");
            if (mainMenuPanel != null && debugMode) Debug.Log("MainMenuPanel encontrado automáticamente");
        }

        if (pauseMenuPanel == null)
        {
            pauseMenuPanel = GameObject.Find("PauseMenuPanel");
            if (pauseMenuPanel != null && debugMode) Debug.Log("PauseMenuPanel encontrado automáticamente");
        }

        if (hudPanel == null)
        {
            hudPanel = GameObject.Find("HUDPanel");
            if (hudPanel != null && debugMode) Debug.Log("HUDPanel encontrado automáticamente");
        }
    }

    bool VerifyGameSetup()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator no encontrado!");
            return false;
        }

        if (mapGenerator.playerPrefab == null)
        {
            Debug.LogError("PlayerPrefab no asignado en MapGenerator!");
            return false;
        }

        return true;
    }

    public void StartGame()
    {
        if (debugMode) Debug.Log("=== INICIANDO JUEGO ===");

        // Verificaciones de seguridad
        if (mapGenerator == null)
        {
            Debug.LogError("No se puede iniciar: MapGenerator es null");
            return;
        }

        if (mapGenerator.playerPrefab == null)
        {
            Debug.LogError("No se puede iniciar: PlayerPrefab es null");
            return;
        }

        currentGameState = GameState.Playing;
        Time.timeScale = 1f;

        // Generar mapa si no está listo
        if (!mapGenerator.IsMapReady())
        {
            if (debugMode) Debug.Log("Generando mapa...");
            mapGenerator.RegenerateMap();
        }
        else
        {
            if (debugMode) Debug.Log("Mapa ya está generado");
        }

        // Configurar UI
        SetUIVisibility(false, false, true);

        // Configurar cámara
        SetupCamera();

        // Actualizar HUD
        ForceHUDUpdate();

        if (debugMode) Debug.Log("✅ JUEGO INICIADO CORRECTAMENTE");
    }

    void SetupCamera()
    {
        ThirdPersonCamera tpc = FindObjectOfType<ThirdPersonCamera>();
        if (tpc != null)
        {
            GameObject player = mapGenerator.GetPlayer();
            if (player != null)
            {
                tpc.player = player.transform;
                if (debugMode) Debug.Log("Cámara configurada con jugador");
            }
            else
            {
                Debug.LogError("No se pudo obtener jugador para la cámara");
            }
        }
        else
        {
            Debug.LogError("ThirdPersonCamera no encontrada");
        }
    }

    void ForceHUDUpdate()
    {
        if (hudPanel != null)
        {
            HUDManager hud = hudPanel.GetComponent<HUDManager>();
            if (hud != null)
            {
                hud.ForceHUDUpdate();
                if (debugMode) Debug.Log("HUD actualizado");
            }
        }
    }

    public void PauseGame()
    {
        if (currentGameState != GameState.Playing) return;

        currentGameState = GameState.Paused;
        Time.timeScale = 0f;

        SetUIVisibility(false, true, false);

        if (debugMode) Debug.Log("Juego pausado");
    }

    public void ResumeGame()
    {
        if (currentGameState != GameState.Paused) return;

        currentGameState = GameState.Playing;
        Time.timeScale = 1f;

        SetUIVisibility(false, false, true);

        if (debugMode) Debug.Log("Juego reanudado");
    }

    public void ShowMainMenu()
    {
        currentGameState = GameState.MainMenu;
        Time.timeScale = 1f;

        SetUIVisibility(true, false, false);

        if (debugMode) Debug.Log("Mostrando menú principal");
    }

    void SetUIVisibility(bool showMainMenu, bool showPauseMenu, bool showHUD)
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(showMainMenu);
            if (debugMode && showMainMenu) Debug.Log("MainMenuPanel activado");
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(showPauseMenu);
            if (debugMode && showPauseMenu) Debug.Log("PauseMenuPanel activado");
        }

        if (hudPanel != null)
        {
            hudPanel.SetActive(showHUD);
            if (debugMode && showHUD) Debug.Log("HUDPanel activado");
        }
    }

    public void RestartGame()
    {
        if (debugMode) Debug.Log("Reiniciando juego...");

        if (mapGenerator != null)
        {
            mapGenerator.RegenerateMap();
        }

        ResumeGame();
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    // Añadir al final de la clase GameManager, antes del enum:

public void OnMapGenerationComplete()
{
    if (debugMode) Debug.Log("Mapa generado completamente");
    
    // Reconfigurar cámara después de que el mapa esté listo
    SetupCamera();
    
    // Forzar actualización del HUD
    ForceHUDUpdate();
}

// Método para verificar estado del juego
public bool IsGamePlaying()
{
    return currentGameState == GameState.Playing;
}

// Método para obtener referencia al jugador
public GameObject GetPlayer()
{
    if (mapGenerator != null)
    {
        return mapGenerator.GetPlayer();
    }
    return null;
}
}


public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}