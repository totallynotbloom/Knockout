using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// This script is the heart of the player's interaction. 
/// It handles summoning heroes, slowing down time for tactics, 
/// and managing the cinematic "Parry" camera system.
/// </summary>
public class HeroSummoner : MonoBehaviour
{
	// --- CORE DATA ---
	public List<HeroData> heroList;        // List of all heroes we can summon
	public TacticalManager tacticalManager; // References the script that slows down time
	public Transform slushTarget;          // The Boss (usually the target for all heroes)

	[Header("UI Bridge")]
	public Transform worldCanvas;          // The canvas used for health bars over heroes' heads

	[Header("Squad UI Visuals")]
	public Image[] cooldownOverlays;       // Dark images that cover hero icons during cooldown
	public TMP_Text[] timerTexts;          // Numbers showing how many seconds are left

	[Header("Move Selection UI")]
	public GameObject[] moveSelectionPanels; // The little menus that pop up in Tactical Mode
	public TMP_Text[] move1Names;
	public TMP_Text[] move2Names;

	[Header("Parry Settings")]
	public float parryWindow = 1.0f;       // How long the player has to hit the button
	private bool isParryActive = false;    // Are we currently in a parry sequence?
	private HeroData pendingHero;          // The hero involved in the current parry
	private int pendingMove;
	public GameObject counterUIText;       // "COUNTER!" text object
	[Range(0, 100)]
	public float parryChance = 20f;        // % chance a summon triggers a parry instead of a normal attack

	[Header("Cinematic Settings")]
	public float cinematicZoomSize = 4f;   // How close the camera gets during a parry
	public float cameraReturnSpeed = 1.5f; // How fast the camera glides back to normal view
	public CinemachineCamera virtualCamera; // The Cinemachine brain (we disable this during manual pans)
	private float originalCameraSize;      // Stores the default zoom level
	private bool isCinematicFocus = false; // Is the camera currently doing a manual cinematic pan?
	private bool isReturningToNormal = false; // Is the camera currently "gliding" back?

	[Header("New Parry Settings")]
	public GameObject parrySuccessUI;      // Visual feedback for hitting the parry
	public GameObject parryFailUI;         // Visual feedback for missing
	public float parryDamage = 20f;        // Damage dealt to boss on success
	public Vector2 parryLaunchForce = new Vector2(30f, 30f); // How hard the boss gets knocked back

	private Vector3 cameraTargetPos;       // Where the camera wants to go
	private bool isCommandMode = false;    // Are we currently in the Slow-Mo menu?

	void Start()
	{
		// Save the default camera zoom so we know what to return to
		if (Camera.main != null)
			originalCameraSize = Camera.main.orthographicSize;

		// Hide all feedback UI by default
		if (counterUIText != null) counterUIText.SetActive(false);
		if (parrySuccessUI != null) parrySuccessUI.SetActive(false);
		if (parryFailUI != null) parryFailUI.SetActive(false);
	}

