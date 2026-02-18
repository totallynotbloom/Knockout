using System;
using UnityEngine;
// Explicitly tell C# to use Unity's Random to avoid ambiguity errors with System.Random
using Random = UnityEngine.Random;

/// <summary>
/// This script controls the individual behavior of a summoned hero.
/// It handles moving toward the boss, dealing damage, and the "Regroup" behavior after attacking.
/// </summary>
public class MVPHero : MonoBehaviour
{
	[System.Serializable]
	public class MoveProfile
	{
		public string moveName;        // The name of the attack
		public Vector2 hitForceXY;     // X = Knockback, Y = Launch height
		public float cooldown = 5f;    // How long before this hero can be used again
		public float approachSpeed = 15f; // How fast the hero flies at the boss
		public float giveUpTime = 4f;  // Fail-safe: Hero despawns if they don't hit the boss in this time
	}

	[Header("Move Configurations")]
	public MoveProfile move1;
	public MoveProfile move2;

	[HideInInspector] public MoveProfile selectedMove; // The specific move chosen by the player

	private float currentApproachSpeed;
	private float currentGiveUpTime;

	private Transform target;          // The Boss's transform
	private Rigidbody targetRb;        // The Boss's physics body
	private bool hasHit = false;       // Prevents the hero from hitting the boss multiple times
	private float lifeTimer = 0f;      // Tracks how long the hero has been alive

	[Header("Regroup Settings")]
	private bool isFollowingCamera = false; // Does the hero glide behind the camera after hitting?
	private Transform camTransform;
	private float randomXOffset;       // Random position behind the camera so heroes don't overlap
	private float randomYOffset;

	[Header("Damage Number Settings")]
	public GameObject damageNumberPrefab; // The "10" or "20" text that pops up
	public Transform worldCanvas;

	[Header("Movement Settings")]
	public float currentSpeedMultiplier = 1f;

	[Header("Parry Duel Settings")]
	public bool isProwling = false;    // True if the hero is waiting for the player to hit the Parry button
	public float prowlDistance = 5f;   // How far away from the boss they wait

	[HideInInspector] public float currentPowerMultiplier = 1f; // Used for buffs or upgrades

	private Rigidbody myRb;

	/// <summary>
	/// Setup the hero's target and stats based on the player's choice.
	/// </summary>
	public void Initialize(Transform slushTarget, int moveNumber)
	{
		target = slushTarget;
		if (target != null) targetRb = target.GetComponent<Rigidbody>();

		myRb = GetComponent<Rigidbody>();
		camTransform = Camera.main.transform;

		// Assign the correct move based on player input
		selectedMove = (moveNumber == 1) ? move1 : move2;

		currentApproachSpeed = selectedMove.approachSpeed;
		currentGiveUpTime = selectedMove.giveUpTime;

		// Randomize the regroup position so multiple heroes look like a squad
		randomXOffset = Random.Range(-25f, -15f);
		randomYOffset = Random.Range(-2f, 2f);
	}

	void Update()
	{
		if (target == null || targetRb == null) return;

		// --- ATTACK STATE ---
		if (!hasHit)
		{
			lifeTimer += Time.deltaTime;

			// Fail-safe: if the boss is moving too fast and we can't catch him, give up
			if (lifeTimer >= currentGiveUpTime)
			{
				GiveUp();
				return;
			}

			// 1. PARRY PROWL LOGIC: Stay at a fixed distance until the parry button is hit
			if (isProwling)
			{
				Vector3 standoffPos = target.position + new Vector3(-prowlDistance, 0, 0);
				transform.position = Vector3.Lerp(transform.position, standoffPos, Time.deltaTime * 5f);
				return;
			}

			// 2. HIGH SPEED PROXIMITY CHECK: If moving super fast, trigger hit earlier
			float distanceToBoss = Vector3.Distance(transform.position, target.position);
			if (currentSpeedMultiplier > 1f && distanceToBoss < 2.0f)
			{
				HandleManualHit(target.gameObject);
				return;
			}

			// 3. NORMAL MOVEMENT: Match the boss's speed + approach speed
			float bossSpeedX = targetRb.linearVelocity.x;
			float totalSpeed = (Mathf.Max(bossSpeedX, 0) + currentApproachSpeed) * currentSpeedMultiplier;

			transform.position = Vector3.MoveTowards(transform.position, target.position, totalSpeed * Time.deltaTime);
		}
		// --- REGROUP STATE ---
		else if (isFollowingCamera)
		{
			// Glide to a position behind the player's view
			Vector3 regroupPos = new Vector3(camTransform.position.x + randomXOffset, target.position.y + randomYOffset, 0f);
			transform.position = Vector3.Lerp(transform.position, regroupPos, Time.deltaTime * 3f);
		}
	}

