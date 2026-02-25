using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PressureManager : MonoBehaviour
{
	[Header("Start Condition")]
	public Transform playerTransform; // Drag the player/boss here
	public float startThresholdX = 5f; // Distance to move before pressure starts
	private bool hasStarted = false;

	[Header("Thresholds")]
	public Rigidbody bossRb;
	public float velocityThreshold = 2.0f;
	public float maxRestTime = 3.0f;
	public float sliderAppearTime = 1.5f;

	[Header("UI Components")]
	public CanvasGroup sliderCanvasGroup;
	public Slider pressureSlider;
	public GameObject loseUI;

	[Header("Visual Shift")]
	public Gradient pressureGradient;
	public Image sliderFillImage;

	[Header("External References")]
	public BossHealth bossScript;
	public HeroSummoner summoner;

	private float currentRestTimer = 0f;
	public bool isGameOver = false;

	void Start()
	{
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
		// 1. Logic Guard: Don't count pressure if game is over or boss is dying
		if (isGameOver || bossRb == null || (bossScript != null && bossScript.isDead)) return;

		// 2. Start Check: Only begin logic once the player moves past the threshold
		if (!hasStarted)
		{
			if (playerTransform != null && playerTransform.position.x > startThresholdX)
			{
				hasStarted = true;
			}
			return; // Exit Update early if hasn't started yet
		}

		bool isStationary = Mathf.Abs(bossRb.linearVelocity.x) < velocityThreshold &&
							Mathf.Abs(bossRb.linearVelocity.y) < velocityThreshold;

		if (isStationary)
			currentRestTimer += Time.deltaTime;
		else
			currentRestTimer = Mathf.MoveTowards(currentRestTimer, 0, Time.deltaTime * 2f);

		HandleUI();

		if (currentRestTimer >= maxRestTime)
			TriggerLose();
	}

	void HandleUI()
	{
		if (pressureSlider == null || sliderCanvasGroup == null) return;

		pressureSlider.value = currentRestTimer;

		if (sliderFillImage != null && pressureGradient != null)
		{
			float normalizedTime = currentRestTimer / maxRestTime;
			sliderFillImage.color = pressureGradient.Evaluate(normalizedTime);
		}

		sliderCanvasGroup.alpha = Mathf.MoveTowards(sliderCanvasGroup.alpha, (currentRestTimer > sliderAppearTime) ? 1f : 0f, Time.deltaTime * 5f);
	}

	void TriggerLose()
	{
		if (isGameOver) return;
		isGameOver = true;
		StartCoroutine(DelayedLoseSequence());
	}

	IEnumerator DelayedLoseSequence()
	{
		yield return new WaitForSeconds(2.0f);
		Time.timeScale = 0f;
		if (loseUI != null) loseUI.SetActive(true);
		if (summoner != null) summoner.enabled = false;
	}

	public void ResetManager()
	{
		isGameOver = false;
		hasStarted = false; // Reset the start trigger
		currentRestTimer = 0f;
		if (loseUI != null) loseUI.SetActive(false);
		if (pressureSlider != null) pressureSlider.value = 0;
		if (sliderCanvasGroup != null) sliderCanvasGroup.alpha = 0f;
		if (summoner != null) summoner.enabled = true;
	}
}