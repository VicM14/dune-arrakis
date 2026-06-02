using System.Text.Json.Serialization;

namespace Dune.Domain;

public class Enclave
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public int Nivel { get; set; } = 1; // De 1 a 5
    public int PoblacionVisitantes { get; set; } = 100;
    public List<Instalacion> Instalaciones { get; set; } = new();

    public int Hectareas { get; set; } = 100;
    public int VisitantesMensualesBase { get; set; }
    public NivelAdquisitivo NivelAdquisitivo { get; set; } = NivelAdquisitivo.MEDIO;

    /// <summary>
    /// Suministros disponibles en el almacén general del enclave.
    /// Capacidad máxima = 3 × Hectareas (Sección 3.3 del PDF).
    /// Cada unidad tiene un coste fijo de 5 solaris al comprarla.
    /// </summary>
    public int Suministros { get; set; } = 0;

    public TipoActividad TipoEnclave { get; set; }

    /// <summary>Capacidad máxima del almacén general (Sección 3.3: triple de las hectáreas).</summary>
    [JsonIgnore]
    public int CapacidadAlmacen => Hectareas * 3;

    /// <summary>Espacio libre en el almacén general.</summary>
    [JsonIgnore]
    public int EspacioLibreEnAlmacen => Math.Max(0, CapacidadAlmacen - Suministros);

    public void ActualizarVisitantes()
    {
        if (Hectareas == 0) return;

        int hectareasInst = Instalaciones.Sum(i => i.Hectareas);

        // Salud media de las criaturas vivas del enclave.
        // Si no hay criaturas vivas, no llegan visitantes — un parque sin animales
        // no tiene atractivo (interpretación del PDF Sección 3.3, donde la
        // saludMediaCriaturas multiplica directamente la fórmula de visitantes).
        var criaturasVivas = Instalaciones
            .SelectMany(i => i.Criaturas)
            .Where(c => c.Salud > 0)
            .ToList();

        if (criaturasVivas.Count == 0)
        {
            // Sin criaturas vivas: nadie llega y todos los actuales se van.
            PoblacionVisitantes = 0;
            return;
        }

        double saludMedia = criaturasVivas.Average(c => c.Salud);

        double llegan = (VisitantesMensualesBase * (double)hectareasInst / Hectareas)
                        * (saludMedia / 100.0);

        double abandonan = PoblacionVisitantes
                         - (PoblacionVisitantes * (double)hectareasInst / Hectareas)
                         * (saludMedia / 100.0);

        PoblacionVisitantes = (int)(PoblacionVisitantes + llegan - abandonan);
        if (PoblacionVisitantes < 0) PoblacionVisitantes = 0;
    }
}
