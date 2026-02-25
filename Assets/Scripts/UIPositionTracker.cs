using UnityEngine;
using TMPro;

public class UIPositionTracker : MonoBehaviour
{
	public Transform target;
	public TextMeshProUGUI positionText;
	public PressureManager pressureManager;

	private Vector3 startPosition;
	private Rigidbody targetRb;
	private float totalElapsedTime = 0f;
	private bool timerActive = false;

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
		if (target == null || positionText == null) return;

		float distance = target.position.x - startPosition.x;
		float height = target.position.y - startPosition.y + 1;
		float horizontalVelocity = (targetRb != null) ? Mathf.Abs(targetRb.linearVelocity.x) : 0f;

		// TIMER LOGIC
		// Check if we should start the timer
		if (!timerActive)
		{
			float threshold = (pressureManager != null) ? pressureManager.startThresholdX : 2f;
			if (distance > threshold) timerActive = true;
		}

		// Only count if active and game isn't over
		bool gameOver = (pressureManager != null) && pressureManager.isGameOver;
		if (timerActive && !gameOver)
		{
			totalElapsedTime += Time.deltaTime;
		}

		int minutes = Mathf.FloorToInt(totalElapsedTime / 60);
		int seconds = Mathf.FloorToInt(totalElapsedTime % 60);

		positionText.text = $"Distance: {distance:F0}m\n" +
						   $"Height: {height:F0}m\n" +
						   $"Horiz Speed: {horizontalVelocity:F1} m/s\n" +
						   $"Time: {minutes:00}:{seconds:00}";
	}
}