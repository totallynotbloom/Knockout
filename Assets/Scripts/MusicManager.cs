using UnityEngine;

public class MusicManager : MonoBehaviour
{
	public static MusicManager Instance;

	[Header("Background Music")]
	public AudioClip backgroundSong;
	[Range(0f, 1f)] public float musicVolume = 0.5f; // Slider for Music
	private AudioSource musicSource;

	[Header("Tactical Audio")]
	public AudioClip slowMoStartSFX;
	[Range(0f, 1f)] public float slowMoSFXVolume = 0.8f; // Slider for Slow-Mo Entry
	private AudioSource sfxSource;

	[Header("Pitch Settings")]
	public float slowMoPitch = 0.7f;
	public float transitionSpeed = 5f;

	[Header("UI Audio")]
	public AudioClip navigationTickSFX; // The "Blip" or "Tick" sound
	[Range(0f, 1f)] public float navVolume = 0.4f;
	private AudioSource uiSource;

	void Awake()
	{
		Instance = this;

		// Setup Music Source
		musicSource = gameObject.AddComponent<AudioSource>();
		musicSource.clip = backgroundSong;
		musicSource.loop = true;
		musicSource.volume = musicVolume; // Apply initial volume
		musicSource.Play();

		// Setup SFX Source
		sfxSource = gameObject.AddComponent<AudioSource>();

		uiSource = gameObject.AddComponent<AudioSource>();
	}

	void Update()
	{
			// 1. Sync Volumes
			// This makes the slider in the MusicManager control the AudioSource volume
			if (musicSource != null) musicSource.volume = musicVolume;
			if (sfxSource != null) sfxSource.volume = slowMoSFXVolume;

			// 2. Sync and Smooth the Pitch
			// The targetPitch is based on whether time is slow or normal
			float targetPitch = (Time.timeScale < 1.0f) ? slowMoPitch : 1.0f;

			// Lerp ensures the music doesn't "snap" but slides to the new pitch
			musicSource.pitch = Mathf.Lerp(musicSource.pitch, targetPitch, Time.unscaledDeltaTime * transitionSpeed);
	}

	public void PlaySlowMoInitiation()
	{
		if (slowMoStartSFX != null)
		{
			// PlayOneShot allows you to pass a volume scale directly
			sfxSource.PlayOneShot(slowMoStartSFX, slowMoSFXVolume);
		}
	}
	public void PlayNavigationSound()
	{
		if (navigationTickSFX != null)
		{
			uiSource.PlayOneShot(navigationTickSFX, navVolume);
		}
	}
}