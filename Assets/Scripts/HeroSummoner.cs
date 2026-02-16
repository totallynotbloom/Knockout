using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using System.Collections;

public class HeroSummoner : MonoBehaviour
{
	public List<HeroData> heroList;
	public TacticalManager tacticalManager;
	public Transform slushTarget;

	[Header("UI Bridge")]
	public Transform worldCanvas;

	[Header("Squad UI Visuals")]
	public Image[] cooldownOverlays;
	public TMP_Text[] timerTexts;

	[Header("Move Selection UI")]
	public GameObject[] moveSelectionPanels;
	public TMP_Text[] move1Names;
	public TMP_Text[] move2Names;

	[Header("Parry Settings")]
	public float parryWindow = 1.0f;
	private bool isParryActive = false;
	private HeroData pendingHero;
	private int pendingMove;
	public GameObject counterUIText;
	[Range(0, 100)]
	public float parryChance = 20f;

	[Header("Cinematic Settings")]
	public float cinematicZoomSize = 4f;
	public float cameraReturnSpeed = 1.5f; // LOWER = Slower glide back
	public CinemachineCamera virtualCamera;
	private float originalCameraSize;
	private bool isCinematicFocus = false;
	private bool isReturningToNormal = false; // The "Glide" state

	[Header("New Parry Settings")]
	public GameObject parrySuccessUI;
	public GameObject parryFailUI;
	public float parryDamage = 20f;
	public Vector2 parryLaunchForce = new Vector2(30f, 30f);

	private Vector3 cameraTargetPos;
	private bool isCommandMode = false;

	void Start()
	{
		if (Camera.main != null)
			originalCameraSize = Camera.main.orthographicSize;

		if (counterUIText != null) counterUIText.SetActive(false);
		if (parrySuccessUI != null) parrySuccessUI.SetActive(false);
		if (parryFailUI != null) parryFailUI.SetActive(false);
	}

	void Update()
	{
		var keyboard = Keyboard.current;
		if (keyboard == null) return;

		// Parry Keys: V or E
		bool parryKeyPressed = keyboard.vKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame;

		if (isParryActive && parryKeyPressed)
		{
			HandleParrySuccess();
		}
		else if (!isParryActive && keyboard.spaceKey.wasPressedThisFrame)
		{
			if (!isCommandMode)
			{
				if (AnyHeroReady()) EnterTactical();
			}
			else
			{
				ExitTactical();
			}
		}

		UpdateCooldownUI();

		if (isCommandMode)
		{
			if (keyboard.digit1Key.wasPressedThisFrame) { if (AttemptSummon(0, 1)) ExitTactical(); }
			if (keyboard.digit2Key.wasPressedThisFrame) { if (AttemptSummon(0, 2)) ExitTactical(); }
			if (keyboard.digit3Key.wasPressedThisFrame) { if (AttemptSummon(1, 1)) ExitTactical(); }
			if (keyboard.digit4Key.wasPressedThisFrame) { if (AttemptSummon(1, 2)) ExitTactical(); }
		}
	}

