using UnityEngine;
using Unity.Cinemachine; 
using UnityEngine.Rendering; 

public class TacticalManager : MonoBehaviour
{
	[Header("Time Settings")]
	public float slowTimeScale = 0.2f;
	private float normalDeltaTime;

	[Header("Camera & Visuals")]
	public CinemachineCamera virtualCamera;
	public Volume slowMoVolume;
	public Rigidbody blueRigidbody;

	[Header("Zoom Settings")]
	public float normalZoom = 10f;
	public float slowMoZoomOffset = 5f; // Subtract this from current zoom
	public float speedZoomMax = 18f;
	public float zoomSpeed = 5f;

	private float targetZoom;
	private bool isSlowMo = false; // Track state to stop speed-zoom interference

	void Start()
	{
		normalDeltaTime = Time.fixedDeltaTime;
		targetZoom = normalZoom;
	}

	void Update()
	{
		// 1. Only calculate Speed-Based Zoom if we are NOT in slow-mo
		if (!isSlowMo)
		{
			float currentSpeed = blueRigidbody.linearVelocity.magnitude;
			// Map speed (0 to 50) to zoom (10 to 18)
			targetZoom = Mathf.Lerp(normalZoom, speedZoomMax, currentSpeed / 50f);
		}

		// 2. Smoothly apply the zoom to Cinemachine Lens
		// Note: Ensure your Cinemachine Camera is set to "Perspective" if using FieldOfView, 
		// or change .FieldOfView to .OrthographicSize if your project is 2D.
		float currentLensValue = virtualCamera.Lens.FieldOfView;
		virtualCamera.Lens.FieldOfView = Mathf.Lerp(currentLensValue, targetZoom, Time.unscaledDeltaTime * zoomSpeed);

		// 3. Smoothly fade the Blue Filter
		float targetWeight = isSlowMo ? 1f : 0f;
		slowMoVolume.weight = Mathf.MoveTowards(slowMoVolume.weight, targetWeight, Time.unscaledDeltaTime * 3f);
	}

	public void EnterSlowMo()
	{
		isSlowMo = true;
		Time.timeScale = slowTimeScale;
		Time.fixedDeltaTime = normalDeltaTime * Time.timeScale;

		// SET RELATIVE ZOOM: Current Zoom minus the offset
		// This captures the camera exactly where it is and pushes it 5 units closer
		targetZoom = virtualCamera.Lens.FieldOfView - slowMoZoomOffset;

		// Safety check so we don't zoom into the boss's atoms
		if (targetZoom < 2f) targetZoom = 2f;
	}

	public void ExitSlowMo()
	{
		isSlowMo = false;

		// ONLY set time back to 1 if we aren't currently frozen by a hit
		if (Time.timeScale != 0f)
		{
			Time.timeScale = 1f;
		}

		Time.fixedDeltaTime = normalDeltaTime;
	}
}