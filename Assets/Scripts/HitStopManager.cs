using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
	public static HitStopManager Instance;

	[Header("Hit Stop Settings")]
	[Tooltip("The minimum time the game freezes for ANY hit (even weak ones).")]
	public float baseDuration = 0.8f;

	[Tooltip("How much the impact force adds to the freeze. Higher = longer freezes for heavy hits.")]
	public float forceScaleFactor = 15000f;

	[Tooltip("The absolute maximum time the game can stay frozen.")]
	public float maxDuration = 0.4f;

	private bool isWaiting = false;

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject); // Prevent duplicates
		}
	}

	public void TriggerVariableHitStop(float impactForce)
	{
		if (isWaiting) return;

		// Use your public baseDuration + the scaling logic
		float scalingBonus = impactForce / forceScaleFactor;
		float totalDuration = Mathf.Clamp(baseDuration + scalingBonus, baseDuration, maxDuration);

		StartCoroutine(DoHitStop(totalDuration));
	}

	private IEnumerator DoHitStop(float duration)
	{
		isWaiting = true;

		// DEBUG: Flash the screen or log to console
		Debug.Log("<color=red>FREEZING TIME NOW</color>");

		float originalTimeScale = Time.timeScale;
		Time.timeScale = 0f;

		yield return new WaitForSecondsRealtime(duration);

		// If Tactical Mode was active, we might want to return to 0.2f instead of 1.0f
		// But for now, let's just restore what it was.
		Time.timeScale = originalTimeScale;

		Debug.Log("<color=green>THAWING TIME</color>");
		isWaiting = false;
	}
}