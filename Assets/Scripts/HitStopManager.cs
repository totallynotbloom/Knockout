using UnityEngine;
using System.Collections;

/// <summary>
/// This manager creates "Impact" in combat by momentarily freezing the entire game world.
/// When a heavy hit occurs, timeScale is set to 0, allowing the player's brain to 
/// register the magnitude of the hit.
/// </summary>
public class HitStopManager : MonoBehaviour
{
	// Singleton instance so any script (Boss, Hero) can call this easily
	public static HitStopManager Instance;

	[Header("Hit Stop Settings")]
	[Tooltip("The minimum time the game freezes for ANY hit (even weak ones).")]
	public float baseDuration = 0.05f; // Note: In your code, this was 0.8, which is very long! Standard is 0.05-0.1

	[Tooltip("How much the impact force adds to the freeze. Higher = longer freezes for heavy hits.")]
	public float forceScaleFactor = 15000f;

	[Tooltip("The absolute maximum time the game can stay frozen.")]
	public float maxDuration = 0.4f;

	// Simple gate to prevent "Freezing while already Frozen" which can cause jitter
	private bool isWaiting = false;

	void Awake()
	{
		// Singleton Pattern: Ensures only one manager exists
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	/// <summary>
	/// Called by the BossHealth script whenever damage is received.
	/// It calculates a freeze duration based on how hard the hit was.
	/// </summary>
	public void TriggerVariableHitStop(float impactForce)
	{
		if (isWaiting) return;

		// CALCULATION: The more force, the longer the bonus.
		// Formula: base + (force / scale), limited by the max duration.
		float scalingBonus = impactForce / forceScaleFactor;
		float totalDuration = Mathf.Clamp(baseDuration + scalingBonus, baseDuration, maxDuration);

		StartCoroutine(DoHitStop(totalDuration));
	}

	/// <summary>
	/// The actual logic that manipulates Unity's Global Time Scale.
	/// </summary>
	private IEnumerator DoHitStop(float duration)
	{
		isWaiting = true;

		// Log for debugging so the team knows why the game "stopped"
		Debug.Log("<color=red>FREEZING TIME NOW</color>");

		// Store what time was doing BEFORE the hit (it might have been in Slow-Mo!)
		float originalTimeScale = Time.timeScale;

		// Hard freeze
		Time.timeScale = 0f;

		// Use WaitForSecondsRealtime because Time.timeScale is now 0!
		// Normal WaitForSeconds would wait forever.
		yield return new WaitForSecondsRealtime(duration);

		// Restore the game speed to what it was
		Time.timeScale = originalTimeScale;

		Debug.Log("<color=green>THAWING TIME</color>");
		isWaiting = false;
	}
}