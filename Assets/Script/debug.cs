using UnityEngine;
using UnityEngine.UI;

public class MainMenuStuckDiagnostic : MonoBehaviour
{
    public Button startButton;
    public GameManager gameManager;
    
    void Start()
    {
        Debug.Log("=== DIAGNÓSTICO MENÚ ATORADO ===");
        
        // Verificar después de que todo se inicialice
        Invoke("RunDiagnostic", 1f);
    }
    
    void RunDiagnostic()
    {
        Debug.Log("1. Verificando GameManager...");
        CheckGameManager();
        
        Debug.Log("2. Verificando botón Iniciar...");
        CheckStartButton();
        
        Debug.Log("3. Verificando eventos del botón...");
        CheckButtonEvents();
        
        Debug.Log("4. Verificando estado del juego...");
        CheckGameState();
    }
    
    void CheckGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }
        
        if (gameManager == null)
        {
            Debug.LogError("❌ GameManager.Instance es NULL");
            
            // Intentar encontrar GameManager en la escena
            GameManager[] managers = FindObjectsOfType<GameManager>();
            Debug.Log($"GameManagers en escena: {managers.Length}");
            
            if (managers.Length > 0)
            {
                gameManager = managers[0];
                Debug.Log("✅ GameManager encontrado manualmente");
            }
            return;
        }
        
        Debug.Log("✅ GameManager encontrado");
        Debug.Log($"- Estado actual: {gameManager.currentGameState}");
        Debug.Log($"- MapGenerator asignado: {gameManager.mapGenerator != null}");
    }
    
    void CheckStartButton()
    {
        if (startButton == null)
        {
            Debug.LogError("❌ StartButton no asignado");
            
            // Buscar automáticamente
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
            if (startButton == null)
            {
                Debug.LogError("❌ No se pudo encontrar StartButton en la escena");
                return;
            }
            Debug.Log("✅ StartButton encontrado automáticamente");
        }
        
        Debug.Log($"- Botón interactuable: {startButton.interactable}");
        Debug.Log($"- Botón activo: {startButton.gameObject.activeInHierarchy}");
    }
    
    void CheckButtonEvents()
    {
        if (startButton != null)
        {
            int listenerCount = startButton.onClick.GetPersistentEventCount();
            Debug.Log($"- Número de listeners: {listenerCount}");
            
            if (listenerCount == 0)
            {
                Debug.LogError("❌ El botón no tiene listeners configurados!");
                Debug.Log("💡 SOLUCIÓN: Configura el evento OnClick en el Inspector:");
                Debug.Log("   - Arrastra el GameManager al campo Object");
                Debug.Log("   - Selecciona: GameManager → StartGame()");
            }
            else
            {
                for (int i = 0; i < listenerCount; i++)
                {
                    Object target = startButton.onClick.GetPersistentTarget(i);
                    string method = startButton.onClick.GetPersistentMethodName(i);
                    Debug.Log($"- Listener {i}: {target?.name} -> {method}");
                }
            }
        }
    }
    
    void CheckGameState()
    {
        if (gameManager != null)
        {
            Debug.Log($"- Estado del juego: {gameManager.currentGameState}");
            Debug.Log($"- Time.timeScale: {Time.timeScale}");
            
            if (gameManager.currentGameState == GameState.MainMenu)
            {
                Debug.Log("ℹ️  El juego está correctamente en estado MainMenu");
            }
        }
    }
    
    void Update()
    {
        // Atajos de teclado para testing
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RunDiagnostic();
        }
        
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ForceStartFromKeyboard();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestButtonClick();
        }
    }
    
    void ForceStartFromKeyboard()
    {
        Debug.Log("=== FORZANDO INICIO DESDE TECLADO ===");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
            Debug.Log("✅ Juego forzado a iniciar desde teclado");
        }
        else
        {
            Debug.LogError("❌ No se puede forzar inicio: GameManager.Instance es null");
        }
    }
    
    void TestButtonClick()
    {
        Debug.Log("=== SIMULANDO CLIC EN BOTÓN ===");
        
        if (startButton != null)
        {
            // Simular clic en el botón
            startButton.onClick?.Invoke();
            Debug.Log("✅ Clic simulado en StartButton");
        }
        else
        {
            Debug.LogError("❌ No se puede simular clic: startButton es null");
        }
    }
    
    // Método público para forzar inicio desde UI
    public void ForceStartGame()
    {
        Debug.Log("=== FORZANDO INICIO DESDE UI ===");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null en ForceStartGame");
        }
    }
}