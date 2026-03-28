using UnityEngine;
using TMPro;

public class MoveInfoPanel : MonoBehaviour
{
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI descriptionText;
	public GameObject panelRoot; // Drag the "Panel" background here

	public void ShowMoveInfo(string mName, string mDesc)
	{
		panelRoot.SetActive(true);
		nameText.text = mName;
		descriptionText.text = mDesc;
	}

	public void Hide()
	{
		if (panelRoot != null)
			panelRoot.SetActive(false);
	}
}