	void Update()
	{
		var keyboard = Keyboard.current;
		if (keyboard == null) return;

		// Parry Keys: V or E (Players can choose their favorite)
		bool parryKeyPressed = keyboard.vKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame;

		// If a parry is active, check for the button press
		if (isParryActive && parryKeyPressed)
		{
			HandleParrySuccess();
		}
		// Open/Close the Tactical Menu with Space
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

		// Always keep the cooldown UI updated
		UpdateCooldownUI();

		// Handle Number Key inputs (1-4) while in Command Mode
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
		// CINEMATIC MODE: We manually control the camera position and zoom
		if (isCinematicFocus)
		{
			// Turn off Cinemachine so it doesn't fight our manual movement
			if (virtualCamera != null) virtualCamera.enabled = false;

			if (isParryActive && pendingHero != null && pendingHero.activeInstance != null)
			{
				// Find the center point between the hero and the boss
				cameraTargetPos = (pendingHero.activeInstance.transform.position + slushTarget.position) / 2f;

				// Adjust zoom based on how far apart they are
				float distBetweenPoints = Vector3.Distance(pendingHero.activeInstance.transform.position, slushTarget.position);
				float dynamicZoom = Mathf.Max(cinematicZoomSize, distBetweenPoints * 0.6f);

				// Move the camera smoothly to the midpoint
				cameraTargetPos.z = -10f; // Keep the camera at the correct Z depth
				Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, cameraTargetPos, Time.unscaledDeltaTime * 4f);

				// Apply the zoom
				Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, dynamicZoom, Time.unscaledDeltaTime * 4f);
			}
			else
			{
				// If the parry is over but we are still focused, look only at the boss
				cameraTargetPos = slushTarget.position;
				cameraTargetPos.z = -10f;
				Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, cameraTargetPos, Time.unscaledDeltaTime * 4f);
				Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, cinematicZoomSize, Time.unscaledDeltaTime * 4f);
			}
		}
		// RETURN MODE: Smoothly transition back to the standard gameplay view
		else if (isReturningToNormal)
		{
			Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, originalCameraSize, Time.unscaledDeltaTime * cameraReturnSpeed);

			// Once we are close enough to the original size, hand control back to Cinemachine
			if (Mathf.Abs(Camera.main.orthographicSize - originalCameraSize) < 0.1f)
			{
				isReturningToNormal = false;
				if (virtualCamera != null) virtualCamera.enabled = true;
			}
		}
	}

	// --- PUBLIC HELPER: Instantly refreshes a hero's cooldowns ---
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

	// --- PARRY SYSTEM: Initiation ---
	void TriggerBossCounter(HeroData hero, int moveNumber)
	{
		isParryActive = true;
		isCinematicFocus = true;
		isReturningToNormal = false;
		pendingHero = hero;
		pendingMove = moveNumber;

		// Clean up old instance and spawn the new one for the parry animation
		if (hero.activeInstance != null) Destroy(hero.activeInstance);
		hero.activeInstance = SpawnHero(hero.prefab, moveNumber);

		// Tell the hero script they are in "Parry Mode"
		if (hero.activeInstance.TryGetComponent(out MVPHero heroScript))
		{
			heroScript.isProwling = true;
		}

		// Flash the "Counter" prompt
		if (counterUIText != null)
		{
			counterUIText.SetActive(true);
			counterUIText.transform.localScale = Vector3.one;
		}

		// Start the countdown. If the player doesn't hit the key in time, they fail.
		Invoke(nameof(HandleParryFail), parryWindow);
	}

	// --- PARRY SYSTEM: Success ---
	void HandleParrySuccess()
	{
		CancelInvoke(nameof(HandleParryFail)); // Stop the failure timer

		if (counterUIText != null) counterUIText.SetActive(false);
		if (parrySuccessUI != null) StartCoroutine(ShowParryStatusUI(parrySuccessUI));

		if (slushTarget != null)
		{
			// Deal damage and apply physical knockback to the boss
			if (slushTarget.TryGetComponent(out BossHealth boss))
			{
				boss.TakeDamage(parryDamage, parryLaunchForce.magnitude, true);
			}

			if (slushTarget.TryGetComponent(out Rigidbody bossRb))
			{
				Vector3 forceToApply = new Vector3(parryLaunchForce.x, parryLaunchForce.y, 0f);
				bossRb.AddForce(forceToApply, ForceMode.Impulse);
			}
		}

		// Set a short penalty cooldown so they can't spam parries
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
		Invoke(nameof(StartCameraReturn), 1.0f); // Wait a second to enjoy the hit before zooming out
	}

	void StartCameraReturn()
	{
		isCinematicFocus = false;
		isReturningToNormal = true;
	}

	// --- PARRY SYSTEM: Failure ---
	void HandleParryFail()
	{
		if (!isParryActive) return;

		isParryActive = false;
		if (counterUIText != null) counterUIText.SetActive(false);
		if (parryFailUI != null) StartCoroutine(ShowParryStatusUI(parryFailUI));

		// Remove the hero and give them a massive cooldown penalty
		if (pendingHero != null && pendingHero.activeInstance != null)
			Destroy(pendingHero.activeInstance);

		pendingHero.move1.currentTimer = 15f;
		pendingHero.move1.isReady = false;
		pendingHero.move2.currentTimer = 15f;
		pendingHero.move2.isReady = false;

		StartCameraReturn();
	}

	// --- UI: Temporary Status Popups ---
	IEnumerator ShowParryStatusUI(GameObject uiElement)
	{
		if (uiElement == null) yield break;
		uiElement.SetActive(true);
		uiElement.transform.localScale = Vector3.one;
		yield return new WaitForSecondsRealtime(2.0f);
		uiElement.SetActive(false);
	}

	// --- SUMMONING: The Decision Phase ---
	bool AttemptSummon(int heroIndex, int moveNumber)
	{
		if (heroIndex >= heroList.Count) return false;
		HeroData hero = heroList[heroIndex];
		if (hero.prefab == null) return false;

		HeroMove moveState = (moveNumber == 1) ? hero.move1 : hero.move2;

		if (moveState.isReady)
		{
			// RNG check: Do we trigger a Parry or a normal attack?
			if (UnityEngine.Random.Range(0f, 100f) < parryChance)
			{
				TriggerBossCounter(hero, moveNumber);
				return true;
			}

			// Normal attack
			ExecuteSummon(hero, moveNumber, 1f);
			return true;
		}
		return false;
	}

	// --- SUMMONING: The Spawning Phase ---
	void ExecuteSummon(HeroData hero, int moveNumber, float powerMultiplier)
	{
		if (hero.activeInstance != null) Destroy(hero.activeInstance);
		hero.activeInstance = SpawnHero(hero.prefab, moveNumber);

		if (hero.activeInstance.TryGetComponent<MVPHero>(out MVPHero heroScript))
		{
			heroScript.currentPowerMultiplier = 1f;
			heroScript.currentSpeedMultiplier = 1f;

			// Put the hero on cooldown based on the move they used
			float cd = heroScript.selectedMove.cooldown;
			hero.move1.currentTimer = cd;
			hero.move1.isReady = false;
			hero.move2.currentTimer = cd;
			hero.move2.isReady = false;
		}
	}

	// --- UI: Cooldown Visuals ---
	void UpdateCooldownUI()
	{
		for (int i = 0; i < heroList.Count; i++)
		{
			if (heroList[i] == null) continue;
			HeroMove m1 = heroList[i].move1;
			HeroMove m2 = heroList[i].move2;

			// Cooldowns tick down based on game time (slower during Tactical mode)
			if (!m1.isReady) m1.currentTimer -= Time.deltaTime;
			if (!m2.isReady) m2.currentTimer -= Time.deltaTime;

			if (m1.currentTimer <= 0) m1.isReady = true;
			if (m2.currentTimer <= 0) m2.isReady = true;

			// Update the dark "Fill" image and the text timer
			if (i < cooldownOverlays.Length && cooldownOverlays[i] != null)
			{
				bool heroBusy = !m1.isReady || !m2.isReady;
				cooldownOverlays[i].gameObject.SetActive(heroBusy);
				if (timerTexts[i] != null)
				{
					timerTexts[i].text = heroBusy ? Mathf.Max(m1.currentTimer, m2.currentTimer).ToString("F0") : "";
				}
			}
		}
	}

	// --- UTILITY: The actual instantiation of the prefab ---
	GameObject SpawnHero(GameObject prefab, int moveNumber)
	{
		if (prefab == null || slushTarget == null) return null;

		// Find a spawn point on the left side of the screen
		Camera mainCam = Camera.main;
		Vector3 screenLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0.5f, 10f));
		Vector3 spawnPos = new Vector3(screenLeft.x - 2f, slushTarget.position.y + Random.Range(-1f, 1f), -0.5f);

		GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

		// Pass the Boss and the Canvas references to the new hero
		if (go.TryGetComponent<MVPHero>(out MVPHero heroScript))
		{
			heroScript.Initialize(slushTarget, moveNumber);
			heroScript.worldCanvas = worldCanvas;
		}
		return go;
	}

	// Check if at least one hero is ready to fight
	bool AnyHeroReady()
	{
		foreach (var hero in heroList)
			if (hero.move1.isReady || hero.move2.isReady) return true;
		return false;
	}

	// Entering/Exiting the Tactical menu
	void EnterTactical()
	{
		isCommandMode = true;
		if (tacticalManager != null) tacticalManager.EnterSlowMo();
		ToggleMovePanels(true);
	}

	void ExitTactical()
	{
		isCommandMode = false;
		if (tacticalManager != null) tacticalManager.ExitSlowMo();
		ToggleMovePanels(false);
	}

	// Shows or hides the names of the moves (Move 1 / Move 2)
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