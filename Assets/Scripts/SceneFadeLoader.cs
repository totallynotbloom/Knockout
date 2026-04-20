using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen black fade + async load + optional hold on black + fade in.
/// Put one instance in your menu scene (root or child — it will move to DontDestroyOnLoad root).
/// </summary>
public class SceneFadeLoader : MonoBehaviour
{
	private const string FadeImageChildName = "FadeImage";

	public static SceneFadeLoader Instance { get; private set; }

	[System.Serializable]
	public struct FadePreset
	{
		[Tooltip("Black overlay 0 → 1 before load.")]
		public float fadeOutSeconds;
		[Tooltip("Black overlay 1 → 0 after load (and after hold).")]
		public float fadeInSeconds;
		[Tooltip("Stay fully black after the new scene is active (e.g. let music / systems start).")]
		public float holdBlackAfterLoadSeconds;
	}

	[Header("Menu → Gameplay")]
	[SerializeField] private FadePreset toGameplay = new FadePreset
	{
		fadeOutSeconds = 0.55f,
		fadeInSeconds = 0.65f,
		holdBlackAfterLoadSeconds = 1f
	};

	[Header("Gameplay → Menu")]
	[SerializeField] private FadePreset toMenu = new FadePreset
	{
		fadeOutSeconds = 0.45f,
		fadeInSeconds = 0.55f,
		holdBlackAfterLoadSeconds = 0.15f
	};

	private CanvasGroup fadeGroup;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		// Must be a root object for reliable DontDestroyOnLoad (and so we are not destroyed with the menu canvas).
		transform.SetParent(null, false);
		DontDestroyOnLoad(gameObject);
		EnsureFadeUI();
	}

	private void EnsureFadeUI()
	{
		Transform fadeTf = transform.Find(FadeImageChildName);
		if (fadeTf != null && fadeTf.TryGetComponent(out CanvasGroup existing))
		{
			fadeGroup = existing;
			EnsureRootCanvasForOverlay();
			ApplyTopmostCanvasSettings();
			return;
		}

		EnsureRootCanvasForOverlay();

		GameObject imgGo = new GameObject(FadeImageChildName);
		imgGo.transform.SetParent(transform, false);
		var img = imgGo.AddComponent<Image>();
		img.color = Color.black;
		img.raycastTarget = true;

		var rt = imgGo.GetComponent<RectTransform>();
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;

		fadeGroup = imgGo.AddComponent<CanvasGroup>();
		fadeGroup.alpha = 0f;
		fadeGroup.blocksRaycasts = false;
		fadeGroup.interactable = false;

		ApplyTopmostCanvasSettings();
	}

	private void EnsureRootCanvasForOverlay()
	{
		Canvas canvas = GetComponent<Canvas>();
		if (canvas == null)
		{
			canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.overrideSorting = true;
			canvas.sortingOrder = 32767;
		}

		if (GetComponent<CanvasScaler>() == null)
		{
			var scaler = gameObject.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
		}

		if (GetComponent<GraphicRaycaster>() == null)
			gameObject.AddComponent<GraphicRaycaster>();
	}

	private void ApplyTopmostCanvasSettings()
	{
		Canvas c = fadeGroup != null ? fadeGroup.GetComponentInParent<Canvas>() : GetComponent<Canvas>();
		if (c != null)
		{
			c.renderMode = RenderMode.ScreenSpaceOverlay;
			c.overrideSorting = true;
			c.sortingOrder = 32767;
		}
	}

	public static void LoadSceneToGameplay(string sceneName, CanvasGroup menuUiToFadeOut = null)
	{
		if (Instance != null)
		{
			Instance.StopAllCoroutines();
			Instance.StartCoroutine(Instance.LoadRoutine(sceneName, Instance.toGameplay, menuUiToFadeOut));
		}
		else
		{
			Debug.LogWarning("SceneFadeLoader: no instance in this session — loading without fade. Play from the menu scene once, or add SceneFadeLoader to a bootstrap scene.");
			SceneManager.LoadScene(sceneName);
		}
	}

	public static void LoadSceneToMenu(string sceneName)
	{
		if (Instance != null)
		{
			Instance.StopAllCoroutines();
			Instance.StartCoroutine(Instance.LoadRoutine(sceneName, Instance.toMenu, null));
		}
		else
		{
			Debug.LogWarning("SceneFadeLoader: no instance — loading menu without fade.");
			SceneManager.LoadScene(sceneName);
		}
	}

	private IEnumerator LoadRoutine(string sceneName, FadePreset preset, CanvasGroup menuUi)
	{
		if (fadeGroup == null)
			EnsureFadeUI();

		fadeGroup.blocksRaycasts = true;

		if (menuUi != null)
			yield return FadeOutBlackAndMenu(menuUi, preset.fadeOutSeconds);
		else
			yield return FadeBlackAlpha(0f, 1f, preset.fadeOutSeconds);

		AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
		op.allowSceneActivation = false;
		while (op.progress < 0.9f)
			yield return null;

		op.allowSceneActivation = true;
		while (!op.isDone)
			yield return null;

		fadeGroup.alpha = 1f;

		if (preset.holdBlackAfterLoadSeconds > 0f)
			yield return HoldUnscaled(preset.holdBlackAfterLoadSeconds);

		yield return FadeBlackAlpha(1f, 0f, preset.fadeInSeconds);

		fadeGroup.blocksRaycasts = false;
	}

	private IEnumerator FadeOutBlackAndMenu(CanvasGroup menu, float duration)
	{
		if (duration <= 0f)
		{
			fadeGroup.alpha = 1f;
			if (menu != null) menu.alpha = 0f;
			yield break;
		}

		float startMenuAlpha = menu != null ? menu.alpha : 1f;
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			float u = Mathf.Clamp01(t / duration);
			fadeGroup.alpha = Mathf.Lerp(0f, 1f, u);
			if (menu != null)
				menu.alpha = Mathf.Lerp(startMenuAlpha, 0f, u);
			yield return null;
		}

		fadeGroup.alpha = 1f;
		if (menu != null)
			menu.alpha = 0f;
	}

	private IEnumerator FadeBlackAlpha(float from, float to, float duration)
	{
		if (duration <= 0f)
		{
			fadeGroup.alpha = to;
			yield break;
		}

		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
			yield return null;
		}

		fadeGroup.alpha = to;
	}

	private IEnumerator HoldUnscaled(float seconds)
	{
		float t = 0f;
		while (t < seconds)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
	}
}
