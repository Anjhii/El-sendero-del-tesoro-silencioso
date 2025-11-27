using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotationByJoystick : MonoBehaviour
{
    [Header("Input del joystick derecho")]
    public InputActionReference accionRotacion;  // Joystick derecho

    [Header("Sensibilidad")]
    public float sensibilidadX = 120f;   // Giro horizontal
    public float sensibilidadY = 90f;    // Giro vertical

    [Header("Límites de inclinación vertical")]
    public float minY = -60f;
    public float maxY = 60f;

    private float rotacionY = 0f; // vertical (pitch)
    private float rotacionX = 0f; // horizontal (yaw)

    void Start()
    {
        if (accionRotacion != null)
            accionRotacion.action.Enable();

        // Inicializar con la rotación actual
        Vector3 rot = transform.localEulerAngles;
        rotacionX = rot.y;
        rotacionY = rot.x;
    }

    void Update()
    {
        Vector2 input = accionRotacion.action.ReadValue<Vector2>();

        // Joystick horizontal → girar eje Y
        rotacionX += input.x * sensibilidadX * Time.deltaTime;

        // Joystick vertical → mirar arriba/abajo
        rotacionY -= input.y * sensibilidadY * Time.deltaTime;

        // Limitar inclinación vertical
        rotacionY = Mathf.Clamp(rotacionY, minY, maxY);

        // Aplicar rotación final
        transform.localRotation = Quaternion.Euler(rotacionY, rotacionX, 0f);
    }

    void OnDisable()
    {
        if (accionRotacion != null)
            accionRotacion.action.Disable();
    }
}
