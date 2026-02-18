using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryManager : MonoBehaviour
{
	public static VictoryManager Instance;

	[Header("References")]
	public GameObject victoryUI;
	public BossHealth bossScript;
	public Rigidbody bossRb;
	public HeroSummoner summoner;
	[Header("Camera Control")]
	public CinemachineCamera virtualCamera; // Drag your Virtual Camera here

	private Vector3 savedVelocity;
	private Vector3 savedAngularVelocity;

	void Awake() => Instance = this;

	public void ShowVictoryMenu()
	{
		// Calls the delay instead of freezing instantly
		StartCoroutine(DelayedWinSequence());
	}

	IEnumerator DelayedWinSequence()
	{
		yield return new WaitForSeconds(2.0f); // Wait for boss to fly off
		Time.timeScale = 0f;
		victoryUI.SetActive(true);
	}

	public void Restart()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	public void Continue()
	{
		// 1. Unfreeze the world first so logic can resume
		Time.timeScale = 1f;
		victoryUI.SetActive(false);

		// 2. RESET THE BOSS STATE FIRST
		// This stops old coroutines and sets isDead = false BEFORE we heal him
		bossScript.ResetDeathState();

		// 3. Snap Camera back to Boss
		if (virtualCamera != null)
		{
			virtualCamera.enabled = true;
			virtualCamera.OnTargetObjectWarped(bossRb.transform, bossRb.transform.position - virtualCamera.transform.position);
		}

		// 4. Reset Pressure logic
		PressureManager pm = Object.FindAnyObjectByType<PressureManager>();
		if (pm != null) pm.ResetManager();

		// 5. Tell the summoner to resume (if it was disabled)
		if (summoner != null) summoner.enabled = true;
	}


	public void EndScene()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}