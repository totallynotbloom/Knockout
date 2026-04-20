using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Scene Loading")]
    public string mainMenuSceneName = "MenuScene";

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Volume Labels (Optional)")]
    public TMP_Text masterPercentText;
    public TMP_Text musicPercentText;
    public TMP_Text sfxPercentText;

    private bool isPaused = false;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        GameAudioSettings.EnsureDefaults();
        LoadSettingsToSliders();
    }

    private void Update()
    {
        bool escPressed = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            escPressed = true;
        else if (Input.GetKeyDown(KeyCode.Escape))
            escPressed = true;

        if (!escPressed) return;

        if (!isPaused)
        {
            OpenPauseMenu();
            return;
        }

        // While paused, ESC closes settings first, then resumes.
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        ResumeGame();
    }

    public void OpenPauseMenu()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneFadeLoader.LoadSceneToMenu(mainMenuSceneName);
    }

    public void OnMasterVolumeChanged(float value)
    {
        GameAudioSettings.SetMasterVolume(value);
        UpdatePercentLabel(masterPercentText, value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        GameAudioSettings.SetMusicVolume(value);
        UpdatePercentLabel(musicPercentText, value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        GameAudioSettings.SetSfxVolume(value);
        UpdatePercentLabel(sfxPercentText, value);
    }

    private void LoadSettingsToSliders()
    {
        if (masterSlider != null) masterSlider.value = GameAudioSettings.GetMasterVolume();
        if (musicSlider != null) musicSlider.value = GameAudioSettings.GetMusicVolume();
        if (sfxSlider != null) sfxSlider.value = GameAudioSettings.GetSfxVolume();
        RefreshAllPercentLabels();
    }

    private void RefreshAllPercentLabels()
    {
        if (masterSlider != null) UpdatePercentLabel(masterPercentText, masterSlider.value);
        if (musicSlider != null) UpdatePercentLabel(musicPercentText, musicSlider.value);
        if (sfxSlider != null) UpdatePercentLabel(sfxPercentText, sfxSlider.value);
    }

    private void UpdatePercentLabel(TMP_Text label, float sliderValue)
    {
        if (label == null) return;
        int percent = Mathf.RoundToInt(Mathf.Clamp01(sliderValue) * 100f);
        label.text = $"{percent}%";
    }

    private void OnDisable()
    {
        // Safety: if this object gets disabled/destroyed while paused, restore gameplay.
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        isPaused = false;
    }
}
