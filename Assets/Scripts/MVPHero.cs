using System;
using UnityEngine;
// Explicitly tell C# to use Unity's Random to avoid ambiguity errors with System.Random
using Random = UnityEngine.Random;

/// <summary>
/// Controls the individual behavior of a summoned hero.
/// Now interacts with the Boss's Structure Meter instead of Armor.
/// </summary>
public class MVPHero : MonoBehaviour
{
	[System.Serializable]
	public class MoveProfile
	{
		public string moveName;
		[TextArea(2, 5)] public string description; // The text for the info box
		public Vector2 hitForceXY;     // X = Knockback, Y = Launch height
		public float cooldown = 5f;
		public float approachSpeed = 15f;
		public float giveUpTime = 4f;
		public bool isSpike;
	}

	[Header("Move Configurations")]
	public MoveProfile move1;
	public MoveProfile move2;

	[HideInInspector] public MoveProfile selectedMove;

	private float currentApproachSpeed;
	private float currentGiveUpTime;

	private Transform target;
	private Rigidbody targetRb;
	private bool hasHit = false;
	private float lifeTimer = 0f;
	private bool canReengage = false;
	private HeroSummoner ownerSummoner;
	private int ownerHeroIndex = -1;

	[Header("Regroup Settings")]
	private bool isFollowingCamera = false;
	private Transform camTransform;
	private float randomXOffset;
	private float randomYOffset;

	[Header("Post-Impact Retreat")]
	public float postImpactDuration = 2.0f;
	public float retreatFollowDistance = 3.5f;
	public float retreatHeightOffset = 0.25f;
	public float retreatMinSpeed = 3.0f;
	public float retreatCatchupBonus = 4.0f;
	public float retreatSpeedSmoothing = 5.0f;
	private bool isPostImpactRetreat = false;
	private float postImpactTimer = 0f;
	private float currentRetreatSpeed = 0f;

	[Header("Damage Number Settings")]
	public GameObject damageNumberPrefab;
	public Transform worldCanvas;

	[Header("Movement Settings")]
	public float currentSpeedMultiplier = 1f;

	[Header("Parry Duel Settings")]
	public bool isProwling = false;
	public float prowlDistance = 5f;

	[Header("Audio Settings")]
	public AudioClip[] characterHitSounds;
	[Range(0f, 1f)] public float hitVolume = 0.1f;
	private AudioSource localAudioSource;

	[Header("SFX Settings")]
	public AudioClip punchSound;
	[Range(0, 1)] public float punchVolume = 0.4f;

	[HideInInspector] public float currentPowerMultiplier = 1f;

	private Rigidbody myRb;

	public void Initialize(Transform slushTarget, int moveNumber, HeroSummoner summoner = null, int heroIndex = -1)
	{
		target = slushTarget;
		if (target != null) targetRb = target.GetComponent<Rigidbody>();
		ownerSummoner = summoner;
		ownerHeroIndex = heroIndex;

		myRb = GetComponent<Rigidbody>();
		camTransform = Camera.main.transform;

		selectedMove = (moveNumber == 1) ? move1 : move2;
		canReengage = false;
		hasHit = false;
		isFollowingCamera = false;
		isPostImpactRetreat = false;
		lifeTimer = 0f;
		postImpactTimer = 0f;
		currentRetreatSpeed = 0f;

		currentApproachSpeed = selectedMove.approachSpeed;
		currentGiveUpTime = selectedMove.giveUpTime;

		randomXOffset = Random.Range(-25f, -15f);
		randomYOffset = Random.Range(-2f, 2f);

		localAudioSource = GetComponent<AudioSource>();
		if (localAudioSource == null)
		{
			localAudioSource = gameObject.AddComponent<AudioSource>();
		}
	}

