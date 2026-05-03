using System.Collections.Generic;

namespace Dune.Domain;

public class Enclave
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public int Nivel { get; set; } = 1; // De 1 a 5
    public int PoblacionVisitantes { get; set; } = 100;
    public List<Instalacion> Instalaciones { get; set; } = new();

    public int Hectareas { get; set; } = 100; // valor por defecto razonable
    public int VisitantesMensualesBase { get; set; } // visitantesMesEnclave del enunciado
    public NivelAdquisitivo NivelAdquisitivo { get; set; } = NivelAdquisitivo.MEDIO;
    public double Suministros { get; set; } = 0;
    public TipoActividad TipoEnclave { get; set; } // CRIANZA o EXHIBICION

    public void ActualizarVisitantes()
    {
        int hectareasInst = Instalaciones.Sum(i => i.Hectareas);
        if (Hectareas == 0) return;

        // Calcular salud media de todas las criaturas
        var todasCriaturas = Instalaciones.SelectMany(i => i.Criaturas).ToList();
        double saludMedia = todasCriaturas.Count > 0
            ? todasCriaturas.Average(c => c.Salud) : 100;

        double llegan = (VisitantesMensualesBase * (double)hectareasInst / Hectareas)
                        * (saludMedia / 100.0);

        double abandonan = PoblacionVisitantes
                         - (PoblacionVisitantes * (double)hectareasInst / Hectareas)
                         * (saludMedia / 100.0);

        PoblacionVisitantes = (int)(PoblacionVisitantes + llegan - abandonan);
        if (PoblacionVisitantes < 0) PoblacionVisitantes = 0;
    }
}
