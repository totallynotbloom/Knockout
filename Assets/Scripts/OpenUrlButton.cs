using UnityEngine;

public class OpenUrlButton : MonoBehaviour
{
	[SerializeField]
	private string url = "https://docs.google.com/forms/d/e/1FAIpQLScCOmhDE_A7QUr8HSjNTEYoaHpFmvrAbrIqximQqCu50E7U9Q/viewform?usp=publish-editor";

	public void Open()
	{
		Application.OpenURL(url);
	}
}
