using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Prefabs")]
    public GameObject uiCanvasPrefab;

    [Header("Game Settings")]
    public int initialAmmo = 30;
    public int maxAmmo = 99;

    // Variables persistentes
    private int currentScore = 0;
    private int currentAmmo = 0;
    private bool gameStarted = false;

    // Referencias UI (se reinician por escena)
    private Text scoreText;
    private Text ammoText;
    private GameObject gameOverPanel;
    private Text finalScoreText;

    // Propiedades públicas para acceso externo
    public int CurrentScore => currentScore;
    public int CurrentAmmo => currentAmmo;

    void Awake()
    {
        // Singleton pattern mejorado
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void InitializeGame()
    {
        currentAmmo = initialAmmo;
        gameStarted = true;
        
        // Suscribirse a eventos de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Crear UI inicial si no existe
        StartCoroutine(DelayedUIInitialization());
    }

    IEnumerator DelayedUIInitialization()
    {
        // Esperar un frame para que la escena se cargue completamente
        yield return null;
        
        if (GameObject.FindGameObjectWithTag("UICanvas") == null && uiCanvasPrefab != null)
        {
            Instantiate(uiCanvasPrefab);
        }
        
        // Buscar referencias UI con más intentos
        yield return StartCoroutine(FindUIReferencesWithRetry());
        UpdateUI();
    }

void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    Debug.Log($"Escena cargada: {scene.name}. Buscando referencias UI...");
    
    // ✅ FORZAR TimeScale a 1 en cada carga de escena
    if (Time.timeScale != 1f)
    {
        Debug.LogWarning($"Corrigiendo TimeScale de {Time.timeScale} a 1");
        Time.timeScale = 1f;
    }
    
    // Reiniciar munición pero mantener puntuación
    currentAmmo = initialAmmo;
    
    // Buscar referencias UI en la nueva escena
    StartCoroutine(DelayedUISetup());
}

