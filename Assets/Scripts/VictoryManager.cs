using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This script manages the "End of Round" state. 
/// It handles the transition from the Boss's death to the Victory UI 
/// and coordinates the "Endless Loop" reset logic when the player continues.
/// </summary>
public class VictoryManager : MonoBehaviour
{
	// Singleton pattern allows the BossHealth script to trigger a win easily
	public static VictoryManager Instance;

	[Header("References")]
	public GameObject victoryUI;        // The UI panel with "Continue" and "Quit" buttons
	public BossHealth bossScript;      // Reference to reset the boss for endless mode
	public Rigidbody bossRb;           // Used for camera warping/positioning
	public HeroSummoner summoner;      // Used to re-enable summoning after a win

	[Header("Camera Control")]
	public CinemachineCamera virtualCamera;

	void Awake() => Instance = this;

	/// <summary>
	/// Public entry point. Usually called by BossHealth.Die().
	/// </summary>
	public void ShowVictoryMenu()
	{
		// We use a Coroutine so the player can watch the boss fly away 
		// before the menu pops up and freezes time.
		StartCoroutine(DelayedWinSequence());
	}

	/// <summary>
	/// The cinematic delay between killing the boss and seeing the UI.
	/// </summary>
	IEnumerator DelayedWinSequence()
	{
		// 2-second "Dramatic Pause" to enjoy the final hit impact
		yield return new WaitForSeconds(2.0f);

		Time.timeScale = 0f; // Freeze all game logic
		victoryUI.SetActive(true);
	}

	/// <summary>
	/// Standard scene reload for a fresh start.
	/// </summary>
	public void Restart()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	/// <summary>
	/// THE ENDLESS LOOP LOGIC:
	/// Resets the current scene state so the player can keep playing without a loading screen.
	/// </summary>
	public void Continue()
	{
		// 1. Unfreeze the world so physics and timers resume
		Time.timeScale = 1f;
		victoryUI.SetActive(false);

		// 2. BOSS RE-INITIALIZATION:
		// We tell the boss to reset health, armor, and its "isDead" flag.
		bossScript.ResetDeathState();

		// 3. CAMERA SYNC:
		// If the boss flew far away, we tell Cinemachine to "Warp" back to him 
		// to prevent the camera from having to travel across the whole map.
		if (virtualCamera != null)
		{
			virtualCamera.enabled = true;
			virtualCamera.OnTargetObjectWarped(bossRb.transform, bossRb.transform.position - virtualCamera.transform.position);
		}

		// 4. PRESSURE RESET:
		// Resets the difficulty/pressure scaling so the new round starts fair.
		PressureManager pm = Object.FindAnyObjectByType<PressureManager>();
		if (pm != null) pm.ResetManager();

		// 5. INPUT RESTORATION:
		// Ensure the summoner is ready to receive new player commands.
		if (summoner != null) summoner.enabled = true;
	}

	/// <summary>
	/// Closes the application (handles Editor and Build versions).
	/// </summary>
	public void EndScene()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}