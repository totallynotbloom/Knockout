using UnityEngine;
using TMPro;

public class UIPositionTracker : MonoBehaviour
{
	public Transform target;
	public TextMeshProUGUI positionText;

	private Vector3 startPosition;
	private Rigidbody targetRb;

	void Start()
	{
		if (target != null)
		{
			startPosition = target.position;
			targetRb = target.GetComponent<Rigidbody>();
		}
	}

	void Update()
	{
		if (target != null && positionText != null && targetRb != null)
		{
			// Calculate relative distance
			float distance = target.position.x - startPosition.x;
			float height = target.position.y - startPosition.y + 1;

			// 1. Get ONLY the horizontal velocity (X axis)
			float horizontalVelocity = targetRb.linearVelocity.x;

			// 2. Use Mathf.Abs if you want "Speed" (always positive) 
			// OR leave it as is if you want to see negative numbers when moving left
			float displayVelocity = Mathf.Abs(horizontalVelocity);

			// Update the UI
			positionText.text = $"Distance: {distance:F0}m\n" +
							   $"Height: {height:F0}m\n" +
							   $"Horiz Speed: {displayVelocity:F1} m/s";
		}
	}
}