using System.Collections.Generic;

namespace Dune.Domain;

public class Enclave
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public int Nivel { get; set; } = 1; // De 1 a 5
    public int PoblacionVisitantes { get; set; } = 100;
    public List<Instalacion> Instalaciones { get; set; } = new();

    // Algoritmo de Visitantes (Sección 3.3)
    public void ActualizarVisitantes()
    {
        // V_actual = V_anterior * (1 + 0.1 * Nivel) - (V_anterior * 0.05)
        // Simplificado: Crecimiento basado en nivel menos abandono del 5%
        double crecimiento = 0.1 * Nivel;
        double abandono = 0.05;

        PoblacionVisitantes = (int)(PoblacionVisitantes * (1 + crecimiento - abandono));

        if (PoblacionVisitantes < 0) PoblacionVisitantes = 0;
    }
}
