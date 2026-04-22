using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain
{
public class Instalacion
{
    public string Codigo { get; set; } = string.Empty;
    public double CosteConstruccion { get; set; }
    public Medio MedioCompatible { get; set; }
    public Alimentacion AlimentacionCompatible { get; set; }
    public int CapacidadMaxima { get; set; }
    public double Hectareas { get; set; }
    public List<Criatura> Criaturas { get; set; } = new();
    public TipoActividad Tipo { get; set; }

    // Fórmula de Donación (Sección 3.4)
    public double CalcularDonacion(Criatura c, NivelAdquisitivo nivel)
    {
        int sigma = nivel switch
        {
            NivelAdquisitivo.BAJO => 1,
            NivelAdquisitivo.MEDIO => 15,
            NivelAdquisitivo.ALTO => 30,
            _ => 1
        };

        return 10 * (c.Salud / 100.0) * ((double)c.EdadActual / c.EdadAdulta) * sigma;
    }
}
}

