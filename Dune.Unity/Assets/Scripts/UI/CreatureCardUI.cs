using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Image = UnityEngine.UI.Image;

public class CreatureCardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Referencias")]
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoEspecie;
    public TextMeshProUGUI textoEdad;
    public Image barraRelleno;
    public Image fondoBarra;
    public Image fondoCard;

    private CriaturaData datos;

    private readonly Color colorSaludAlta = new Color(0.322f, 0.718f, 0.533f); // #52B788
    private readonly Color colorSaludMedia = new Color(1f, 0.549f, 0.259f);  // #FF8C42
    private readonly Color colorSaludBaja = new Color(0.816f, 0f, 0f);      // #D00000
    private readonly Color colorLetargo = new Color(0.286f, 0.286f, 0.361f); // #494959
    private readonly Color colorCardNormal = new Color(0.149f, 0.149f, 0.22f);  // #262638
    private readonly Color colorCardHover = new Color(0.235f, 0.235f, 0.345f); // #3C3C58

    public void Inicializar(CriaturaData criatura)
    {
        datos = criatura;

        if (textoNombre != null) textoNombre.text = criatura.Nombre;
        if (textoEspecie != null) textoEspecie.text = criatura.Tipo;
        if (textoEdad != null)
        {
            string estado = criatura.EnLetargo ? "LETARGO"
                          : criatura.EsAdulta ? "Adulta"
                          : "Joven";
            textoEdad.text = $"Edad {criatura.EdadActual} — {estado}";
        }

        // Barra de salud
        if (barraRelleno != null)
        {
            float pct = (float)(criatura.Salud / 100.0);
            barraRelleno.fillAmount = Mathf.Clamp01(pct);
            barraRelleno.color = criatura.EnLetargo ? colorLetargo
                               : pct > 0.6f ? colorSaludAlta
                               : pct > 0.3f ? colorSaludMedia
                               : colorSaludBaja;
        }

        // Fondo de la card según estado
        if (fondoCard != null)
            fondoCard.color = criatura.EnLetargo ? colorLetargo : colorCardNormal;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (datos != null)
            UIManager.Instance?.detailPanel?.MostrarCriatura(datos);
    }
}
