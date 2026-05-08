namespace Dune.Domain;

public class Instalacion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public TipoActividad Tipo { get; set; }
    public int CapacidadMaxima { get; set; } = 0;
    public List<Criatura> Criaturas { get; set; } = new();
    public List<Visitante> VisitantesActuales { get; set; } = new();
    public int Hectareas { get; set; }
    public int CosteConstruccion { get; set; }

    /// <summary>
    /// Stock interno de suministros de la instalación.
    /// Se llena al construir (SuministrosIniciales) y se rellena moviendo
    /// suministros desde el almacén general del enclave (gratis).
    /// El stock total no puede superar el valor numérico de CosteConstruccion.
    /// </summary>
    public int Suministros { get; set; } = 0;

    /// <summary>
    /// Suministros con los que la instalación nace al construirse,
    /// según la tabla de la Sección 3.4 del PDF.
    /// </summary>
    public int SuministrosIniciales { get; set; } = 0;

    /// <summary>
    /// Cálculo de donaciones de la instalación.
    /// El factor σ depende del nivel adquisitivo del enclave que contiene
    /// esta instalación; lo recibimos por parámetro para no acoplar a Enclave.
    /// </summary>
    public double CalcularDonacionesTotales(NivelAdquisitivo nivelEnclave)
    {
        double sigma = nivelEnclave switch
        {
            NivelAdquisitivo.BAJO => 1,
            NivelAdquisitivo.MEDIO => 15,
            NivelAdquisitivo.ALTO => 30,
            _ => 1
        };

        double total = 0;
        foreach (var v in VisitantesActuales)
        {
            var favorita = Criaturas
                .Where(c => c.Salud > 0)
                .OrderByDescending(c => c.Salud)
                .FirstOrDefault();

            if (favorita == null) continue;

            // Fórmula del PDF: donacion = 10 × (salud/100) × (edad/edadAdulta) × σ
            double donacion = 10
                * (favorita.Salud / 100.0)
                * ((double)favorita.EdadActual / favorita.EdadAdulta)
                * sigma;

            favorita.VecesFavorita++;
            total += donacion;
        }
        return total;
    }
}
