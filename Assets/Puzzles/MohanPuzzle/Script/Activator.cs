using UnityEngine;
using UnityEngine.InputSystem;

public class Activator : MonoBehaviour
{
    public int laneNumber = 0;
    public InputActionReference laneAction; // ← Asigna en inspector
    
    bool active = false;
    GameObject note;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        
        if (laneAction != null)
        {
            laneAction.action.Enable();
            laneAction.action.performed += OnLanePressed;
        }
    }

    void OnLanePressed(InputAction.CallbackContext context)
    {
        if (active && note != null)
        {
            Destroy(note);
            active = false;
            note = null;
            StartCoroutine(Pressed());
            // Agregar puntos aquí
        }
    }

    // Resto del código igual...
    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.CompareTag("Note"))
        {
            active = true;
            note = col.gameObject;
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if(col.CompareTag("Note") && col.gameObject == note)
        {
            active = false;
            note = null;
        }
    }

    System.Collections.IEnumerator Pressed()
    {
        Color original = sr.color;
        sr.color = Color.gray;
        yield return new WaitForSeconds(0.1f);
        sr.color = original;
    }

    void OnDestroy()
    {
        if (laneAction != null)
            laneAction.action.performed -= OnLanePressed;
    }
}