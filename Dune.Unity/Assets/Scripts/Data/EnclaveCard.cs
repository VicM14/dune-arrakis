using UnityEngine;
using TMPro;
using System.Linq;

public class EnclaveCard : MonoBehaviour
{
    [SerializeField] private TMP_Text txtNombre;
    [SerializeField] private TMP_Text txtRecursos;
    [SerializeField] private TMP_Text txtInstalaciones;

    public void Inicializar(EnclaveData enclave)
    {
        txtNombre.text = $"[{enclave.TipoEnclave}] {enclave.Nombre}";
        txtRecursos.text = $"Almacén: {enclave.Suministros}/{enclave.CapacidadAlmacen}" +
                           $"  |  Visitantes: {enclave.PoblacionVisitantes}" +
                           $"  |  Nivel: {enclave.NivelAdquisitivo}";

        var sb = new System.Text.StringBuilder();
        foreach (var inst in enclave.Instalaciones)
        {
            sb.AppendLine($"  ▸ {inst.Nombre}  [{inst.Tipo}]" +
                          $"  Stock: {inst.Suministros}/{inst.CosteConstruccion}" +
                          $"  Criaturas: {inst.Criaturas?.Count ?? 0}/{inst.CapacidadMaxima}");

            // Criaturas ordenadas por salud descendente (requisito sección 3.8 del PDF)
            var ordenadas = inst.Criaturas?
                .OrderByDescending(c => c.Salud)
                .ToList() ?? new();

            foreach (var c in ordenadas)
            {
                string estado = c.EnLetargo ? "⚠ LETARGO" :
                                !c.EsAdulta ? "joven" : "adulta";
                sb.AppendLine($"      • {c.Nombre}  Salud:{c.Salud:F0}  " +
                              $"Edad:{c.EdadActual}/{c.EdadAdulta}  [{estado}]");
            }
        }
        txtInstalaciones.text = sb.ToString();
    }
}