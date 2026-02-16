using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
	public HeroSummoner summoner;
	public float hoverHeight = 3f;

	void Update()
	{
		if (summoner.slushTarget != null)
		{
			// Stay above the current target
			transform.position = summoner.slushTarget.position + Vector3.up * hoverHeight;
			// Add a little spin for juice
			transform.Rotate(Vector3.up, 200 * Time.deltaTime);
		}
	}
}