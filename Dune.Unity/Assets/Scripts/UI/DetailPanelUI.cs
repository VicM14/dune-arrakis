using UnityEngine;
using TMPro;

public class DetailPanelUI : MonoBehaviour
{
    [Header("Contenido")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoCuerpo;

    public CriaturaData CriaturaSeleccionada { get; private set; }
    public InstalacionData InstalacionSeleccionada { get; private set; }

    public void MostrarCriatura(CriaturaData criatura)
    {
        CriaturaSeleccionada = criatura;
        InstalacionSeleccionada = null;

        if (textoTitulo != null)
            textoTitulo.text = criatura.Nombre.ToUpper();

        string estado = criatura.EnLetargo ? "⚠ LETARGO" : $"Salud: {criatura.Salud:F0}";
        string adulta = criatura.EsAdulta ? "Adulta" : "Joven";

        if (textoCuerpo != null)
            textoCuerpo.text =
                $"{estado}\n" +
                $"Edad: {criatura.EdadActual} ({adulta})\n" +
                $"Especie: {criatura.Tipo}\n" +
                $"Hábitat: {HabitatNombre(criatura.Habitat)}\n" +
                $"Dieta: {DietaNombre(criatura.Dieta)}\n" +
                $"Favorita: {criatura.VecesFavorita}x\n" +
                (criatura.PuedeTraslado ? "\n✓ Puede trasladarse" : "\n✗ No puede trasladarse");
    }

    public void MostrarInstalacion(InstalacionData inst)
    {
        InstalacionSeleccionada = inst;
        CriaturaSeleccionada = null;

        if (textoTitulo != null)
            textoTitulo.text = inst.Nombre.ToUpper();

        int ocupacion = inst.Criaturas?.Count ?? 0;
        if (textoCuerpo != null)
            textoCuerpo.text =
                $"Código: {inst.Codigo}\n" +
                $"Criaturas: {ocupacion}/{inst.CapacidadMaxima}\n" +
                $"Hectáreas: {inst.Hectareas}\n" +
                $"Suministros: {inst.Suministros}\n" +
                $"Medio: {HabitatNombre(inst.Medio)}\n" +
                $"Tipo: {TipoRecintoNombre(inst.TipoRecinto)}";
    }

    public void LimpiarDetalle()
    {
        CriaturaSeleccionada = null;
        InstalacionSeleccionada = null;
        if (textoTitulo != null) textoTitulo.text = "SELECCIONA";
        if (textoCuerpo != null) textoCuerpo.text = "Haz click en una\ninstalación o criatura.";
    }

    private string HabitatNombre(int h) => h switch
    { 0 => "Desierto", 1 => "Aéreo", 2 => "Subterráneo", _ => "?" };
    private string DietaNombre(int d) => d == 0 ? "Recolector" : "Depredador";
    private string TipoRecintoNombre(int t) => t switch
    {
        0 => "Roca Sellada",
        1 => "Escudo Estático",
        2 => "Cúpula Blindada",
        3 => "Pozo Reforzado",
        _ => "?"
    };
}
