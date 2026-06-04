using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Image = UnityEngine.UI.Image;

public class InstallationSprite : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias visuales")]
    public Image iconoInstalacion;
    public Image indicadorTipo;        // barra de color lateral
    public TextMeshProUGUI textoCodigo;
    public TextMeshProUGUI textoOcupacion;

    private InstalacionData datos;
    private Color colorBase;

    public void Inicializar(InstalacionData inst, Sprite sprite, Color color)
    {
        datos = inst;
        colorBase = color;

        if (iconoInstalacion != null)
        {
            iconoInstalacion.sprite = sprite;
            if (sprite == null) iconoInstalacion.color = color;
        }

        if (indicadorTipo != null) indicadorTipo.color = color;

        if (textoCodigo != null) textoCodigo.text = inst.Codigo;

        if (textoOcupacion != null)
        {
            int ocu = inst.Criaturas?.Count ?? 0;
            textoOcupacion.text = $"{ocu}/{inst.CapacidadMaxima}";
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        UIManager.Instance?.detailPanel?.MostrarInstalacion(datos);

        if (datos.Criaturas != null && datos.Criaturas.Count > 0)
        {
            var mejor = datos.Criaturas[0];
            foreach (var c in datos.Criaturas)
                if (c.Salud > mejor.Salud) mejor = c;
            UIManager.Instance?.detailPanel?.MostrarCriatura(mejor);
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (iconoInstalacion != null) iconoInstalacion.color = Color.white;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (iconoInstalacion != null)
            iconoInstalacion.color = datos.Tipo == 0
                ? new Color(0.32f, 0.53f, 1f)
                : new Color(1f, 0.80f, 0.47f);
    }
}