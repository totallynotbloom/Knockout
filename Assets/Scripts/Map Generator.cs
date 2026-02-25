using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessMap : MonoBehaviour
{
    public GameObject[] mapChunks; // Your different map prefabs
    public Transform player;        // Player transform
    public float chunkWidth = 50f;  // Width of each chunk
    public int chunksAhead = 3;     // How many chunks to keep ahead of player

    private float nextChunkX = 0f;  // Where to spawn next chunk
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    void Start()
    {
        // Pre-spawn some chunks
        for (int i = 0; i < chunksAhead; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        // Check if player is close to next chunk
        if (player.position.x + (chunksAhead * chunkWidth) > nextChunkX - chunkWidth)
        {
            SpawnChunk();
            RemoveOldChunk();
        }
    }

    void SpawnChunk()
    {
        // Pick a random chunk prefab
        GameObject chunk = Instantiate(mapChunks[Random.Range(0, mapChunks.Length)]);
        chunk.transform.position = new Vector3(nextChunkX, 0, 0);
        activeChunks.Enqueue(chunk);

        nextChunkX += chunkWidth;
    }

    void RemoveOldChunk()
    {
        // Optional: remove chunks behind player
        if (activeChunks.Count > chunksAhead + 1)
        {
            GameObject oldChunk = activeChunks.Dequeue();
            Destroy(oldChunk);
        }
    }
}
