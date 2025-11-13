using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class SpriteSet
    {
        public Sprite luminoso;
        public Sprite oscuro;
    }

    [Header("UI References")]
    public TMP_Text nombreText;
    public Image[] fragmentoImages;

    [Header("Sprites por Tipo de Fragmento")]
    public SpriteSet spritesMohan;
    public SpriteSet spritesMadremonte;
    public SpriteSet spritesBachue;

    private bool gameManagerListo = false;

    void Start()
    {
        StartCoroutine(EsperarGameManager());
    }

    IEnumerator EsperarGameManager()
    {
        // Esperar hasta que GameManager esté listo
        while (GameManager.Instance == null || GameManager.Instance.fragmentManager == null)
        {
            yield return null;
        }
        
        gameManagerListo = true;
        ActualizarUI();
    }
    
    public void ActualizarUI()
    {
        if (!gameManagerListo)
        {
            Debug.LogWarning("GameManager no está listo todavía");
            return;
        }

        // Verificar todas las referencias críticas
        if (nombreText == null)
        {
            Debug.LogError("nombreText no está asignado en el Inspector");
            return;
        }

        if (fragmentoImages == null || fragmentoImages.Length == 0)
        {
            Debug.LogError("fragmentoImages no está asignado en el Inspector");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance es null");
            return;
        }

        if (GameManager.Instance.fragmentManager == null)
        {
            Debug.LogError("FragmentManager es null");
            return;
        }

        // Actualizar nombre
        nombreText.text = GameManager.Instance.playerName;
        
        // Actualizar fragmentos
        for (int i = 0; i < fragmentoImages.Length; i++)
        {
            if (fragmentoImages[i] != null)
            {
                try
                {
                    var estado = GameManager.Instance.fragmentManager.ObtenerEstadoPorIndice(i);
                    var tipo = GameManager.Instance.fragmentManager.ObtenerTipoPorIndice(i);
                    
                    if (estado == FragmentManager.EstadoFragmento.NoObtenido)
                    {
                        fragmentoImages[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        fragmentoImages[i].gameObject.SetActive(true);
                        Sprite spriteAAplicar = ObtenerSpritePorTipoYEstado(tipo, estado);
                        if (spriteAAplicar != null)
                        {
                            fragmentoImages[i].sprite = spriteAAplicar;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error actualizando fragmento {i}: {e.Message}");
                }
            }
        }
    }

    Sprite ObtenerSpritePorTipoYEstado(FragmentManager.TipoFragmento tipo, FragmentManager.EstadoFragmento estado)
    {
        switch (tipo)
        {
            case FragmentManager.TipoFragmento.Mohan:
                return (estado == FragmentManager.EstadoFragmento.Luminoso) ? 
                    spritesMohan.luminoso : spritesMohan.oscuro;
                    
            case FragmentManager.TipoFragmento.Madremonte:
                return (estado == FragmentManager.EstadoFragmento.Luminoso) ? 
                    spritesMadremonte.luminoso : spritesMadremonte.oscuro;
                    
            case FragmentManager.TipoFragmento.Bachue:
                return (estado == FragmentManager.EstadoFragmento.Luminoso) ? 
                    spritesBachue.luminoso : spritesBachue.oscuro;
                    
            default:
                Debug.LogWarning($"Tipo de fragmento no reconocido: {tipo}");
                return null;
        }
    }
    
    public void ActivarBrilloTemporal(int indexFragmento)
    {
        if (indexFragmento >= 0 && indexFragmento < fragmentoImages.Length && fragmentoImages[indexFragmento] != null)
        {
            StartCoroutine(BrilloTemporal(fragmentoImages[indexFragmento]));
        }
    }
    
    System.Collections.IEnumerator BrilloTemporal(Image img)
    {
        if (img != null && img.material != null)
        {
            img.material.SetFloat("_Glow", 1f);
            yield return new WaitForSeconds(1f);
            img.material.SetFloat("_Glow", 0f);
        }
    }

    // Método para forzar actualización cuando sea necesario
    public void ReintentarActualizacion()
    {
        if (GameManager.Instance != null && GameManager.Instance.fragmentManager != null)
        {
            ActualizarUI();
        }
    }
}