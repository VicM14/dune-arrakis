using UnityEngine;
using TMPro;

public class DetailPanelUI : MonoBehaviour
{
    [Header("Contenido")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoCuerpo;

    public void MostrarCriatura(CriaturaData criatura)
    {
        if (textoTitulo != null)
            textoTitulo.text = criatura.Nombre.ToUpper();

        string estado = criatura.EnLetargo ? "LETARGO" : $"Salud: {criatura.Salud:F0}";
        string adulta = criatura.EsAdulta ? "Adulta" : "Joven";

        if (textoCuerpo != null)
            textoCuerpo.text =
                $"{estado}\n" +
                $"Edad: {criatura.EdadActual} ({adulta})\n" +
                $"Especie: {criatura.Tipo}\n" +
                $"Hab: {HabitatNombre(criatura.Habitat)}\n" +
                $"Dieta: {DietaNombre(criatura.Dieta)}\n" +
                $"Favorita: {criatura.VecesFavorita}x";
    }

    public void MostrarInstalacion(InstalacionData inst)
    {
        if (textoTitulo != null)
            textoTitulo.text = inst.Nombre.ToUpper();

        int ocupacion = inst.Criaturas?.Count ?? 0;
        if (textoCuerpo != null)
            textoCuerpo.text =
                $"Codigo: {inst.Codigo}\n" +
                $"Criaturas: {ocupacion}/{inst.CapacidadMaxima}\n" +
                $"Hectareas: {inst.Hectareas}\n" +
                $"Suministros: {inst.Suministros}\n" +
                $"Medio: {HabitatNombre(inst.Medio)}\n" +
                $"Tipo: {TipoRecintoNombre(inst.TipoRecinto)}";
    }

    public void LimpiarDetalle()
    {
        if (textoTitulo != null) textoTitulo.text = "SELECCIONA";
        if (textoCuerpo != null) textoCuerpo.text = "Haz click en una\ninstalación o criatura\npara ver detalles.";
    }

    private string HabitatNombre(int h) => h switch { 0 => "Desierto", 1 => "Aéreo", 2 => "Subterráneo", _ => "?" };
    private string DietaNombre(int d) => d == 0 ? "Recolector" : "Depredador";
    private string TipoRecintoNombre(int t) => t switch
    {
        0 => "Roca Sellada",
        1 => "Escudo Estatico",
        2 => "Cupula Blindada",
        3 => "Pozo Reforzado",
        _ => "?"
    };
}