	/// <summary>
	/// Called by HeroSummoner when the player successfully hits a Parry.
	/// </summary>
	public void TriggerParryExit()
	{
		isProwling = false;
		hasHit = true;

		// Create a massive time-freeze effect for the success
		if (HitStopManager.Instance != null)
			HitStopManager.Instance.TriggerVariableHitStop(2000f);

		if (myRb != null)
		{
			myRb.isKinematic = false;
			myRb.useGravity = false;
			myRb.linearVelocity = new Vector3(-20f, 5f, 0f); // Fly backward stylishly
		}

		Destroy(gameObject, 1.5f); // Cleanup
	}

	/// <summary>
	/// If the hero misses or takes too long, they go back on cooldown and regroup.
	/// </summary>
	void GiveUp()
	{
		hasHit = true;
		HeroSummoner summoner = FindFirstObjectByType<HeroSummoner>();
		if (summoner != null) summoner.ResetHeroCooldown(this.gameObject);
		StayOnScreen();
	}

	/// <summary>
	/// The core "Attack" logic. Calculates damage and applies force to the Boss.
	/// </summary>
	void HandleManualHit(GameObject bossObj)
	{
		if (hasHit) return;
		hasHit = true;

		if (bossObj.TryGetComponent<BossHealth>(out BossHealth boss))
		{
			// Calculate damage based on the move's physical force
			float damageCalculated = (selectedMove.hitForceXY.magnitude * 0.5f) * currentPowerMultiplier;

			// Deal damage to boss and trigger a Hit-Stop based on the impact
			float impactForce = selectedMove.hitForceXY.magnitude * currentPowerMultiplier;
			boss.TakeDamage(damageCalculated, impactForce);

			// SPAWN DAMAGE NUMBER:
			if (damageNumberPrefab != null && worldCanvas != null)
			{
				Vector3 spawnPos = bossObj.transform.position + new Vector3(0, 2f, -1f);
				GameObject dn = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity, worldCanvas);
				if (dn.TryGetComponent<DamageNumber>(out DamageNumber dnScript)) dnScript.SetText(damageCalculated);
			}

			// APPLY PHYSICS FORCE:
			Rigidbody bossRb = bossObj.GetComponent<Rigidbody>();
			if (bossRb != null)
			{
				Vector3 force = new Vector3(selectedMove.hitForceXY.x, selectedMove.hitForceXY.y, 0);
				// If armor is broken, the boss is twice as easy to knock around!
				float armorBonus = boss.isArmorBroken ? 2f : 1f;
				bossRb.AddForce(force * armorBonus * currentPowerMultiplier, ForceMode.Impulse);
			}
		}
		StayOnScreen();
	}

	// Uses Trigger for standard collision detection
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && !hasHit)
		{
			HandleManualHit(other.gameObject);
		}
	}

	/// <summary>
	/// After hitting, turn the hero into a physics object so they fall/tumble briefly.
	/// </summary>
	void StayOnScreen()
	{
		if (TryGetComponent<Collider>(out Collider col)) col.isTrigger = false;
		if (myRb != null)
		{
			myRb.isKinematic = false;
			myRb.useGravity = true;
			myRb.AddForce(new Vector3(-2, 3, 0), ForceMode.Impulse);
		}
		Invoke("StartFollowing", 1.5f); // Transition to the glide state after 1.5s
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
}