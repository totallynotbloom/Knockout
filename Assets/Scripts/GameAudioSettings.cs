using UnityEngine;

public static class GameAudioSettings
{
    public const string MasterKey = "MasterVolume";
    public const string MusicKey = "MusicVolume";
    public const string SfxKey = "SfxVolume";

    private const float DefaultVolume = 1f;

    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterKey, DefaultVolume);
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicKey, DefaultVolume);
    }

    public static float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(SfxKey, DefaultVolume);
    }

    public static void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
        ApplyMasterVolume();
        PlayerPrefs.Save();
    }

    public static void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static void SetSfxVolume(float value)
    {
        PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static void EnsureDefaults()
    {
        if (!PlayerPrefs.HasKey(MasterKey)) PlayerPrefs.SetFloat(MasterKey, DefaultVolume);
        if (!PlayerPrefs.HasKey(MusicKey)) PlayerPrefs.SetFloat(MusicKey, DefaultVolume);
        if (!PlayerPrefs.HasKey(SfxKey)) PlayerPrefs.SetFloat(SfxKey, DefaultVolume);
        ApplyMasterVolume();
    }

    public static void ApplyMasterVolume()
    {
        AudioListener.volume = GetMasterVolume();
    }
}