	void Update()
	{
		if (target == null || targetRb == null) return;

		if (!hasHit)
		{
			lifeTimer += Time.deltaTime;

			if (lifeTimer >= currentGiveUpTime)
			{
				GiveUp();
				return;
			}

			if (isProwling)
			{
				Vector3 standoffPos = target.position + new Vector3(-prowlDistance, 0, 0);
				transform.position = Vector3.Lerp(transform.position, standoffPos, Time.deltaTime * 5f);
				return;
			}

			float distanceToBoss = Vector3.Distance(transform.position, target.position);
			if (currentSpeedMultiplier > 1f && distanceToBoss < 2.0f)
			{
				HandleManualHit(target.gameObject);
				return;
			}

			float bossSpeedX = targetRb.linearVelocity.x;
			float totalSpeed = (Mathf.Max(bossSpeedX, 0) + currentApproachSpeed) * currentSpeedMultiplier;

			transform.position = Vector3.MoveTowards(transform.position, target.position, totalSpeed * Time.deltaTime);
		}
		else if (isFollowingCamera)
		{
			Vector3 regroupPos = new Vector3(camTransform.position.x + randomXOffset, target.position.y + randomYOffset, 0f);
			transform.position = Vector3.Lerp(transform.position, regroupPos, Time.deltaTime * 3f);
		}
		else if (isPostImpactRetreat)
		{
			UpdatePostImpactRetreat();
		}
	}

	public void TriggerParryExit()
	{
		isProwling = false;
		hasHit = true;
		canReengage = false;
		isPostImpactRetreat = false;

		if (HitStopManager.Instance != null)
			HitStopManager.Instance.TriggerVariableHitStop(2000f);

		if (myRb != null)
		{
			myRb.isKinematic = false;
			myRb.useGravity = false;
			myRb.linearVelocity = new Vector3(-20f, 5f, 0f);
		}

		Destroy(gameObject, 1.5f);
	}

	void GiveUp()
	{
		hasHit = true;
		canReengage = false;
		HeroSummoner summoner = FindFirstObjectByType<HeroSummoner>();
		if (summoner != null) summoner.ResetHeroCooldown(this.gameObject);
		StayOnScreen();
	}

