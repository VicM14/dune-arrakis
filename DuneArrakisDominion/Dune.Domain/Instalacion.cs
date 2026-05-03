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

    public double CalcularDonacionesTotales(int nivelEnclave)
    {
        double total = 0;
        foreach (var v in VisitantesActuales)
        {
            foreach (var c in Criaturas)
            {
                if (c.Salud > 0)
                {
                    // Fórmula: (Salud/100) * (Edad * 10) * NivelEnclave * FactorVisitante
                    double factorV = (v.Nivel == NivelAdquisitivo.ALTO) ? 2.0 : 1.0;
                    total += (c.Salud / 100.0) * (c.EdadActual * 10) * nivelEnclave * factorV;
                }
            }
        }
        return total;
    }
}


