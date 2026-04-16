using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MoveInfoPanel : MonoBehaviour
{
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI descriptionText;
	public TextMeshProUGUI statsText;
	public GameObject panelRoot;

	[Header("Panel Layout")]
	public RectTransform panelRect;
	public float compactHeight = 120f;
	public float expandedHeight = 260f;
	public bool keepBottomEdgePinned = true;

	[Header("Text Layout")]
	public RectTransform nameTextRect;
	public RectTransform descriptionTextRect;
	public RectTransform statsTextRect;
	public float expandedNameYOffset = 40f;
	public float expandedDescriptionYOffset = 10f;
	public float expandedDescriptionHeight = 180f;
	public float expandedDescriptionWidth = 210f;
	public float expandedStatsWidth = 170f;
	public float expandedStatsXOffset = 115f;
	public float expandedStatsYOffset = 10f;
	public float expandedStatsHeight = 180f;

	[Header("Optional Direction Visual")]
	public RectTransform launchArrow;
	public Image launchArrowImage;

	private MVPHero.MoveProfile currentMoveProfile;
	private bool showTechnicalDetails;
	private Color currentAccentColor = Color.white;
	private bool layoutInitialized;
	private float compactPanelAnchoredY;
	private Vector2 compactNameAnchoredPosition;
	private Vector2 compactDescriptionAnchoredPosition;
	private Vector2 compactStatsAnchoredPosition;
	private float compactDescriptionHeightCached;
	private float compactDescriptionWidthCached;
	private float compactStatsHeightCached;
	private float compactStatsWidthCached;

	private void Awake()
	{
		EnsureLayoutReferences();
		CacheCompactLayout();
	}

	public void ShowMoveInfo(string mName, string mDesc)
	{
		EnsureLayoutReferences();

		// Safety check to prevent errors if you haven't assigned the root yet
		GetPanelTarget().SetActive(true);

		nameText.text = mName;
		descriptionText.text = mDesc;
	}

	public void ShowMoveInfo(MVPHero.MoveProfile moveProfile)
	{
		ShowMoveInfo(moveProfile, Color.white);
	}

	public void ShowMoveInfo(MVPHero.MoveProfile moveProfile, Color accentColor)
	{
		if (moveProfile == null) return;

		EnsureLayoutReferences();
		currentMoveProfile = moveProfile;
		currentAccentColor = accentColor;

		GetPanelTarget().SetActive(true);

		RefreshDisplay();
	}

	public void Hide()
	{
		EnsureLayoutReferences();
		GetPanelTarget().SetActive(false);

		if (launchArrowImage != null)
			launchArrowImage.enabled = false;

		currentMoveProfile = null;
		showTechnicalDetails = false;
		ApplyLayout(false);
	}

	public void SetDetailsExpanded(bool expanded)
	{
		if (showTechnicalDetails == expanded) return;

		showTechnicalDetails = expanded;
		RefreshDisplay();
	}

	private void RefreshDisplay()
	{
		if (currentMoveProfile == null) return;

		nameText.text = currentMoveProfile.moveName;
		nameText.color = currentAccentColor;
		descriptionText.color = currentAccentColor;
		descriptionText.text = showTechnicalDetails
			? BuildFlavorDescription(currentMoveProfile)
			: BuildCompactDescription(currentMoveProfile);

		if (statsText != null)
		{
			statsText.color = currentAccentColor;
			statsText.text = showTechnicalDetails ? BuildStatsText(currentMoveProfile) : "";
		}
		else if (showTechnicalDetails)
		{
			descriptionText.text = $"{BuildCompactDescription(currentMoveProfile)}\n\n{BuildStatsText(currentMoveProfile)}";
		}

		ApplyLayout(showTechnicalDetails);
		UpdateLaunchArrow(currentMoveProfile);
	}

	private string BuildCompactDescription(MVPHero.MoveProfile moveProfile)
	{
		return $"{BuildFlavorDescription(moveProfile)}\n\n{BuildCompactQuickStats(moveProfile)}\n<align=\"center\"><color=#66FF66>Hold Tab for More Info</color></align>";
	}

	private string BuildFlavorDescription(MVPHero.MoveProfile moveProfile)
	{
		return string.IsNullOrWhiteSpace(moveProfile.description)
			? "No move description written yet."
			: moveProfile.description.Trim();
	}

	private string BuildCompactQuickStats(MVPHero.MoveProfile moveProfile)
	{
		return $"Hp {moveProfile.hitForceXY.x:F0}  Vp {moveProfile.hitForceXY.y:F0}<pos=72%>CD {moveProfile.cooldown:F1}s";
	}

	private string BuildStatsText(MVPHero.MoveProfile moveProfile)
	{
		string spikeText = moveProfile.isSpike ? "Yes" : "No";
		string directionText = GetDirectionSummary(moveProfile.hitForceXY);
		float attackDamage = CalculateAttackDamage(moveProfile);
		float structureDamage = CalculateStructureDamage(moveProfile);

		return BuildStatLine("Horizontal Power:", $"{moveProfile.hitForceXY.x:F0}") + "\n" +
			   BuildStatLine("Vertical Power:", $"{moveProfile.hitForceXY.y:F0}") + "\n" +
			   BuildStatLine("Attack Damage:", $"{attackDamage:F1}") + "\n" +
			   BuildStatLine("Structure Damage:", $"{structureDamage:F1}") + "\n" +
			   BuildStatLine("Cooldown:", $"{moveProfile.cooldown:F1}s") + "\n" +
			   BuildStatLine("Approach Speed:", $"{moveProfile.approachSpeed:F1}") + "\n" +
			   BuildStatLine("Give Up Time:", $"{moveProfile.giveUpTime:F1}s") + "\n" +
			   BuildStatLine("Spikes:", spikeText) + "\n" +
			   BuildStatLine("Launch Direction:", directionText);
	}

	private float CalculateAttackDamage(MVPHero.MoveProfile moveProfile)
	{
		return moveProfile.hitForceXY.magnitude * 0.5f;
	}

	private float CalculateStructureDamage(MVPHero.MoveProfile moveProfile)
	{
		return moveProfile.hitForceXY.magnitude * 0.5f;
	}

	private string BuildStatLine(string label, string value)
	{
		return $"{label}<pos=72%>{value}";
	}

	private string GetDirectionSummary(Vector2 force)
	{
		if (Mathf.Approximately(force.x, 0f) && Mathf.Approximately(force.y, 0f))
			return "No launch";

		string horizontal = Mathf.Approximately(force.x, 0f) ? "" : (force.x > 0f ? "Right" : "Left");
		string vertical = Mathf.Approximately(force.y, 0f) ? "" : (force.y > 0f ? "Up" : "Down");

		if (!string.IsNullOrEmpty(horizontal) && !string.IsNullOrEmpty(vertical))
			return $"{vertical}-{horizontal}";

		return string.IsNullOrEmpty(horizontal) ? vertical : horizontal;
	}

	private void EnsureLayoutReferences()
	{
		if (panelRect == null)
			panelRect = transform as RectTransform;

		if (nameTextRect == null && nameText != null)
			nameTextRect = nameText.rectTransform;

		if (descriptionTextRect == null && descriptionText != null)
			descriptionTextRect = descriptionText.rectTransform;

		if (statsTextRect == null && statsText != null)
			statsTextRect = statsText.rectTransform;

		if (!layoutInitialized)
		{
			CacheCompactLayout();
		}
	}

	private void CacheCompactLayout()
	{
		if (panelRect == null || nameTextRect == null || descriptionTextRect == null) return;

		if (compactHeight <= 0f)
			compactHeight = panelRect.rect.height;

		compactPanelAnchoredY = panelRect.anchoredPosition.y;
		compactNameAnchoredPosition = nameTextRect.anchoredPosition;
		compactDescriptionAnchoredPosition = descriptionTextRect.anchoredPosition;
		compactDescriptionHeightCached = descriptionTextRect.sizeDelta.y;
		compactDescriptionWidthCached = descriptionTextRect.sizeDelta.x;

		if (statsTextRect != null)
		{
			compactStatsAnchoredPosition = statsTextRect.anchoredPosition;
			compactStatsHeightCached = statsTextRect.sizeDelta.y;
			compactStatsWidthCached = statsTextRect.sizeDelta.x;
		}
		layoutInitialized = true;
	}

	private void ApplyLayout(bool expanded)
	{
		if (!layoutInitialized || panelRect == null || nameTextRect == null || descriptionTextRect == null) return;

		float targetHeight = expanded ? expandedHeight : compactHeight;
		ApplyPanelHeight(targetHeight);
		ApplyTextLayout(expanded);

		if (nameText != null)
			nameText.ForceMeshUpdate();

		if (descriptionText != null)
			descriptionText.ForceMeshUpdate();
	}

	private void ApplyPanelHeight(float targetHeight)
	{
		if (panelRect == null) return;

		Vector2 size = panelRect.sizeDelta;
		float previousHeight = size.y;
		size.y = targetHeight;
		panelRect.sizeDelta = size;

		if (keepBottomEdgePinned)
		{
			Vector2 anchoredPosition = panelRect.anchoredPosition;
			float deltaHeight = targetHeight - previousHeight;
			anchoredPosition.y += deltaHeight * panelRect.pivot.y;
			panelRect.anchoredPosition = anchoredPosition;
		}
	}

	private void ApplyTextLayout(bool expanded)
	{
		if (nameTextRect == null || descriptionTextRect == null) return;

		nameTextRect.anchoredPosition = expanded
			? compactNameAnchoredPosition + new Vector2(0f, expandedNameYOffset)
			: compactNameAnchoredPosition;

		descriptionTextRect.anchoredPosition = expanded
			? compactDescriptionAnchoredPosition + new Vector2(0f, expandedDescriptionYOffset)
			: compactDescriptionAnchoredPosition;

		Vector2 descriptionSize = descriptionTextRect.sizeDelta;
		descriptionSize.y = expanded ? expandedDescriptionHeight : compactDescriptionHeightCached;
		descriptionSize.x = expanded && statsTextRect != null ? expandedDescriptionWidth : compactDescriptionWidthCached;
		descriptionTextRect.sizeDelta = descriptionSize;

		if (statsTextRect != null)
		{
			statsTextRect.gameObject.SetActive(expanded);
			statsTextRect.anchoredPosition = expanded
				? compactDescriptionAnchoredPosition + new Vector2(expandedStatsXOffset, expandedStatsYOffset)
				: compactStatsAnchoredPosition;

			Vector2 statsSize = statsTextRect.sizeDelta;
			statsSize.x = expanded ? expandedStatsWidth : compactStatsWidthCached;
			statsSize.y = expanded ? expandedStatsHeight : compactStatsHeightCached;
			statsTextRect.sizeDelta = statsSize;
		}
	}

	private GameObject GetPanelTarget()
	{
		return panelRoot != null ? panelRoot : gameObject;
	}

	private void UpdateLaunchArrow(MVPHero.MoveProfile moveProfile)
	{
		if (launchArrow == null || launchArrowImage == null) return;

		Vector2 force = moveProfile.hitForceXY;
		bool hasDirection = force.sqrMagnitude > 0.001f;
		launchArrowImage.enabled = hasDirection;

		if (!hasDirection) return;

		float angle = Mathf.Atan2(force.y, force.x) * Mathf.Rad2Deg;
		launchArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
	}
}
