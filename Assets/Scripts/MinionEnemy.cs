using UnityEngine;

public class MinionEnemy : MonoBehaviour
{
	private GlobalTargetManager targetManager;

	void Start()
	{
		targetManager = FindFirstObjectByType<GlobalTargetManager>();
		if (targetManager != null)
		{
			targetManager.RegisterEnemy(this.transform);
		}
	}

	// Call this when the minion's health reaches 0
	public void OnDeath()
	{
		if (targetManager != null)
		{
			targetManager.UnregisterEnemy(this.transform);
		}
		Destroy(gameObject);
	}

	private void OnDestroy()
	{
		// Safety check: ensure it unregisters even if destroyed by other means
		if (targetManager != null) targetManager.UnregisterEnemy(this.transform);
	}
}