void OnApplicationFocus(bool hasFocus)
{
    // Asegurar que el tiempo sea correcto cuando la aplicación recupera el foco
    if (hasFocus && Time.timeScale == 0f)
    {
        Debug.LogWarning("Aplicación recuperó foco con TimeScale=0. Corrigiendo...");
        Time.timeScale = 1f;
    }
}

    IEnumerator DelayedUISetup()
    {
        // Esperar que la escena esté completamente cargada
        yield return new WaitForSeconds(0.1f);
        
        // Buscar referencias UI con múltiples intentos
        yield return StartCoroutine(FindUIReferencesWithRetry());
        UpdateUI();
    }

    IEnumerator FindUIReferencesWithRetry()
    {
        int maxAttempts = 5;
        int currentAttempt = 0;
        
        while (currentAttempt < maxAttempts)
        {
            FindUIReferences();
            
            // Verificar si encontramos todas las referencias críticas
            if (scoreText != null && ammoText != null && gameOverPanel != null)
            {
                Debug.Log($" Todas las referencias UI encontradas en intento {currentAttempt + 1}");
                break;
            }
            
            currentAttempt++;
            if (currentAttempt < maxAttempts)
            {
                Debug.Log($" Intento {currentAttempt} fallado. Reintentando en 0.1 segundos...");
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Log final del estado
        Debug.Log($"Estado final UI - ScoreText: {scoreText != null}, AmmoText: {ammoText != null}, GameOverPanel: {gameOverPanel != null}");
    }

    void FindUIReferences()
    {
        // Buscar canvas de UI
        GameObject uiCanvas = GameObject.FindGameObjectWithTag("UICanvas");
        if (uiCanvas == null)
        {
            // Buscar por nombre si no encuentra por tag
            uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null)
            {
                Debug.LogWarning("No se encontró UICanvas en la escena.");
                if (uiCanvasPrefab != null)
                {
                    uiCanvas = Instantiate(uiCanvasPrefab);
                    uiCanvas.name = "UICanvas";
                    Debug.Log("UICanvas creado desde prefab");
                }
                else
                {
                    Debug.LogError("No hay uiCanvasPrefab asignado!");
                    return;
                }
            }
        }

        // Buscar componentes dentro del canvas - MÚLTIPLES MÉTODOS
        FindGameOverPanel(uiCanvas);
        FindScoreAndAmmoText(uiCanvas);

        // Si aún no encontramos el GameOverPanel, buscar recursivamente
        if (gameOverPanel == null)
        {
            gameOverPanel = FindObjectRecursive(uiCanvas.transform, "GameOverPanel");
            if (gameOverPanel != null)
            {
                Debug.Log("GameOverPanel encontrado recursivamente");
            }
        }

        // Si aún no encontramos, buscar por componente
        if (gameOverPanel == null)
        {
            GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
            if (gameOverManager != null && gameOverManager.gameOverPanel != null)
            {
                gameOverPanel = gameOverManager.gameOverPanel;
                Debug.Log("GameOverPanel encontrado a través de GameOverManager");
            }
        }
    }

    void FindGameOverPanel(GameObject uiCanvas)
    {
        // Método 1: Buscar por tag
        gameOverPanel = GameObject.FindGameObjectWithTag("GameOverPanel");
        
        // Método 2: Buscar por nombre si el tag falla
        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
        }
        
        // Método 3: Buscar en hijos del canvas
        if (gameOverPanel == null && uiCanvas != null)
        {
            Transform panelTransform = uiCanvas.transform.Find("GameOverPanel");
            if (panelTransform != null)
            {
                gameOverPanel = panelTransform.gameObject;
            }
        }

        if (gameOverPanel != null)
        {
            // Buscar el texto de puntuación final dentro del panel
            finalScoreText = FindComponentInChildren<Text>(gameOverPanel, "FinalScoreText");
            if (finalScoreText == null)
            {
                // Buscar por nombre común
                Transform finalScoreTransform = gameOverPanel.transform.Find("FinalScoreText");
                if (finalScoreTransform != null)
                {
                    finalScoreText = finalScoreTransform.GetComponent<Text>();
                }
            }
        }
    }

    void FindScoreAndAmmoText(GameObject uiCanvas)
    {
        // Buscar ScoreText
        scoreText = GameObject.FindGameObjectWithTag("ScoreText")?.GetComponent<Text>();
        if (scoreText == null)
        {
            scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
        }
        if (scoreText == null && uiCanvas != null)
        {
            Transform scoreTransform = uiCanvas.transform.Find("ScoreText");
            if (scoreTransform != null)
            {
                scoreText = scoreTransform.GetComponent<Text>();
            }
        }

        // Buscar AmmoText
        ammoText = GameObject.FindGameObjectWithTag("AmmoText")?.GetComponent<Text>();
        if (ammoText == null)
        {
            ammoText = GameObject.Find("AmmoText")?.GetComponent<Text>();
        }
        if (ammoText == null && uiCanvas != null)
        {
            Transform ammoTransform = uiCanvas.transform.Find("AmmoText");
            if (ammoTransform != null)
            {
                ammoText = ammoTransform.GetComponent<Text>();
            }
        }
    }

    GameObject FindObjectRecursive(Transform parent, string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName)
                return child.gameObject;
            
            GameObject result = FindObjectRecursive(child, objectName);
            if (result != null)
                return result;
        }
        return null;
    }

    T FindComponentInChildren<T>(GameObject parent, string tag) where T : Component
    {
        if (parent == null) return null;
        
        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
                return child.GetComponent<T>();
        }
        return null;
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateUI();
        Debug.Log($"Puntos agregados: {points}. Puntuación total: {currentScore}");
    }

    public bool UseAmmo()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        UpdateUI();
        Debug.Log($"Munición agregada: {amount}. Total: {currentAmmo}");
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"PUNTOS: {currentScore}";
        else
            Debug.LogWarning("ScoreText es null en UpdateUI");
        
        if (ammoText != null)
            ammoText.text = $"BALAS: {currentAmmo}";
        else
            Debug.LogWarning("AmmoText es null en UpdateUI");

        if (finalScoreText != null && gameOverPanel != null && gameOverPanel.activeInHierarchy)
            finalScoreText.text = $"PUNTUACIÓN FINAL: {currentScore}";
    }

    public void GameOver()
    {
        Debug.Log("GameOver llamado - Buscando GameOverPanel...");
        
        // Último intento de encontrar el panel si aún es null
        if (gameOverPanel == null)
        {
            Debug.Log("GameOverPanel es null, buscando de nuevo...");
            FindUIReferences();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            UpdateUI();
            Debug.Log(" GameOverPanel mostrado correctamente");
        }
        else
        {
            Debug.LogError(" GameOverPanel NO encontrado después de múltiples intentos");
            
            // Fallback: usar GameOverManager directamente
            GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
            if (gameOverManager != null)
            {
                Debug.Log(" Usando GameOverManager como fallback");
                gameOverManager.ShowGameOver();
            }
            else
            {
                Debug.LogError(" GameOverManager tampoco encontrado!");
            }
        }
    }

    public void RestartGame()
    {
        // Mantener la puntuación entre reinicios si lo deseas
        // currentScore = 0; // Descomenta si quieres reiniciar puntuación
        
        currentAmmo = initialAmmo;
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        UpdateUI();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(nextSceneIndex);
    }

    void OnDestroy()
    {
        // Desuscribirse de eventos
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    
    
}