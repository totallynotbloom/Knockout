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
		Time.timeScale = 1f;
		victoryUI.SetActive(false);

		// 1. Reset Boss Stats
		bossScript.currentHealth = bossScript.maxHealth;
		bossScript.currentArmor = bossScript.maxArmor;
		bossScript.isArmorBroken = false;
		bossScript.ResetDeathState();

		// 2. Snap Camera back to Boss
		if (virtualCamera != null)
		{
			// Re-enabling the camera makes it track the 'Follow' target again
			virtualCamera.enabled = true;

			// This 'OnTargetObjectWarped' line prevents the camera from "sliding" 
			// back slowly and instead snaps it instantly to the boss's current position.
			virtualCamera.OnTargetObjectWarped(bossRb.transform, bossRb.transform.position - virtualCamera.transform.position);
		}

		// 3. Reset Pressure
		PressureManager pm = Object.FindAnyObjectByType<PressureManager>();
		if (pm != null) pm.ResetManager();
	}


public void EndScene()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}