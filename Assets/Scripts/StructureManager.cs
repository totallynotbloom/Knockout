using UnityEngine;
using UnityEngine.UI;

public class StructureManager : MonoBehaviour
{
	[Header("Structure Settings")]
	public float maxStructure = 100f;
	public float currentStructure = 0f;
	public float baseRiseRate = 10f;
	public float movementSuppression = 3f;
	private bool hasBeenHit = false;

	[Header("UI (Center-Out Visuals)")]
	public RectTransform fillRect;
	public Image fillImage;
	public Gradient structureGradient;

	[Header("Panic Settings (5s Total Window)")]
	public GameObject panicTimerUI;
	public Slider panicSlider;
	public float panicLoseDuration = 5f;
	public float overheadAppearDelay = 2f;
	private float panicTimer = 0f;

	[Header("References")]
	public Rigidbody bossRb;
	public BossHealth bossHealth;
	public GameObject loseUI;

	[Header("Threshold Visuals")]
	public Image leftLine;
	public Image rightLine;
	public Color normalLineColor = Color.greenYellow;
	public Color panicLineColor = Color.red;

	void Start()
	{
		if (panicTimerUI != null) panicTimerUI.SetActive(false);
		if (loseUI != null) loseUI.SetActive(false);

		// FIX: Initialize UI immediately so it's visible at start
		HandleUI();
	}

	void Update()
	{
		// Always update UI even if not hit yet, so currentStructure changes are reflected
		HandleUI();

		// Guard: Only perform the rise/panic logic after the first hit
		if (!hasBeenHit || (bossHealth != null && bossHealth.isDead)) return;

		float currentSpeed = (bossRb != null) ? bossRb.linearVelocity.magnitude : 0f;
		float riseMultiplier = (currentSpeed > movementSuppression) ? 0f : 1f;

		currentStructure += baseRiseRate * riseMultiplier * Time.deltaTime;
		currentStructure = Mathf.Clamp(currentStructure, 0, maxStructure);

		HandlePanicLogic();
	}

	void HandleUI()
	{
		float normalized = currentStructure / maxStructure;

		if (fillRect != null)
		{
			// 1. Removed: fillRect.anchoredPosition = Vector2.zero; 
			// This line was causing the whole bar to jump to 0,0.

			// 2. Scale only the internal Fill
			// This makes the red part grow from the center of the grey part.
			fillRect.localScale = new Vector3(normalized, 1, 1);
		}

		if (fillImage != null && structureGradient != null)
		{
			fillImage.color = structureGradient.Evaluate(normalized);
		}
	}

	void HandlePanicLogic()
	{
		// Trigger panic at 90%
		if (currentStructure >= (maxStructure * 0.9f))
		{
			// Change line colors to warn the player
			if (leftLine != null) leftLine.color = panicLineColor;
			if (rightLine != null) rightLine.color = panicLineColor;

			panicTimer += Time.deltaTime;
			if (panicTimer >= overheadAppearDelay)
			{
				if (panicTimerUI != null && !panicTimerUI.activeSelf) panicTimerUI.SetActive(true);
				if (panicSlider != null) panicSlider.value = panicTimer / panicLoseDuration;
			}
			if (panicTimer >= panicLoseDuration) TriggerLose();
		}
		else
		{
			// Reset line colors when structure is pushed back
			if (leftLine != null) leftLine.color = normalLineColor;
			if (rightLine != null) rightLine.color = normalLineColor;

			panicTimer = Mathf.MoveTowards(panicTimer, 0, Time.deltaTime * 1.5f);

			// Only hide the UI; the lines stay visible as markers
			if (panicTimerUI != null && panicTimerUI.activeSelf)
				panicTimerUI.SetActive(false);
		}
	}
	public void SetStructure(float newValue)
	{
		currentStructure = Mathf.Clamp(newValue, 0, maxStructure);
		HandleUI();
	}

	public void ReduceStructure(float amount)
	{
		if (!hasBeenHit) hasBeenHit = true;
		currentStructure -= amount;
		currentStructure = Mathf.Max(currentStructure, 0);
	}

	void TriggerLose()
	{
		Time.timeScale = 0f;
		if (loseUI != null) loseUI.SetActive(true);
	}
}