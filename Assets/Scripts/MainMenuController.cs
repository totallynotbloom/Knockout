using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;

    [Header("Scene Loading")]
    public string gameplaySceneName = "Map1Moutains";
    [Tooltip("Optional: main menu canvas (add Canvas Group to root if missing). Fades out with the black transition when Play is pressed.")]
    public CanvasGroup mainMenuCanvasGroup;

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Volume Labels (Optional)")]
    public TMP_Text masterPercentText;
    public TMP_Text musicPercentText;
    public TMP_Text sfxPercentText;

    private void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        LoadSettingsToSliders();
    }

    public void PlayGame()
    {
        SceneFadeLoader.LoadSceneToGameplay(gameplaySceneName, mainMenuCanvasGroup);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit pressed");
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
        GameAudioSettings.EnsureDefaults();

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
}
