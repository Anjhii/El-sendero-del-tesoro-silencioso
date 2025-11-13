using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    public float speed = 2f;
    
    void Start()
    {
        Debug.Log("Note creada: " + gameObject.name);
        // Asegurar que tiene Rigidbody2D
        if (GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogError("Note no tiene Rigidbody2D!");
        }
    }

    void Update()
    {
        // Mover hacia abajo
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        Debug.Log("Note position Y: " + transform.position.y);
        
        // Destruir si sale de pantalla
        if(transform.position.y < -10f)
        {
            Debug.Log("Note destruida por salir de pantalla");
            Destroy(gameObject);
        }
    }
}