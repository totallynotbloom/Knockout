using UnityEngine;

[System.Serializable]
public class HeroMove
{
	// These are "Live" variables tracked by the Summoner
	[HideInInspector] public float currentTimer;
	[HideInInspector] public bool isReady = true;
}

[System.Serializable]
public class HeroData
{
	public string heroName;
	public GameObject prefab;

	// Changed names to match the class above
	public HeroMove move1 = new HeroMove();
	public HeroMove move2 = new HeroMove();

	[HideInInspector] public GameObject activeInstance;
}