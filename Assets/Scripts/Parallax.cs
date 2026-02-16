using UnityEngine;

public class Parallax : MonoBehaviour
{
	private float length, startpos;
	public GameObject cam;
	public float parallaxEffect; // 0 = moves with player (static), 1 = stays still (sky)

	void Start()
	{
		startpos = transform.position.x;
		// Get the width of the sprite so we know when to loop it
		length = GetComponent<SpriteRenderer>().bounds.size.x;
	}

	void Update()
	{
		// How far we have moved relative to the parallax speed
		float temp = (cam.transform.position.x * (1 - parallaxEffect));

		// How far we should move the background object
		float dist = (cam.transform.position.x * parallaxEffect);

		// Move the background
		transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

		// The Infinite Loop Logic:
		// If the camera has moved past the sprite's edge, jump the start position forward
		if (temp > startpos + length) startpos += length;
		else if (temp < startpos - length) startpos -= length;
	}
}