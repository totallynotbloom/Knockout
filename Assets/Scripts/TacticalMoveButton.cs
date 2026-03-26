using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// Added IDeselectHandler and IPointerExitHandler to handle the "OFF" state
public class TacticalMoveButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
	public int heroIndex;
	public int moveNumber;
	public TextMeshProUGUI buttonLabel;
	public TextMeshProUGUI statsDisplayText;
	public Image statsBackground;

	private MVPHero.MoveProfile myMoveProfile;
	private Color mySlotColor;
	public bool isMoveReady;

	[Header("Selection Visuals")]
	public Image selectionBorder;
	public Color readyColor = Color.white;
	public Color disabledHoverColor = Color.red;

	// --- NAVIGATION LOGIC ---

	public void OnSelect(BaseEventData eventData)
	{
		// Play sound only if it's a real navigation event (eventData is not null)
		if (eventData != null && MusicManager.Instance != null)
		{
			MusicManager.Instance.PlayNavigationSound();
		}

		UpdateUIFeedback();

		if (selectionBorder != null)
		{
			selectionBorder.enabled = true;
			// Check local flag to set the color immediately
			selectionBorder.color = isMoveReady ? readyColor : disabledHoverColor;
		}
	}

	public void OnDeselect(BaseEventData eventData)
	{
		ShowSelection(false); // Turn OFF border for WASD
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		// Force the keyboard selection to follow the mouse
		EventSystem.current.SetSelectedGameObject(gameObject);

		if (MusicManager.Instance != null)
			MusicManager.Instance.PlayNavigationSound();

		ShowSelection(true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		// If the mouse leaves this button, clear the selection 
		// This prevents multiple things from being highlighted
		if (EventSystem.current.currentSelectedGameObject == gameObject)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}

		ShowSelection(false);
	}

	private void ShowSelection(bool isVisible)
	{
		if (selectionBorder == null) return;

		selectionBorder.enabled = isVisible;

		// Toggle the stats panel based on selection
		if (statsBackground != null)
			statsBackground.gameObject.SetActive(isVisible);

		if (isVisible)
		{
			// 9-Slicing must be set in Inspector to avoid "Solid Block" look
			selectionBorder.color = isMoveReady ? readyColor : disabledHoverColor;
			UpdateUIFeedback();
		}
	}

	// --- COOLDOWN LOGIC ---

	public void SetCooldownState(bool isReady)
	{
		isMoveReady = isReady; // Update our internal flag

		Button btn = GetComponent<Button>();
		if (btn != null)
		{
			// We keep it interactable so WASD/F keys still work
			btn.interactable = true;

			if (buttonLabel != null)
				buttonLabel.alpha = isReady ? 1.0f : 0.2f;

			// If we are currently looking at the button, update the border color immediately
			if (selectionBorder != null && selectionBorder.enabled)
			{
				selectionBorder.color = isReady ? readyColor : disabledHoverColor;
			}
		}
	}

	private void UpdateUIFeedback()
	{
		if (myMoveProfile != null && statsDisplayText != null)
		{
			// Update the Stats Text
			statsDisplayText.text = $"{myMoveProfile.moveName}\n" +
									$"Force: {myMoveProfile.hitForceXY.magnitude} | CD: {myMoveProfile.cooldown}s";

			// Match the Stats Bar background to the Hero's color
			if (statsBackground != null)
				statsBackground.color = mySlotColor;
		}
	}

	public void SetupButton(MVPHero.MoveProfile profile, int moveDisplayNumber)
	{
		myMoveProfile = profile;
		if (buttonLabel != null) buttonLabel.text = $"{moveDisplayNumber}. {profile.moveName}";

		// Get the color from the HeroSlot (the grandparent)
		Image slotImage = transform.parent.parent.GetComponent<Image>();
		if (slotImage != null)
		{
			mySlotColor = slotImage.color;

			Button btn = GetComponent<Button>();
			// We modify the ColorBlock to ensure the transitions are opaque and visible
			ColorBlock colors = btn.colors;

			// Idle state: The hero's signature color
			colors.normalColor = new Color(mySlotColor.r, mySlotColor.g, mySlotColor.b, 0.8f);

			// Selection/Hover: Pure opaque white (Alpha = 1f)
			colors.highlightedColor = Color.white;
			colors.selectedColor = Color.white;

			// Cooldown: Dark and transparent

			btn.colors = colors;
		}
	}

	public void OnClick()
	{
		HeroSummoner summoner = Object.FindFirstObjectByType<HeroSummoner>();
		if (summoner == null) return;

		// Direct Check: Ask the summoner if THIS specific hero/move is ready
		// This bypasses the local isMoveReady flag which might be out of sync
		bool actuallyReady = summoner.CheckMoveReady(heroIndex, moveNumber);

		if (actuallyReady)
		{
			if (summoner.AttemptSummon(heroIndex, moveNumber))
			{
				summoner.ExitTactical();
			}
		}
		else
		{
			Debug.Log($"Hero {heroIndex} Move {moveNumber} is still on cooldown!");
		}
	}

}