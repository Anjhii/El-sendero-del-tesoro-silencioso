using System.Collections;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab; // Arrastra aquí el prefab de Note
    public Transform[] spawnPoints; // Posiciones donde pueden aparecer las notas
    public float minSpawnTime = 0.5f;
    public float maxSpawnTime = 2f;
    public float noteSpeed = 2f;

    void Start()
    {
        StartCoroutine(SpawnNotes());
    }

    IEnumerator SpawnNotes()
    {
        while (true)
        {
            // Esperar tiempo aleatorio
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            // Elegir posición aleatoria
            if (spawnPoints.Length > 0)
            {
                int randomIndex = Random.Range(0, spawnPoints.Length);
                SpawnNoteAtPosition(spawnPoints[randomIndex].position);
            }
        }
    }

    void SpawnNoteAtPosition(Vector3 position)
    {
        if (notePrefab != null)
        {
            GameObject newNote = Instantiate(notePrefab, position, Quaternion.identity);
            NoteMovement noteMovement = newNote.GetComponent<NoteMovement>();
            if (noteMovement != null)
            {
                noteMovement.speed = noteSpeed;
            }
            Debug.Log($"🎵 Nota generada en posición: {position}");
        }
        else
        {
            Debug.LogError("❌ Note Prefab no asignado en el NoteSpawner!");
        }
    }
}
