namespace Dune.Domain;

public class Instalacion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Código de instalación según la tabla del PDF: ADR05, ADP03, AAV02, ASU04, EDR02, EDP03, EAV02, ESU03.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public TipoActividad Tipo { get; set; }
    public int CapacidadMaxima { get; set; } = 0;
    public List<Criatura> Criaturas { get; set; } = new();
    public List<Visitante> VisitantesActuales { get; set; } = new();
    public int Hectareas { get; set; }
    public int CosteConstruccion { get; set; }

    /// <summary>Medio para el que está preparada la instalación (Sección 3.4 del PDF).</summary>
    public Medio Medio { get; set; }

    /// <summary>Patrón de alimentación de las criaturas que la instalación aloja (Sección 3.4 del PDF).</summary>
    public Alimentacion Alimentacion { get; set; }

    /// <summary>Tipo de recinto físico (Sección 3.4 del PDF).</summary>
    public TipoRecinto TipoRecinto { get; set; }

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
    /// Calcula las donaciones totales que esta instalación de exhibición
    /// recibe en el mes actual, siguiendo la fórmula del PDF (Sección 3.4):
    ///
    ///     donacion_por_visitante = 10 × (salud/100) × (edad/edadAdulta) × σ
    ///
    /// donde σ depende del nivel adquisitivo del ENCLAVE (no del visitante)
    /// y la criatura usada en el cálculo es la favorita: aquella con mayor
    /// salud entre las vivas. La donación total se obtiene multiplicando por
    /// el número de visitantes del enclave que han llegado este mes.
    /// </summary>
    public double CalcularDonacionesTotales(int numVisitantes, NivelAdquisitivo nivelEnclave)
    {
        if (numVisitantes <= 0) return 0;

        var favorita = Criaturas
            .Where(c => c.Salud > 0 && c.EdadAdulta > 0)
            .OrderByDescending(c => c.Salud)
            .FirstOrDefault();

        if (favorita == null) return 0;

        double sigma = nivelEnclave switch
        {
            NivelAdquisitivo.BAJO => 1,
            NivelAdquisitivo.MEDIO => 15,
            NivelAdquisitivo.ALTO => 30,
            _ => 1
        };

        double donacionPorVisitante = 10.0
            * (favorita.Salud / 100.0)
            * ((double)favorita.EdadActual / favorita.EdadAdulta)
            * sigma;

        // Cada visitante que recibe la instalación elige a la criatura favorita
        // y dona. Modelamos el contador "VecesFavorita" con el número total
        // de visitantes que la han elegido este mes.
        favorita.VecesFavorita += numVisitantes;

        return donacionPorVisitante * numVisitantes;
    }
}
