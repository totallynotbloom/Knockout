using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

/// <summary>
/// This script acts as the "Air Traffic Controller" for combat. 
/// It maintains a list of all valid attack targets (Boss and Minions) and 
/// ensures the Camera and the Heroes are all looking at the same thing.
/// </summary>
public class GlobalTargetManager : MonoBehaviour
{
	// The master list of everything the player can target
	public List<Transform> enemies = new List<Transform>();
	public int currentTargetIndex = 0;

	[Header("References")]
	public CinemachineCamera virtualCamera; // To tell the camera who to follow
	public HeroSummoner heroSummoner;       // To tell the heroes who to fly toward

	void Start()
	{
		// INITIAL SEARCH: Find everything tagged as a target at the start of the level.
		// Note: Ensure your Boss and Minions are tagged correctly in the Inspector!
		GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("Player");
		foreach (GameObject obj in enemyObjs)
		{
			enemies.Add(obj.transform);
		}

		// If we found targets, point the camera at the first one (usually the Boss)
		if (enemies.Count > 0) UpdateTargeting();
	}

	void Update()
	{
		// Toggle between targets using the Tab key
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			CycleTarget();
		}
	}

	/// <summary>
	/// Moves the index to the next enemy in the list, looping back to 0 if at the end.
	/// </summary>
	void CycleTarget()
	{
		if (enemies.Count <= 1) return; // Nothing to switch to

		currentTargetIndex = (currentTargetIndex + 1) % enemies.Count;
		UpdateTargeting();
	}

	/// <summary>
	/// The core logic that synchronizes the Camera and the HeroSummoner.
	/// </summary>
	void UpdateTargeting()
	{
		if (enemies.Count == 0) return;

		Transform newTarget = enemies[currentTargetIndex];

		// 1. VIRTUAL CAMERA: Immediately shift the camera's focus
		virtualCamera.Follow = newTarget;

		// 2. HERO SUMMONER: Update the global target so the NEXT hero spawned 
		// knows exactly where to fly.
		heroSummoner.slushTarget = newTarget;

		Debug.Log("Target Switched to: " + newTarget.name);
	}

	/// <summary>
	/// Called by Minions when they spawn so they can be added to the Tab-cycle.
	/// </summary>
	public void RegisterEnemy(Transform newEnemy)
	{
		if (!enemies.Contains(newEnemy))
		{
			enemies.Add(newEnemy);
		}
	}

	/// <summary>
	/// Called when a Minion or Boss dies. Prevents the player from 
	/// targeting a "Null" or destroyed object.
	/// </summary>
	public void UnregisterEnemy(Transform deadEnemy)
	{
		if (enemies.Contains(deadEnemy))
		{
			enemies.Remove(deadEnemy);

			// CRITICAL: If the thing that just died was our current target, 
			// we must switch away immediately to avoid camera/hero errors.
			if (heroSummoner.slushTarget == deadEnemy)
			{
				currentTargetIndex = 0; // Default back to the primary target (the Boss)
				UpdateTargeting();
			}
		}
	}
}