using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Botones de Pausa")]
    public Button resumeButton;
    public Button restartButton;
    public Button optionsButton;
    public Button mainMenuButton;
    
    [Header("Paneles de Pausa")]
    public GameObject pauseMainPanel;
    public GameObject pauseOptionsPanel;
    
    [Header("Opciones de Pausa")]
    public Slider pauseVolumeSlider;
    public Toggle pauseFullscreenToggle;

    [Header("Configuración")]
    public bool debugMode = true;

    private void Start()
    {
        if (debugMode) Debug.Log("PauseMenuManager iniciado");

        // Verificar referencias críticas
        if (!VerifyCriticalReferences())
        {
            Debug.LogError("Faltan referencias críticas en PauseMenuManager!");
            return;
        }

        // Configurar listeners de botones
        SetupButtonListeners();
        
        // Configurar opciones
        SetupPauseOptions();

        // Ocultar menú de pausa al inicio
        gameObject.SetActive(false);

        if (debugMode) Debug.Log("PauseMenuManager configurado correctamente");
    }

    bool VerifyCriticalReferences()
    {
        bool allCriticalReferencesFound = true;

        if (resumeButton == null)
        {
            Debug.LogError("ResumeButton no asignado en PauseMenuManager!");
            allCriticalReferencesFound = false;
        }

        if (restartButton == null)
        {
            Debug.LogError("RestartButton no asignado en PauseMenuManager!");
            allCriticalReferencesFound = false;
        }

        if (mainMenuButton == null)
        {
            Debug.LogError("MainMenuButton no asignado en PauseMenuManager!");
            allCriticalReferencesFound = false;
        }

        if (pauseMainPanel == null)
        {
            Debug.LogError("PauseMainPanel no asignado en PauseMenuManager!");
            allCriticalReferencesFound = false;
        }

        return allCriticalReferencesFound;
    }

    void SetupButtonListeners()
    {
        // Configurar botones críticos
        resumeButton.onClick.AddListener(ResumeGame);
        restartButton.onClick.AddListener(RestartGame);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Configurar botón opcional de opciones
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(ShowPauseOptions);
        }
        else if (debugMode)
        {
            Debug.Log("OptionsButton no asignado (opcional)");
        }
    }

    void SetupPauseOptions()
    {
        // Configurar volumen si existe
        if (pauseVolumeSlider != null)
        {
            pauseVolumeSlider.value = AudioListener.volume;
            pauseVolumeSlider.onValueChanged.AddListener(SetVolume);
        }

        // Configurar pantalla completa si existe
        if (pauseFullscreenToggle != null)
        {
            pauseFullscreenToggle.isOn = Screen.fullScreen;
            pauseFullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    void ResumeGame()
    {
        if (debugMode) Debug.Log("Reanudando juego desde PauseMenuManager");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null!");
            // Fallback: reanudar manualmente
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }
    }

    void RestartGame()
    {
        if (debugMode) Debug.Log("Reiniciando juego desde PauseMenuManager");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null!");
        }
    }

    void ShowPauseOptions()
    {
        if (debugMode) Debug.Log("Mostrando opciones de pausa");
        
        if (pauseMainPanel != null && pauseOptionsPanel != null)
        {
            pauseMainPanel.SetActive(false);
            pauseOptionsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Paneles de pausa no asignados!");
        }
    }

    void ReturnToMainMenu()
    {
        if (debugMode) Debug.Log("Volviendo al menú principal desde PauseMenuManager");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowMainMenu();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null!");
        }
    }

    public void ShowPauseMainPanel()
    {
        if (pauseMainPanel != null && pauseOptionsPanel != null)
        {
            pauseMainPanel.SetActive(true);
            pauseOptionsPanel.SetActive(false);
        }
    }

    void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SavePauseOptions()
    {
        PlayerPrefs.Save();
        ShowPauseMainPanel();
        if (debugMode) Debug.Log("Opciones guardadas");
    }

    // Método para cuando se activa/desactiva el menú de pausa
    private void OnEnable()
    {
        if (debugMode) Debug.Log("Menú de pausa activado");
        
        // Restaurar configuración cuando se activa el menú de pausa
        if (pauseVolumeSlider != null)
            pauseVolumeSlider.value = AudioListener.volume;
        if (pauseFullscreenToggle != null)
            pauseFullscreenToggle.isOn = Screen.fullScreen;
            
        ShowPauseMainPanel();
    }

    private void OnDisable()
    {
        if (debugMode) Debug.Log("Menú de pausa desactivado");
    }
}