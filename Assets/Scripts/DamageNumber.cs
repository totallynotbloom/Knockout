using UnityEngine;
using TMPro;

/// <summary>
/// A "Fire and Forget" script that handles the visual pop-up text when damage is dealt.
/// It features a 'Hold' phase for readability and a 'Fade' phase for polish.
/// </summary>
public class DamageNumber : MonoBehaviour
{
	[Header("Movement")]
	public float floatSpeed = 2f;    // How fast the text drifts upward
	public float holdTime = 0.5f;    // How many seconds the text stays still before drifting

	[Header("Visuals")]
	public float fadeDuration = 1f;  // How long the fade-out takes

	private TextMeshProUGUI text;
	private Color startColor;
	private float timer;
	private float holdTimer;

	void Start()
	{
		text = GetComponent<TextMeshProUGUI>();
		startColor = text.color;

		// SELF-DESTRUCT: Automatically cleans up the object once the animation is finished.
		// This prevents the hierarchy from getting cluttered with "dead" text objects.
		Destroy(gameObject, holdTime + fadeDuration);
	}

	void Update()
	{
		// 1. THE HOLD PHASE: 
		// We keep the text stationary briefly so the player can actually read the number.
		if (holdTimer < holdTime)
		{
			holdTimer += Time.deltaTime;
			return; // Exit update early; don't move or fade yet.
		}

		// 2. THE DRIFT PHASE:
		// Move the text upward in world/screen space.
		transform.position += Vector3.up * floatSpeed * Time.deltaTime;

		// 3. THE FADE PHASE:
		// Gradually reduce alpha from 1 to 0 based on the fadeDuration.
		timer += Time.deltaTime;
		float alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
		text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
	}

	/// <summary>
	/// Public method called by MVPHero during a hit.
	/// Formats the float damage into a clean integer string.
	/// </summary>
	public void SetText(float amount)
	{
		// Using Mathf.RoundToInt so we don't see long decimals like "-10.33333"
		if (text == null) text = GetComponent<TextMeshProUGUI>();
		text.text = "-" + Mathf.RoundToInt(amount).ToString();
	}
}