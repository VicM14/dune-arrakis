using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DetailPanelUI : MonoBehaviour
{
    [Header("Cabecera")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoCuerpo;

    [Header("Lista de criaturas (ordenada por salud)")]
    public Transform contenedorCriaturas;
    public GameObject prefabCreatureCard;

    public CriaturaData CriaturaSeleccionada { get; private set; }
    public InstalacionData InstalacionSeleccionada { get; private set; }

    private readonly List<GameObject> cards = new();

    // ─── Mostrar instalación ─────────────────────────────────────────────

    public void MostrarInstalacion(InstalacionData inst)
    {
        InstalacionSeleccionada = inst;
        CriaturaSeleccionada = null;

        if (textoTitulo != null) textoTitulo.text = inst.Nombre.ToUpper();

        int ocupacion = inst.Criaturas?.Count ?? 0;
        if (textoCuerpo != null)
            textoCuerpo.text =
                $"Código: {inst.Codigo}\n" +
                $"Criaturas: {ocupacion}/{inst.CapacidadMaxima}\n" +
                $"Hectáreas: {inst.Hectareas}\n" +
                $"Suministros: {inst.Suministros}/{inst.CosteConstruccion}\n" +
                $"Medio: {HabitatNombre(inst.Medio)}\n" +
                $"Dieta: {DietaNombre(inst.Alimentacion)}\n" +
                $"Tipo: {TipoRecintoNombre(inst.TipoRecinto)}";

        // Mostrar criaturas ordenadas por salud descendente
        GenerarCards(inst.Criaturas);
    }

    // ─── Mostrar criatura ────────────────────────────────────────────────

    public void MostrarCriatura(CriaturaData criatura)
    {
        CriaturaSeleccionada = criatura;

        if (textoTitulo != null) textoTitulo.text = criatura.Nombre.ToUpper();

        string estado = criatura.EnLetargo ? "⚠ EN LETARGO"
                      : $"Salud: {criatura.Salud:F0}/100";
        string adulta = criatura.EsAdulta ? "Adulta" : "Joven";
        string traslado = criatura.PuedeTraslado
                        ? "✓ Puede trasladarse"
                        : "✗ No puede trasladarse";

        if (textoCuerpo != null)
            textoCuerpo.text =
                $"{estado}\n" +
                $"Especie: {criatura.Tipo}\n" +
                $"Edad: {criatura.EdadActual} ({adulta})\n" +
                $"Ed. adulta: {criatura.EdadAdulta}\n" +
                $"Hábitat: {HabitatNombre(criatura.Habitat)}\n" +
                $"Dieta: {DietaNombre(criatura.Dieta)}\n" +
                $"Favorita: {criatura.VecesFavorita}x\n" +
                $"\n{traslado}";
    }

    // ─── Cards de criaturas ──────────────────────────────────────────────

    private void GenerarCards(List<CriaturaData> criaturas)
    {
        // Limpiar cards anteriores
        foreach (var c in cards) Destroy(c);
        cards.Clear();

        if (criaturas == null || criaturas.Count == 0 ||
            contenedorCriaturas == null || prefabCreatureCard == null) return;

        // Ordenar por salud descendente (requisito de la práctica)
        var ordenadas = new List<CriaturaData>(criaturas);
        ordenadas.Sort((a, b) => b.Salud.CompareTo(a.Salud));

        foreach (var criatura in ordenadas)
        {
            var go = Instantiate(prefabCreatureCard, contenedorCriaturas);
            var card = go.GetComponent<CreatureCardUI>();
            card?.Inicializar(criatura);
            cards.Add(go);
        }
    }

    // ─── Limpiar ─────────────────────────────────────────────────────────

    public void LimpiarDetalle()
    {
        CriaturaSeleccionada = null;
        InstalacionSeleccionada = null;
        foreach (var c in cards) Destroy(c);
        cards.Clear();
        if (textoTitulo != null) textoTitulo.text = "SELECCIONA";
        if (textoCuerpo != null) textoCuerpo.text = "Haz click en una\ninstalación o criatura.";
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

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