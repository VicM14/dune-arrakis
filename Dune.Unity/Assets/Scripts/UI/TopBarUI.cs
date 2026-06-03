using UnityEngine;
using TMPro;

public class TopBarUI : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI textoSolaris;
    public TextMeshProUGUI textoRonda;
    public TextMeshProUGUI textoNombreJugador;

    public void Actualizar(PartidaData partida)
    {
        if (textoSolaris != null)
            textoSolaris.text = $"₡ {partida.Solaris:N0}";

        if (textoRonda != null)
            textoRonda.text = $"MES {partida.MesActual}";

        if (textoNombreJugador != null)
            textoNombreJugador.text = partida.NombreJugador.ToUpper();
    }
}