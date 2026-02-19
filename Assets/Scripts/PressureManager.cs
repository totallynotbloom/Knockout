using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// This script monitors the Boss's movement. If the player stops hitting the Boss
/// (causing it to sit still), a "Pressure" bar fills up. If it fills completely, 
/// the player loses. This forces aggressive play.
/// </summary>
public class PressureManager : MonoBehaviour
{
	[Header("Thresholds")]
	public Rigidbody bossRb;          // We check the boss's velocity here
	public float velocityThreshold = 2.0f; // Speed below this is considered "Stationary"
	public float maxRestTime = 3.0f;       // Total time boss can be still before Game Over
	public float sliderAppearTime = 1.5f;  // Show the warning bar after this many seconds

	[Header("UI Components")]
	public CanvasGroup sliderCanvasGroup; // Used to fade the UI bar in and out
	public Slider pressureSlider;
	public GameObject loseUI;             // The "YOU LOSE" panel

	[Header("Visual Shift")]
	public Gradient pressureGradient;     // Goes from Green (Safe) to Red (Danger)
	public Image sliderFillImage;

	[Header("External References")]
	public BossHealth bossScript;
	public HeroSummoner summoner;

	private float currentRestTimer = 0f;
	public bool isGameOver = false;

	void Start()
	{
		// Hide UI on startup
		if (sliderCanvasGroup != null) sliderCanvasGroup.alpha = 0f;
		if (loseUI != null) loseUI.SetActive(false);

		if (pressureSlider != null)
		{
			pressureSlider.maxValue = maxRestTime;
			pressureSlider.minValue = 0;
		}
	}

	void Update()
	{
		// Stop all logic if the game ended or the boss is already dead
		if (isGameOver || bossRb == null || (bossScript != null && bossScript.isDead)) return;

		// 1. CHECK BOSS MOVEMENT
		// We check both X and Y axis to see if the boss is effectively "idle"
		bool isStationary = Mathf.Abs(bossRb.linearVelocity.x) < velocityThreshold &&
							Mathf.Abs(bossRb.linearVelocity.y) < velocityThreshold;

		if (isStationary)
		{
			// Boss is still: Increase the pressure!
			currentRestTimer += Time.deltaTime;
		}
		else
		{
			// Boss is moving: Cool down the pressure twice as fast as it builds
			currentRestTimer = Mathf.MoveTowards(currentRestTimer, 0, Time.deltaTime * 2f);
		}

		HandleUI();

		// 2. CHECK LOSE CONDITION
		if (currentRestTimer >= maxRestTime)
			TriggerLose();
	}

	/// <summary>
	/// Manages the visibility and color of the Pressure Slider.
	/// </summary>
	void HandleUI()
	{
		if (pressureSlider == null || sliderCanvasGroup == null) return;

		pressureSlider.value = currentRestTimer;

		// Update color based on the gradient (Green -> Red)
		if (sliderFillImage != null && pressureGradient != null)
		{
			float normalizedTime = currentRestTimer / maxRestTime;
			sliderFillImage.color = pressureGradient.Evaluate(normalizedTime);
		}

		// Fade the UI in only if the boss has been still for longer than 'sliderAppearTime'
		float targetAlpha = (currentRestTimer > sliderAppearTime) ? 1f : 0f;
		sliderCanvasGroup.alpha = Mathf.MoveTowards(sliderCanvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
	}

	void TriggerLose()
	{
		if (isGameOver) return;
		isGameOver = true;
		StartCoroutine(DelayedLoseSequence());
	}

	IEnumerator DelayedLoseSequence()
	{
		// Brief pause to let the player realize why they lost
		yield return new WaitForSeconds(2.0f);
		Time.timeScale = 0f;
		if (loseUI != null) loseUI.SetActive(true);

		// Disable summoner so keys don't trigger moves while on the lose screen
		if (summoner != null) summoner.enabled = false;
	}

	/// <summary>
	/// Resets the manager for the Endless Loop. 
	/// Called by VictoryManager when the player clicks "Continue".
	/// </summary>
	public void ResetManager()
	{
		isGameOver = false;
		currentRestTimer = 0f;
		if (loseUI != null) loseUI.SetActive(false);
		if (pressureSlider != null) pressureSlider.value = 0;
		if (sliderCanvasGroup != null) sliderCanvasGroup.alpha = 0f;
		if (summoner != null) summoner.enabled = true;
	}
}