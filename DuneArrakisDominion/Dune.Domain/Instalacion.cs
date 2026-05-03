using System.Collections.Generic;

namespace Dune.Domain;

public class Instalacion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public TipoActividad Tipo { get; set; }
    public int CapacidadMaxima { get; set; } = 5;
    public List<Criatura> Criaturas { get; set; } = new();
    public List<Visitante> VisitantesActuales { get; set; } = new();
    public int Hectareas { get; set; }
    public int CosteConstruccion { get; set; }
    public double CalcularCosteMantenimiento()
    {
        // Convención razonable: 1% del coste de construcción por mes
        return CosteConstruccion * 0.01;
    }

    public double CalcularDonacionesTotales()
    {
        double total = 0;
        foreach (var v in VisitantesActuales)
        {
            // La fórmula solo aplica a la criatura "favorita" — elegida por el visitante
            // Por simplificación: cada visitante elige la criatura con más salud
            var favorita = Criaturas.Where(c => c.Salud > 0)
                                    .OrderByDescending(c => c.Salud)
                                    .FirstOrDefault();
            if (favorita == null) continue;

            double sigma = v.Nivel switch
            {
                NivelAdquisitivo.BAJO => 1,
                NivelAdquisitivo.MEDIO => 15,
                NivelAdquisitivo.ALTO => 30,
                _ => 1
            };
            double donacion = 10 * (favorita.Salud / 100.0)
                                 * ((double)favorita.EdadActual / favorita.EdadAdulta)
                                 * sigma;
            total += donacion;
        }
        return total;
    }
}



