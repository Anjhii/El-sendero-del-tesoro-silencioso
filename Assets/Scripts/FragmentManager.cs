using UnityEngine;

public class FragmentManager : MonoBehaviour
{
    [System.Serializable]
    public class FragmentoData
    {
        public EstadoFragmento estado = EstadoFragmento.NoObtenido;
        public string nombreTemplo;
        public int puntajeObtenido;
        public TipoFragmento tipoFragmento;
    }
    
    public enum EstadoFragmento { NoObtenido, Oscuro, Luminoso }
    public enum TipoFragmento { Mohan, Madremonte, Bachue }

    [Header("Fragmentos del Juego")]
    public FragmentoData[] fragmentos = new FragmentoData[]
    {
        new FragmentoData { nombreTemplo = "Mohán", tipoFragmento = TipoFragmento.Mohan },
        new FragmentoData { nombreTemplo = "Madremonte", tipoFragmento = TipoFragmento.Madremonte },
        new FragmentoData { nombreTemplo = "Bachué", tipoFragmento = TipoFragmento.Bachue }
    };

    public void AsignarFragmento(string templo, int puntaje, int puntajeMaximo = 1000)
    {
        for (int i = 0; i < fragmentos.Length; i++)
        {
            if (fragmentos[i].nombreTemplo == templo)
            {
                fragmentos[i].puntajeObtenido = puntaje;
                fragmentos[i].estado = (puntaje >= puntajeMaximo * 0.7f) ? 
                    EstadoFragmento.Luminoso : EstadoFragmento.Oscuro;
                
                Debug.Log($"Fragmento {templo} asignado: {fragmentos[i].estado} (Puntaje: {puntaje}/{puntajeMaximo})");
                return;
            }
        }
        Debug.LogWarning("Templo no encontrado: " + templo);
    }

    public TipoFragmento ObtenerTipoPorIndice(int index)
    {
        if (index >= 0 && index < fragmentos.Length)
            return fragmentos[index].tipoFragmento;
        return TipoFragmento.Mohan;
    }

    public EstadoFragmento ObtenerEstadoPorIndice(int index)
    {
        if (index >= 0 && index < fragmentos.Length)
            return fragmentos[index].estado;
        return EstadoFragmento.NoObtenido;
    }

    public int ContarLuminosos()
    {
        int count = 0;
        foreach (var fragmento in fragmentos)
        {
            if (fragmento.estado == EstadoFragmento.Luminoso) count++;
        }
        return count;
    }

    public bool TodosLosFragmentosCompletados()
    {
        foreach (var fragmento in fragmentos)
        {
            if (fragmento.estado == EstadoFragmento.NoObtenido) return false;
        }
        return true;
    }
    
    [ContextMenu("Debug Fragmentos")]
    void DebugFragmentos()
    {
        foreach (var fragmento in fragmentos)
        {
            Debug.Log($"{fragmento.nombreTemplo}: {fragmento.estado} (Puntaje: {fragmento.puntajeObtenido})");
        }
        Debug.Log($"Luminosos: {ContarLuminosos()}/3");
    }
}