	/// <summary>
	/// The core "Attack" logic. 
	/// UPDATED: Directly damages Boss Health and Structure.
	/// </summary>
	void HandleManualHit(GameObject bossObj)
	{
		if (hasHit) return;
		hasHit = true;
		canReengage = true;
		StartMoveCooldownOnImpact();
		DisableCollisionsAfterImpact();

		if (bossObj.TryGetComponent<BossHealth>(out BossHealth boss))
		{
			float damageCalculated = (selectedMove.hitForceXY.magnitude * 0.5f) * currentPowerMultiplier;
			float impactForce = selectedMove.hitForceXY.magnitude * currentPowerMultiplier;

			// Pass the isSpike boolean from the selected move
			boss.TakeDamage(damageCalculated, impactForce, false, selectedMove.isSpike);

			// 3. UI FEEDBACK
			if (damageNumberPrefab != null && worldCanvas != null)
			{
				Vector3 spawnPos = bossObj.transform.position + new Vector3(0, 2f, -1f);
				GameObject dn = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity, worldCanvas);
				if (dn.TryGetComponent<DamageNumber>(out DamageNumber dnScript))
					dnScript.SetText(damageCalculated);
			}

			// 4. PHYSICS KNOCKBACK
			// Boss knockback is now consistent since Armor is gone
			Rigidbody bossRb = bossObj.GetComponent<Rigidbody>();
			if (bossRb != null)
			{
				Vector3 force = new Vector3(selectedMove.hitForceXY.x, selectedMove.hitForceXY.y, 0);
				bossRb.AddForce(force * currentPowerMultiplier, ForceMode.Impulse);
			}

			PlayCharacterHitSound();
		}
		BeginPostImpactRetreat();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && !hasHit)
		{
			HandleManualHit(other.gameObject);
		}
	}

	void StayOnScreen()
	{
		isPostImpactRetreat = false;

		if (myRb != null)
		{
			myRb.linearVelocity = Vector3.zero;
			myRb.angularVelocity = Vector3.zero;
			myRb.isKinematic = true;
			myRb.useGravity = false;
		}
		Invoke("StartFollowing", 1.5f);
	}

	public bool CanReengage()
	{
		return canReengage && hasHit;
	}

	public void ReigniteAttack()
	{
		if (!CanReengage()) return;

		canReengage = false;
		hasHit = false;
		isFollowingCamera = false;
		isPostImpactRetreat = false;
		lifeTimer = 0f;
		postImpactTimer = 0f;
		CancelInvoke(nameof(StartFollowing));

		if (myRb != null)
		{
			myRb.linearVelocity = Vector3.zero;
			myRb.angularVelocity = Vector3.zero;
			myRb.isKinematic = true;
			myRb.useGravity = false;
		}

		EnableCollisionsForAttack();
	}

	private void BeginPostImpactRetreat()
	{
		isPostImpactRetreat = true;
		isFollowingCamera = false;
		postImpactTimer = postImpactDuration;
		currentRetreatSpeed = retreatMinSpeed;
		CancelInvoke(nameof(StartFollowing));

		if (myRb != null)
		{
			myRb.linearVelocity = Vector3.zero;
			myRb.angularVelocity = Vector3.zero;
			myRb.isKinematic = true;
			myRb.useGravity = false;
		}
	}

	private void UpdatePostImpactRetreat()
	{
		if (target == null || targetRb == null)
		{
			StayOnScreen();
			return;
		}

		postImpactTimer -= Time.deltaTime;

		float bossSpeedX = Mathf.Max(targetRb.linearVelocity.x, 0f);
		float desiredRetreatSpeed = retreatMinSpeed + (bossSpeedX * retreatCatchupBonus * 0.1f);
		currentRetreatSpeed = Mathf.Lerp(currentRetreatSpeed, desiredRetreatSpeed, Time.deltaTime * retreatSpeedSmoothing);

		Vector3 desiredPos = target.position + new Vector3(-retreatFollowDistance, retreatHeightOffset, 0f);
		transform.position = Vector3.MoveTowards(transform.position, desiredPos, currentRetreatSpeed * Time.deltaTime);

		if (postImpactTimer <= 0f)
		{
			StayOnScreen();
		}
	}

	private void DisableCollisionsAfterImpact()
	{
		Collider[] colliders = GetComponentsInChildren<Collider>();
		foreach (Collider col in colliders)
		{
			col.enabled = false;
		}
	}

	private void EnableCollisionsForAttack()
	{
		Collider[] colliders = GetComponentsInChildren<Collider>();
		foreach (Collider col in colliders)
		{
			col.enabled = true;
		}
	}

	private void StartMoveCooldownOnImpact()
	{
		if (selectedMove == null)
		{
			Debug.LogWarning($"{name} could not start cooldown on impact because selectedMove is null.");
			return;
		}

		if (ownerSummoner == null)
		{
			ownerSummoner = FindFirstObjectByType<HeroSummoner>();
		}

		if (ownerSummoner == null)
		{
			Debug.LogWarning($"{name} could not start cooldown on impact because no HeroSummoner was found.");
			return;
		}

		if (ownerHeroIndex < 0)
		{
			Debug.LogWarning($"{name} could not start cooldown on impact because ownerHeroIndex is invalid.");
			return;
		}

		ownerSummoner.StartCooldownFromImpact(ownerHeroIndex, selectedMove.cooldown);
	}

	void StartFollowing()
	{
		if (myRb != null)
		{
			myRb.isKinematic = true;
			myRb.useGravity = false;
		}
		isFollowingCamera = true;
	}

	private void PlayCharacterHitSound()
	{
		if (characterHitSounds == null || characterHitSounds.Length == 0) return;

		int randomIndex = Random.Range(0, characterHitSounds.Length);

		// Randomize pitch to make repetitive hits sound more natural
		localAudioSource.pitch = Random.Range(0.9f, 1.1f);

		// FIX: Added 'hitVolume' as the second parameter to control loudness
		localAudioSource.PlayOneShot(characterHitSounds[randomIndex], hitVolume);
	}

	public void TriggerHitSound(AudioSource source)
	{
		if (source != null && punchSound != null)
		{
			// FIX: Ensure the punchVolume is actually being applied here
			source.PlayOneShot(punchSound, punchVolume);
		}
	}
}
