using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealth : MonoBehaviour
{
	public string bossName = "Big Blue";
	public float maxHealth = 100f;
	public float currentHealth;

	[Header("Armor Settings")]
	public float maxArmor = 50f;
	public float currentArmor;
	public bool isArmorBroken = false;
	public float armorRegenTime = 15f;

	[Header("UI References")]
	public Slider healthSlider;
	public Slider armorSlider; // The new blue slider
	public TextMeshProUGUI armorTimerText;
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI healthNumbersText;

	[Header("Death Settings")]
	public float deathLaunchForce = 50f;
	public bool isDead = false;

	public GameObject winUI; // Assign a "YOU WIN" panel here
	public GlobalTargetManager targetManager;


	void Start()
	{
		currentHealth = maxHealth;
		currentArmor = maxArmor;

		if (healthSlider != null) healthSlider.maxValue = maxHealth;
		if (armorSlider != null) armorSlider.maxValue = maxArmor;
		if (nameText != null) nameText.text = bossName;

		UpdateUI();
	}

	public void TakeDamage(float amount, float impactForce = 0f, bool isParry = false)
	{
		Debug.Log($"Boss took {amount} damage. Force received: {impactForce}. Parry: {isParry}");

		if (targetManager != null && targetManager.enemies.Count > 1) return;

		if (impactForce > 0 && HitStopManager.Instance != null)
		{
			// THE PARRY BOOST: 
			// If it's a parry, we multiply the force by 5 (or any number you like)
			// This ensures the HitStopManager sees a "Heavy" hit and freezes time longer.
			float finalForce = isParry ? impactForce * 5f : impactForce;

			HitStopManager.Instance.TriggerVariableHitStop(finalForce);
		}

		// --- Rest of your existing logic ---
		if (!isArmorBroken)
		{
			currentArmor -= amount;
			if (currentArmor <= 0)
			{
				currentArmor = 0;
				BreakArmor();
			}
		}
		else
		{
			currentHealth -= amount;
		}

		currentHealth = Mathf.Max(currentHealth, 0);
		UpdateUI();

		if (currentHealth <= 0) Die();
	}
	void BreakArmor()
	{
		isArmorBroken = true;
		Debug.Log("Armor Broken! Double Force Active.");
		StartCoroutine(RegenArmorTimer());
	}

	IEnumerator RegenArmorTimer()
	{
		float timeLeft = armorRegenTime;

		// Show the text when broken
		if (armorTimerText != null) armorTimerText.gameObject.SetActive(true);

		while (timeLeft > 0)
		{
			if (armorTimerText != null)
			{
				// Display as "Shield: 14.2s"
				armorTimerText.text = $"Shield: {timeLeft:F1}s";
			}
			if (timeLeft < 3f)
			{
				armorTimerText.color = Color.red; // Flash red when under 3 seconds
			}
			else
			{
				armorTimerText.color = Color.white;
			}

			timeLeft -= Time.deltaTime;
			yield return null; // Wait for next frame
		}

		// Reset armor
		currentArmor = maxArmor;
		isArmorBroken = false;

		// Hide the text when restored
		if (armorTimerText != null) armorTimerText.gameObject.SetActive(false);

		UpdateUI();
		Debug.Log("Armor Restored!");
	}
	public void UpdateUI()
	{
		if (healthSlider != null) healthSlider.value = currentHealth;
		if (armorSlider != null) armorSlider.value = currentArmor;

		if (healthNumbersText != null)
		{
			healthNumbersText.text = $"{Mathf.RoundToInt(currentHealth)} / {maxHealth}";
		}
	}

	void Die()
	{
		if (isDead) return;
		isDead = true;

		// 2. Apply a massive "Finisher" force to launch him off-screen
		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.linearVelocity = Vector3.zero; // Reset current speed
			rb.AddForce(new Vector3(1, 1, 0) * deathLaunchForce, ForceMode.Impulse);
		}

		// 3. Start checking if he is off-screen
		StartCoroutine(CheckOffScreen());
	}
	IEnumerator CheckOffScreen()
	{
		// Wait for the boss to actually move off the camera view
		while (true)
		{
			Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
			bool isOffScreen = screenPoint.x < -0.1f || screenPoint.x > 1.1f || screenPoint.y < -0.1f || screenPoint.y > 1.1f;

			if (isOffScreen) break;
			yield return new WaitForSeconds(0.1f);
		}

		// Tell the Victory Manager to show the selection screen
		VictoryManager.Instance.ShowVictoryMenu();
	}
	public void ResetDeathState()
	{
		isDead = false;

		// Re-enable the collider so heroes can hit him again
		if (TryGetComponent<Collider>(out Collider col))
		{
			col.enabled = true;
		}

		// Reset physics so he doesn't keep drifting away
		if (TryGetComponent<Rigidbody>(out Rigidbody rb))
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
	}
}