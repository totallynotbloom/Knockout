using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;

/// <summary>
/// This script manages the "Command Mode" (Tactical Slow-Mo) and the 
/// Dynamic Camera Zoom that reacts to the Boss's movement speed.
/// </summary>
public class TacticalManager : MonoBehaviour
{
	[Header("Time Settings")]
	public float slowTimeScale = 0.2f; // The speed of the game during selection (20% speed)
	private float normalDeltaTime;    // Stores the original physics step

	[Header("Camera & Visuals")]
	public CinemachineCamera virtualCamera; // The main brain of our camera system
	public Volume slowMoVolume;             // Post-processing (blue filter/blur)
	public Rigidbody blueRigidbody;         // The Boss's physics body (to track speed)

	[Header("Zoom Settings")]
	public float normalZoom = 10f;          // The base Field of View
	public float slowMoZoomOffset = 5f;     // How much closer the camera gets in Slow-Mo
	public float speedZoomMax = 18f;        // The maximum zoom-out when things are fast
	public float zoomSpeed = 5f;            // How quickly the lens glides to new values

	private float targetZoom;
	private bool isSlowMo = false;          // Flag to prevent speed-zoom from overriding menu-zoom

	void Start()
	{
		// Store the physics fixedDeltaTime so physics don't jitter when we slow down time
		normalDeltaTime = Time.fixedDeltaTime;
		targetZoom = normalZoom;
	}

	void Update()
	{
		// 1. SPEED-BASED ZOOM LOGIC
		// If the game is running normally, the camera zooms out as the boss gets faster
		if (!isSlowMo)
		{
			float currentSpeed = blueRigidbody.linearVelocity.magnitude;
			// Map speed (0 to 50) to zoom (10 to 18) for a dynamic "Speed" feel
			targetZoom = Mathf.Lerp(normalZoom, speedZoomMax, currentSpeed / 50f);
		}

		// 2. CAMERA INTERPOLATION
		// Smoothly transition the Cinemachine lens to our targetZoom
		float currentLensValue = virtualCamera.Lens.FieldOfView;
		virtualCamera.Lens.FieldOfView = Mathf.Lerp(currentLensValue, targetZoom, Time.unscaledDeltaTime * zoomSpeed);

		// 3. POST-PROCESSING FADE
		// Smoothly fade the "Slow-Mo Blue Filter" in or out
		float targetWeight = isSlowMo ? 1f : 0f;
		slowMoVolume.weight = Mathf.MoveTowards(slowMoVolume.weight, targetWeight, Time.unscaledDeltaTime * 3f);
	}

	/// <summary>
	/// Triggered by HeroSummoner when the player opens the move menu.
	/// </summary>
	public void EnterSlowMo()
	{
		isSlowMo = true;
		Time.timeScale = slowTimeScale; // Slow down the world

		// Update fixedDeltaTime so physics (collisions/movement) stay smooth in slow-mo
		Time.fixedDeltaTime = normalDeltaTime * Time.timeScale;

		// ZOOM IN: Capture the current camera FOV and move it 5 units closer for focus
		targetZoom = virtualCamera.Lens.FieldOfView - slowMoZoomOffset;

		// Safety check: prevent the camera from clipping inside objects
		if (targetZoom < 2f) targetZoom = 2f;
	}

	/// <summary>
	/// Triggered by HeroSummoner when a move is picked or the menu is closed.
	/// </summary>
	public void ExitSlowMo()
	{
		isSlowMo = false;

		// Safeguard: Only return to 1.0 speed if the game isn't currently 
		// "Hit-Stopped" by the HitStopManager (Time.timeScale == 0)
		if (Time.timeScale != 0f)
		{
			Time.timeScale = 1f;
		}

		Time.fixedDeltaTime = normalDeltaTime;
	}
}