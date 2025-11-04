using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Botones del Menú Principal")]
    public Button startButton;
    public Button optionsButton;
    public Button quitButton;
    
    [Header("Paneles")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    
    [Header("Opciones")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    
    [Header("Configuración")]
    public bool debugMode = true;

    private void Start()
    {
        if (debugMode) Debug.Log("MainMenuManager iniciado");
        
        // Verificar referencias críticas
        if (!VerifyCriticalReferences())
        {
            Debug.LogError("MainMenuManager: Faltan referencias críticas!");
            return;
        }

        // Configurar listeners de botones
        SetupButtonListeners();
        
        // Configurar opciones
        SetupOptions();
        
        // Mostrar panel principal
        ShowMainPanel();

        if (debugMode) Debug.Log("MainMenuManager configurado correctamente");
    }

    bool VerifyCriticalReferences()
    {
        bool allOK = true;

        if (startButton == null)
        {
            Debug.LogError("MainMenuManager: startButton no asignado!");
            allOK = false;
        }

        if (mainPanel == null)
        {
            Debug.LogError("MainMenuManager: mainPanel no asignado!");
            allOK = false;
        }

        return allOK;
    }

    void SetupButtonListeners()
    {
        // Configurar botón INICIAR (CRÍTICO)
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
            if (debugMode) Debug.Log("Listener de StartButton configurado");
        }

        // Configurar botón OPCIONES (opcional)
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(ShowOptions);
        }

        // Configurar botón SALIR
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void StartGame() // Cambiado a público para acceso externo
    {
        if (debugMode) Debug.Log("Botón Iniciar Juego presionado");
        
        // Verificar que GameManager existe
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance es null!");
            // Intentar encontrar el GameManager
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                Debug.Log("GameManager encontrado manualmente");
                gm.StartGame();
            }
            return;
        }

        // Llamar al método StartGame del GameManager
        GameManager.Instance.StartGame();
        
        if (debugMode) Debug.Log("StartGame llamado exitosamente");
    }

    void ShowOptions()
    {
        if (debugMode) Debug.Log("Mostrando opciones");
        
        if (mainPanel != null && optionsPanel != null)
        {
            mainPanel.SetActive(false);
            optionsPanel.SetActive(true);
        }
    }

    void QuitGame()
    {
        if (debugMode) Debug.Log("Saliendo del juego");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null al intentar salir!");
            
            // Fallback
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }

    public void ShowMainPanel()
    {
        if (debugMode) Debug.Log("Mostrando panel principal");
        
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    void SetupOptions()
    {
        // Configurar volumen si existe
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        // Configurar pantalla completa si existe
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // Configurar resoluciones si existe
        if (resolutionDropdown != null)
        {
            SetupResolutions();
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

    void SetupResolutions()
    {
        resolutionDropdown.ClearOptions();
        
        Resolution[] resolutions = Screen.resolutions;
        
        foreach (Resolution res in resolutions)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData($"{res.width}x{res.height}");
            resolutionDropdown.options.Add(option);
        }
        
        resolutionDropdown.value = GetCurrentResolutionIndex(resolutions);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    int GetCurrentResolutionIndex(Resolution[] resolutions)
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }
        return 0;
    }

    void SetResolution(int index)
    {
        Resolution[] resolutions = Screen.resolutions;
        if (index >= 0 && index < resolutions.Length)
        {
            Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreen);
        }
    }

    public void SaveOptions()
    {
        PlayerPrefs.Save();
        ShowMainPanel();
        if (debugMode) Debug.Log("Opciones guardadas");
    }
}