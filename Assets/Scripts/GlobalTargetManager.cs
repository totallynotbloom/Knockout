using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class GlobalTargetManager : MonoBehaviour
{
	public List<Transform> enemies = new List<Transform>();
	public int currentTargetIndex = 0;

	[Header("References")]
	public CinemachineCamera virtualCamera;
	public HeroSummoner heroSummoner;

	void Start()
	{
		// Automatically find everything with the "Enemy" tag at the start
		GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("Player"); // Or "Enemy"
		foreach (GameObject obj in enemyObjs)
		{
			enemies.Add(obj.transform);
		}

		if (enemies.Count > 0) UpdateTargeting();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab)) // Press Tab to switch
		{
			CycleTarget();
		}
	}

	void CycleTarget()
	{
		if (enemies.Count <= 1) return;

		currentTargetIndex = (currentTargetIndex + 1) % enemies.Count;
		UpdateTargeting();
	}

	void UpdateTargeting()
	{
		Transform newTarget = enemies[currentTargetIndex];

		// 1. Update the Camera to follow the new guy
		virtualCamera.Follow = newTarget;

		// 2. Update the HeroSummoner so heroes fly toward the new guy
		heroSummoner.slushTarget = newTarget;

		Debug.Log("Target Switched to: " + newTarget.name);
	}
	public void RegisterEnemy(Transform newEnemy)
	{
		if (!enemies.Contains(newEnemy))
		{
			enemies.Add(newEnemy);
		}
	}

	public void UnregisterEnemy(Transform deadEnemy)
	{
		if (enemies.Contains(deadEnemy))
		{
			enemies.Remove(deadEnemy);
			// If the target we were looking at just died, switch to the next one
			if (heroSummoner.slushTarget == deadEnemy)
			{
				currentTargetIndex = 0; // Default back to Boss
				UpdateTargeting();
			}
		}
	}
}