	void LateUpdate()
	{
		if (isCinematicFocus)
		{
			if (virtualCamera != null) virtualCamera.enabled = false;

			if (isParryActive && pendingHero != null && pendingHero.activeInstance != null)
			{
				// 1. Calculate the Midpoint
				cameraTargetPos = (pendingHero.activeInstance.transform.position + slushTarget.position) / 2f;

				// 2. Calculate Distance to see if we need to zoom out more
				float distBetweenPoints = Vector3.Distance(pendingHero.activeInstance.transform.position, slushTarget.position);

				// If they are far apart, increase zoom size dynamically. 
				// 0.6f is a "buffer" to keep them away from the very edge of the screen.
				float dynamicZoom = Mathf.Max(cinematicZoomSize, distBetweenPoints * 0.6f);

				// 3. Move the Camera
				float distToTarget = Vector3.Distance(Camera.main.transform.position, cameraTargetPos);
				float followSpeed = distToTarget > 20f ? distToTarget * 2f : 4f;

				cameraTargetPos.z = -10f;
				Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, cameraTargetPos, Time.unscaledDeltaTime * followSpeed);

				// Use the dynamicZoom instead of just cinematicZoomSize
				Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, dynamicZoom, Time.unscaledDeltaTime * 4f);
			}
			else
			{
				// Focus on Boss only (after parry success)
				cameraTargetPos = slushTarget.position;
				cameraTargetPos.z = -10f;
				Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, cameraTargetPos, Time.unscaledDeltaTime * 4f);
				Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, cinematicZoomSize, Time.unscaledDeltaTime * 4f);
			}
		}

		// STATE 2: Gliding back to the main game view
		else if (isReturningToNormal)
		{
			Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, originalCameraSize, Time.unscaledDeltaTime * cameraReturnSpeed);

			// Return to original height/position if needed, or let Cinemachine take over
			if (Mathf.Abs(Camera.main.orthographicSize - originalCameraSize) < 0.1f)
			{
				isReturningToNormal = false;
				if (virtualCamera != null) virtualCamera.enabled = true;
			}
		}
	}

	public void ResetHeroCooldown(GameObject heroInstance)
	{
		foreach (var hero in heroList)
		{
			if (hero.activeInstance == heroInstance)
			{
				hero.move1.currentTimer = 0;
				hero.move1.isReady = true;
				hero.move2.currentTimer = 0;
				hero.move2.isReady = true;
				break;
			}
		}
	}

	void TriggerBossCounter(HeroData hero, int moveNumber)
	{
		isParryActive = true;
		isCinematicFocus = true;
		isReturningToNormal = false;
		pendingHero = hero;
		pendingMove = moveNumber;

		if (hero.activeInstance != null) Destroy(hero.activeInstance);
		hero.activeInstance = SpawnHero(hero.prefab, moveNumber);

		if (hero.activeInstance.TryGetComponent(out MVPHero heroScript))
		{
			heroScript.isProwling = true;
		}

		if (counterUIText != null)
		{
			counterUIText.SetActive(true);
			counterUIText.transform.localScale = Vector3.one;
		}

		Invoke(nameof(HandleParryFail), parryWindow);
	}

	void HandleParrySuccess()
	{
		CancelInvoke(nameof(HandleParryFail));

		if (counterUIText != null) counterUIText.SetActive(false);
		if (parrySuccessUI != null) StartCoroutine(ShowParryStatusUI(parrySuccessUI));

		if (slushTarget != null)
		{
			cameraTargetPos = slushTarget.position;

			if (slushTarget.TryGetComponent(out BossHealth boss))
			{
				// We pass 'true' at the end to signal this is a Parry for a longer HitStop
				boss.TakeDamage(parryDamage, parryLaunchForce.magnitude, true);
			}

			if (slushTarget.TryGetComponent(out Rigidbody bossRb))
			{
				// Convert your Vector2 parryLaunchForce into a Vector3 for the physics engine
				Vector3 forceToApply = new Vector3(parryLaunchForce.x, parryLaunchForce.y, 0f);
				bossRb.AddForce(forceToApply, ForceMode.Impulse);
			}
		}

		if (pendingHero != null && pendingHero.activeInstance != null)
		{
			pendingHero.move1.currentTimer = 3f;
			pendingHero.move1.isReady = false;
			pendingHero.move2.currentTimer = 3f;
			pendingHero.move2.isReady = false;

			if (pendingHero.activeInstance.TryGetComponent(out MVPHero hScript))
				hScript.TriggerParryExit();
		}

		isParryActive = false;

		// This delay allows the camera to glide to the BOSS before zooming out
		Invoke(nameof(StartCameraReturn), 1.0f);
	}

	void StartCameraReturn()
	{
		isCinematicFocus = false;
		isReturningToNormal = true;
	}

	void HandleParryFail()
	{
		if (!isParryActive) return;

		isParryActive = false;
		if (counterUIText != null) counterUIText.SetActive(false);
		if (parryFailUI != null) StartCoroutine(ShowParryStatusUI(parryFailUI));

		if (pendingHero != null && pendingHero.activeInstance != null)
			Destroy(pendingHero.activeInstance);

		pendingHero.move1.currentTimer = 15f;
		pendingHero.move1.isReady = false;
		pendingHero.move2.currentTimer = 15f;
		pendingHero.move2.isReady = false;

		StartCameraReturn();
	}

	IEnumerator ShowParryStatusUI(GameObject uiElement)
	{
		if (uiElement == null) yield break;
		uiElement.SetActive(true);
		uiElement.transform.localScale = Vector3.one;
		yield return new WaitForSecondsRealtime(2.0f);
		uiElement.SetActive(false);
	}

	bool AttemptSummon(int heroIndex, int moveNumber)
	{
		if (heroIndex >= heroList.Count) return false;
		HeroData hero = heroList[heroIndex];
		if (hero.prefab == null) return false;
		HeroMove moveState = (moveNumber == 1) ? hero.move1 : hero.move2;
		if (moveState.isReady)
		{
			if (UnityEngine.Random.Range(0f, 100f) < parryChance)
			{
				TriggerBossCounter(hero, moveNumber);
				return true;
			}
			ExecuteSummon(hero, moveNumber, 1f);
			return true;
		}
		return false;
	}

	void ExecuteSummon(HeroData hero, int moveNumber, float powerMultiplier)
	{
		if (hero.activeInstance != null) Destroy(hero.activeInstance);
		hero.activeInstance = SpawnHero(hero.prefab, moveNumber);
		if (hero.activeInstance.TryGetComponent<MVPHero>(out MVPHero heroScript))
		{
			heroScript.currentPowerMultiplier = 1f;
			heroScript.currentSpeedMultiplier = 1f;
			float cd = heroScript.selectedMove.cooldown;
			hero.move1.currentTimer = cd;
			hero.move1.isReady = false;
			hero.move2.currentTimer = cd;
			hero.move2.isReady = false;
		}
	}

	void UpdateCooldownUI()
	{
		for (int i = 0; i < heroList.Count; i++)
		{
			if (heroList[i] == null) continue;
			HeroMove m1 = heroList[i].move1;
			HeroMove m2 = heroList[i].move2;

			// CHANGE: Use Time.deltaTime instead of unscaledDeltaTime.
			// If timeScale is 0.1 (10% speed), cooldowns now progress 10x slower.
			if (!m1.isReady) m1.currentTimer -= Time.deltaTime;
			if (!m2.isReady) m2.currentTimer -= Time.deltaTime;

			if (m1.currentTimer <= 0) m1.isReady = true;
			if (m2.currentTimer <= 0) m2.isReady = true;

			if (i < cooldownOverlays.Length && cooldownOverlays[i] != null)
			{
				bool heroBusy = !m1.isReady || !m2.isReady;
				cooldownOverlays[i].gameObject.SetActive(heroBusy);
				if (timerTexts[i] != null)
				{
					// Mathf.Max gets the highest timer, F0 removes decimals for a clean UI
					timerTexts[i].text = heroBusy ? Mathf.Max(m1.currentTimer, m2.currentTimer).ToString("F0") : "";
				}
			}
		}
	}

	GameObject SpawnHero(GameObject prefab, int moveNumber)
	{
		if (prefab == null || slushTarget == null) return null;
		Camera mainCam = Camera.main;
		Vector3 screenLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0.5f, 10f));
		Vector3 spawnPos = new Vector3(screenLeft.x - 2f, slushTarget.position.y + Random.Range(-1f, 1f), -0.5f);
		GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
		if (go.TryGetComponent<MVPHero>(out MVPHero heroScript))
		{
			heroScript.Initialize(slushTarget, moveNumber);
			heroScript.worldCanvas = worldCanvas;
		}
		return go;
	}

	bool AnyHeroReady()
	{
		foreach (var hero in heroList)
			if (hero.move1.isReady || hero.move2.isReady) return true;
		return false;
	}

	void EnterTactical() { isCommandMode = true; if (tacticalManager != null) tacticalManager.EnterSlowMo(); ToggleMovePanels(true); }
	void ExitTactical() { isCommandMode = false; if (tacticalManager != null) tacticalManager.ExitSlowMo(); ToggleMovePanels(false); }

	void ToggleMovePanels(bool show)
	{
		for (int i = 0; i < moveSelectionPanels.Length; i++)
		{
			if (moveSelectionPanels[i] == null) continue;
			if (i < heroList.Count)
			{
				moveSelectionPanels[i].SetActive(show);
				if (show && heroList[i].prefab.TryGetComponent(out MVPHero p))
				{
					move1Names[i].text = (i == 0 ? "1: " : "3: ") + p.move1.moveName;
					move2Names[i].text = (i == 0 ? "2: " : "4: ") + p.move2.moveName;
				}
			}
			else moveSelectionPanels[i].SetActive(false);
		}
	}
}