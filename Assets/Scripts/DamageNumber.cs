using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
	public float floatSpeed = 2f;
	public float fadeDuration = 1f;
	public float holdTime = 0.5f; // How many seconds to stay still

	private TextMeshProUGUI text;
	private Color startColor;
	private float timer;
	private float holdTimer;

	void Start()
	{
		text = GetComponent<TextMeshProUGUI>();
		startColor = text.color;

		// Destroy after total time (hold + fade)
		Destroy(gameObject, holdTime + fadeDuration);
	}

	void Update()
	{
		// 1. Handle the "Still" phase
		if (holdTimer < holdTime)
		{
			holdTimer += Time.deltaTime;
			return; // Skip the rest of the update while holding
		}

		// 2. Move upward over time (only after holdTimer is finished)
		transform.position += Vector3.up * floatSpeed * Time.deltaTime;

		// 3. Fade out
		timer += Time.deltaTime;
		float alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
		text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
	}

	public void SetText(float amount)
	{
		// Using Mathf.RoundToInt so we don't see long decimals
		GetComponent<TextMeshProUGUI>().text = "-" + Mathf.RoundToInt(amount).ToString();
	}
}