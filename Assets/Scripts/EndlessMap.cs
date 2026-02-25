using UnityEngine;
using System.Collections.Generic;

public class EndlessMap : MonoBehaviour
{
	[Header("Setup")]
	public GameObject firstMapChunk;
	public GameObject[] mapChunks;
	public Transform player;

	[Header("Dimensions")]
	public float chunkWidth = 278f;
	public int chunksAhead = 2;

	private float nextChunkX = 0f;
	private Queue<GameObject> activeChunks = new Queue<GameObject>();

	void Start()
	{
		if (player == null) player = Camera.main.transform;

		if (firstMapChunk != null)
		{
			activeChunks.Enqueue(firstMapChunk);
			// Sync starting height and position
			nextChunkX = firstMapChunk.transform.position.x + chunkWidth;
		}
		else
		{
			nextChunkX = 0f;
		}

		// Initial fill
		for (int i = 0; i < chunksAhead; i++)
		{
			SpawnChunk();
		}
	}

	void Update()
	{
		if (player == null) return;

		// FIXED TRIGGER: While loop ensures that even if you teleport or move fast, 
		// the map catches up to your current position.
		while (player.position.x > nextChunkX - (chunksAhead * chunkWidth))
		{
			SpawnChunk();
			RemoveOldChunk();
		}
	}

	void SpawnChunk()
	{
		if (mapChunks.Length == 0) return;

		GameObject prefab = mapChunks[Random.Range(0, mapChunks.Length)];

		// Match the height (Y) of your first chunk to prevent jumping
		float spawnY = (firstMapChunk != null) ? firstMapChunk.transform.position.y : 0f;

		GameObject chunk = Instantiate(prefab, new Vector3(nextChunkX, spawnY, 0), Quaternion.identity);

		chunk.transform.SetParent(this.transform);
		activeChunks.Enqueue(chunk);

		nextChunkX += chunkWidth;
	}

	void RemoveOldChunk()
	{
		// Lowered threshold: Keeps exactly enough chunks to cover the camera view
		if (activeChunks.Count > chunksAhead + 1)
		{
			GameObject oldChunk = activeChunks.Dequeue();
			Destroy(oldChunk);
		}
	}
}