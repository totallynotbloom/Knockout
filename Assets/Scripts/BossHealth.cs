using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// This script manages the Boss's vitals, armor mechanics, and death sequence.
/// It acts as the primary data source for the Boss's status in the game loop.
/// </summary>
public class BossHealth : MonoBehaviour
{
	[Header("Identity")]
	public string bossName = "Big Blue";
	public float maxHealth = 100f;
	public float currentHealth;

	[Header("Armor Settings")]
	public float maxArmor = 50f;
	public float currentArmor;
	public bool isArmorBroken = false;    // When true, damage goes to Health instead of Armor
	public float armorRegenTime = 15f;    // How long the boss stays vulnerable

	[Header("UI References")]
	public Slider healthSlider;           // Main red bar
	public Slider armorSlider;            // The blue shield bar
	public TextMeshProUGUI armorTimerText; // Countdown timer for shield return
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI healthNumbersText; // Text display (e.g., "100/100")

	[Header("Death Settings")]
	public float deathLaunchForce = 50f;  // How hard the boss is hit when defeated
	public bool isDead = false;           // State gate to prevent multiple death triggers

	public GameObject winUI;              // The "YOU WIN" UI Panel
	public GlobalTargetManager targetManager; // Ensures boss only takes damage if no minions are left


	void Start()
	{
		// Initialize vitals to full at start
		currentHealth = maxHealth;
		currentArmor = maxArmor;

		// Setup UI limits
		if (healthSlider != null) healthSlider.maxValue = maxHealth;
		if (armorSlider != null) armorSlider.maxValue = maxArmor;
		if (nameText != null) nameText.text = bossName;

		UpdateUI();
	}

	/// <summary>
	/// The main entry point for dealing damage. 
	/// Handles Armor-first logic and communicates with the HitStop system.
	/// </summary>
	public void TakeDamage(float amount, float impactForce = 0f, bool isParry = false)
	{
		if (isDead) return; // Ignore damage if the boss is already in death sequence

		// Logic Gate: If minions exist in the TargetManager, the boss is invulnerable
		if (targetManager != null && targetManager.enemies.Count > 1) return;

		// HIT-STOP SYSTEM: Create visual "impact" based on force
		if (impactForce > 0 && HitStopManager.Instance != null)
		{
			// If the player successfully parried, we boost the force 5x 
			// to create a massive "heavy" hit-stop effect.
			float finalForce = isParry ? impactForce * 5f : impactForce;
			HitStopManager.Instance.TriggerVariableHitStop(finalForce);
		}

		// DAMAGE LOGIC: Check if we hit the shield (Armor) or the actual Health
		if (!isArmorBroken)
		{
			currentArmor -= amount;
			if (currentArmor <= 0)
			{
				currentArmor = 0;
				BreakArmor(); // Transition to vulnerable state
			}
		}
		else
		{
			currentHealth -= amount;
		}

		currentHealth = Mathf.Max(currentHealth, 0); // Clamp health so it doesn't go negative
		UpdateUI();

		if (currentHealth <= 0) Die();
	}

	/// <summary>
	/// Triggers when armor hits 0. Starts the vulnerability window.
	/// </summary>
	void BreakArmor()
	{
		isArmorBroken = true;
		StartCoroutine(RegenArmorTimer());
	}

	/// <summary>
	/// A timer that tracks how long the boss is vulnerable before shield resets.
	/// </summary>
	IEnumerator RegenArmorTimer()
	{
		float timeLeft = armorRegenTime;

		if (armorTimerText != null) armorTimerText.gameObject.SetActive(true);

		while (timeLeft > 0)
		{
			if (armorTimerText != null)
			{
				armorTimerText.text = $"Shield: {timeLeft:F1}s"; // Show 1 decimal place

				// Visual Warning: Turn text red when shield is about to return
				armorTimerText.color = (timeLeft < 3f) ? Color.red : Color.white;
			}

			timeLeft -= Time.deltaTime;
			yield return null; // Wait for next frame
		}

		// Reset Armor State
		currentArmor = maxArmor;
		isArmorBroken = false;

		if (armorTimerText != null) armorTimerText.gameObject.SetActive(false);

		UpdateUI();
	}

	/// <summary>
	/// Synchronizes the internal variables with the On-Screen UI elements.
	/// </summary>
	public void UpdateUI()
	{
		if (healthSlider != null) healthSlider.value = currentHealth;
		if (armorSlider != null) armorSlider.value = currentArmor;

		if (healthNumbersText != null)
		{
			healthNumbersText.text = $"{Mathf.RoundToInt(currentHealth)} / {maxHealth}";
		}
	}

	/// <summary>
	/// Initiates the victory sequence.
	/// </summary>
	void Die()
	{
		if (isDead) return;
		isDead = true;

		// Apply a cinematic physics "kick" to the boss
		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.AddForce(new Vector3(1, 1, 0) * 10f, ForceMode.Impulse);
		}

		// Notify the VictoryManager to handle the UI and Time freeze
		VictoryManager.Instance.ShowVictoryMenu();
	}

	/// <summary>
	/// Essential for the Endless Loop. Restores the boss to a "Fresh" state
	/// so the player can kill him again without reloading the scene.
	/// </summary>
	public void ResetDeathState()
	{
		isDead = false;

		// Clean up any leftovers from the previous fight
		StopAllCoroutines();
		if (armorTimerText != null) armorTimerText.gameObject.SetActive(false);

		// Restore Vitals
		currentHealth = maxHealth;
		currentArmor = maxArmor;
		isArmorBroken = false;

		// Ensure the boss can be clicked/hit again
		if (TryGetComponent<Collider>(out Collider col))
		{
			col.enabled = true;
		}

		UpdateUI();
	}
}