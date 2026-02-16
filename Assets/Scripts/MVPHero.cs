using System;
using UnityEngine;
// Explicitly tell C# to use Unity's Random to avoid ambiguity errors
using Random = UnityEngine.Random;

public class MVPHero : MonoBehaviour
{
	[System.Serializable]
	public class MoveProfile
	{
		public string moveName;
		public Vector2 hitForceXY;
		public float cooldown = 5f;
		public float approachSpeed = 15f;
		public float giveUpTime = 4f;
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

	[Header("Regroup Settings")]
	private bool isFollowingCamera = false;
	private Transform camTransform;
	private float randomXOffset;
	private float randomYOffset;

	[Header("Damage Number Settings")]
	public GameObject damageNumberPrefab;
	public Transform worldCanvas;

	[Header("Movement Settings")]
	public float currentSpeedMultiplier = 1f;

	[Header("Parry Duel Settings")]
	public bool isProwling = false;
	public float prowlDistance = 5f;

	[HideInInspector] public float currentPowerMultiplier = 1f;

	private Rigidbody myRb;

	public void Initialize(Transform slushTarget, int moveNumber)
	{
		target = slushTarget;
		if (target != null) targetRb = target.GetComponent<Rigidbody>();

		myRb = GetComponent<Rigidbody>();
		camTransform = Camera.main.transform;

		selectedMove = (moveNumber == 1) ? move1 : move2;

		currentApproachSpeed = selectedMove.approachSpeed;
		currentGiveUpTime = selectedMove.giveUpTime;

		// Uses our 'using Random' alias from the top
		randomXOffset = Random.Range(-25f, -15f);
		randomYOffset = Random.Range(-2f, 2f);
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

			// 1. PARRY PROWL LOGIC
			if (isProwling)
			{
				Vector3 standoffPos = target.position + new Vector3(-prowlDistance, 0, 0);
				transform.position = Vector3.Lerp(transform.position, standoffPos, Time.deltaTime * 5f);
				return;
			}

			// 2. HIGH SPEED PROXIMITY CHECK
			float distanceToBoss = Vector3.Distance(transform.position, target.position);
			if (currentSpeedMultiplier > 1f && distanceToBoss < 2.0f)
			{
				HandleManualHit(target.gameObject);
				return;
			}

			// 3. NORMAL MOVEMENT LOGIC
			// Use linearVelocity for Unity 6, or velocity for older versions
			float bossSpeedX = targetRb.linearVelocity.x;
			float totalSpeed = (Mathf.Max(bossSpeedX, 0) + currentApproachSpeed) * currentSpeedMultiplier;

			transform.position = Vector3.MoveTowards(transform.position, target.position, totalSpeed * Time.deltaTime);
		}
		else if (isFollowingCamera)
		{
			Vector3 regroupPos = new Vector3(camTransform.position.x + randomXOffset, target.position.y + randomYOffset, 0f);
			transform.position = Vector3.Lerp(transform.position, regroupPos, Time.deltaTime * 3f);
		}
	}

	public void TriggerParryExit()
	{
		isProwling = false;
		hasHit = true;

		// Trigger a manual massive hit-stop for the parry blast
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
		HeroSummoner summoner = FindFirstObjectByType<HeroSummoner>();
		if (summoner != null) summoner.ResetHeroCooldown(this.gameObject);
		StayOnScreen();
	}

	void HandleManualHit(GameObject bossObj)
	{
		if (hasHit) return;
		hasHit = true;

		if (bossObj.TryGetComponent<BossHealth>(out BossHealth boss))
		{
			float damageCalculated = (selectedMove.hitForceXY.magnitude * 0.5f) * currentPowerMultiplier;

			// PASS THE FORCE: We pass the magnitude of hitForceXY to trigger the scaled hit-stop
			float impactForce = selectedMove.hitForceXY.magnitude * currentPowerMultiplier;
			boss.TakeDamage(damageCalculated, impactForce);

			if (damageNumberPrefab != null && worldCanvas != null)
			{
				Vector3 spawnPos = bossObj.transform.position + new Vector3(0, 2f, -1f);
				GameObject dn = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity, worldCanvas);
				if (dn.TryGetComponent<DamageNumber>(out DamageNumber dnScript)) dnScript.SetText(damageCalculated);
			}

			Rigidbody bossRb = bossObj.GetComponent<Rigidbody>();
			if (bossRb != null)
			{
				Vector3 force = new Vector3(selectedMove.hitForceXY.x, selectedMove.hitForceXY.y, 0);
				float armorBonus = boss.isArmorBroken ? 2f : 1f;
				bossRb.AddForce(force * armorBonus * currentPowerMultiplier, ForceMode.Impulse);
			}
		}
		StayOnScreen();
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
		if (TryGetComponent<Collider>(out Collider col)) col.isTrigger = false;
		if (myRb != null)
		{
			myRb.isKinematic = false;
			myRb.useGravity = true;
			myRb.AddForce(new Vector3(-2, 3, 0), ForceMode.Impulse);
		}
		Invoke("StartFollowing", 1.5f);
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