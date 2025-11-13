using UnityEngine;

public class TerrainHeight : MonoBehaviour
{
    public Terrain terreno;
    
    [ContextMenu("Ajustar Todos Los Puntos Al Terreno")]
    void AjustarAltura()
    {
        if (terreno == null) terreno = FindObjectOfType<Terrain>();
        
        foreach (Transform punto in transform)
        {
            Vector3 pos = punto.position;
            float alturaTerreno = terreno.SampleHeight(pos);
            punto.position = new Vector3(pos.x, alturaTerreno + 0.5f, pos.z);
        }
    }
}