using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
	[Header("Identity")]
	public string bossName = "Big Blue";
	public float maxHealth = 100f;
	public float currentHealth;

	[Header("Structure Link")]
	public StructureManager structureManager;

	[Header("UI References")]
	public Slider healthSlider;
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI healthNumbersText;

	[Header("Death Settings")]
	public float deathLaunchForce = 50f;
	public bool isDead = false;

	[Header("Ground Slam Settings")]
	public float slamThreshold = -15f;
	public float speedToDamageMultiplier = 2f;
	public AudioSource slamAudioSource;
	public AudioClip slamSound;
	[Range(0, 1)] public float slamVolume = 0.5f;
	private float slamCooldown = 0.5f;
	private float nextSlamTime = 0f;

	[Header("Slam Balance")]
	public float slamHealthDamage = 25f;    // <--- NEW: Set Health damage in Inspector
	public float slamStructureDamage = 50f; // <--- NEW: Set Structure damage in Inspector

	[Header("Slam Visuals")]
	public GameObject damageNumberPrefab;
	public Color slamNumberColor = Color.green;

	[Header("Move Slam Logic")]
	private float slamWindowTimer = 0f;
	public float slamWindowDuration = 1.5f;
	public Transform worldCanvas;

	[Header("References")]
	public GameObject winUI;
	public GlobalTargetManager targetManager;
	private Rigidbody rb;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
		currentHealth = maxHealth;

		if (healthSlider != null) healthSlider.maxValue = maxHealth;
		if (nameText != null) nameText.text = bossName;

		UpdateUI();
	}

	void Update()
	{
		if (slamWindowTimer > 0)
		{
			slamWindowTimer -= Time.deltaTime;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		PhysicsMaterial hitMaterial = collision.collider.sharedMaterial;

		if (hitMaterial == null)
		{
			hitMaterial = collision.gameObject.GetComponentInParent<Collider>()?.sharedMaterial;
		}

		if (hitMaterial != null && hitMaterial.name.ToLower().Contains("slush"))
		{
			if (Time.time >= nextSlamTime && !isDead)
			{
				float verticalSpeed = rb.linearVelocity.y;

				bool isPhysicsSlam = verticalSpeed <= slamThreshold;
				bool isMoveSlam = slamWindowTimer > 0;

				if (isPhysicsSlam || isMoveSlam)
				{
					// We now use your custom Inspector values instead of just calculating by speed
					float finalHealthDamage = slamHealthDamage;

					// Optional: If they are falling REALLY fast, add a bonus based on speed
					if (Mathf.Abs(verticalSpeed) > Mathf.Abs(slamThreshold) * 1.5f)
					{
						finalHealthDamage += Mathf.Abs(verticalSpeed) * 0.5f;
					}

					// Apply the damage using the 'isSlam' flag
					TakeDamage(finalHealthDamage, 0f, false, false, true);
					SpawnSlamDamageNumber(finalHealthDamage);

					if (slamAudioSource != null && slamSound != null)
					{
						// The second parameter is the volume scale (0.0 to 1.0)
						slamAudioSource.PlayOneShot(slamSound, slamVolume);
					}

					slamWindowTimer = 0;
					nextSlamTime = Time.time + slamCooldown;
				}
			}
		}
	}

	public void TakeDamage(float amount, float impactForce = 0f, bool isParry = false, bool isSpike = false, bool isSlam = false)
	{
		if (isDead) return;

		if (isSpike) slamWindowTimer = slamWindowDuration;

		if (structureManager != null)
		{
			if (isParry)
			{
				structureManager.SetStructure(structureManager.maxStructure * 0.2f);
			}
			else if (isSlam)
			{
				// Uses the specific float you set in the Inspector
				structureManager.ReduceStructure(slamStructureDamage);
			}
			else
			{
				structureManager.ReduceStructure(amount);
			}
		}

		currentHealth -= amount;
		currentHealth = Mathf.Max(currentHealth, 0);

		UpdateUI();
		if (currentHealth <= 0) Die();
	}
	void SpawnSlamDamageNumber(float damage)
	{
		if (damageNumberPrefab != null && worldCanvas != null)
		{
			// Instantiate as a child of the World Canvas
			GameObject dn = Instantiate(damageNumberPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, worldCanvas);

			if (dn.TryGetComponent<DamageNumber>(out DamageNumber dnScript))
			{
				dnScript.SetText(damage);

				// Access the TMP component to change the color
				TMPro.TextMeshProUGUI txt = dn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
				if (txt != null)
				{
					txt.color = slamNumberColor;
				}
			}
		}
	}

	public void UpdateUI()
	{
		if (healthSlider != null) healthSlider.value = currentHealth;
		if (healthNumbersText != null) healthNumbersText.text = $"{Mathf.RoundToInt(currentHealth)} / {maxHealth}";
	}

	void Die()
	{
		if (isDead) return;
		isDead = true;
		if (rb != null) rb.AddForce(new Vector3(1, 1, 0) * deathLaunchForce, ForceMode.Impulse);
		VictoryManager.Instance.ShowVictoryMenu();
	}

	public void ResetDeathState()
	{
		isDead = false;
		StopAllCoroutines();
		currentHealth = maxHealth;
		if (TryGetComponent<Collider>(out Collider col)) col.enabled = true;
		UpdateUI();
	}
}