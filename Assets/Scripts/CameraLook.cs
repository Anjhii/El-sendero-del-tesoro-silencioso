using UnityEngine;

public class CameraOrientacionCarril : MonoBehaviour
{
    [Header("Ángulos de orientación por carril (Y)")]
    public float[] angulosCarril = { 0f, 180f, 240f };

    [Header("Carril activo")]
    public int carrilActivo = 0;

    [Header("Suavidad de rotación")]
    public float velocidadRotacion = 5f;

    void Update()
    {
        AplicarOrientacionCarril();
    }

    void AplicarOrientacionCarril()
    {
        if (carrilActivo < 0 || carrilActivo >= angulosCarril.Length)
            return;

        float anguloY = angulosCarril[carrilActivo];

        Quaternion rotObjetivo = Quaternion.Euler(0f, anguloY, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotObjetivo,
            velocidadRotacion * Time.deltaTime
        );
    }

    // Puedes llamar a esto desde otro script si cambias de carril
    public void CambiarCarril(int nuevoCarril)
    {
        carrilActivo = nuevoCarril;
    }
}
