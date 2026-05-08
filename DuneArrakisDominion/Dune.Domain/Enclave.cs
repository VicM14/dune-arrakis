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
    public int CapacidadAlmacen => Hectareas * 3;

    /// <summary>Espacio libre en el almacén general.</summary>
    public int EspacioLibreEnAlmacen => Math.Max(0, CapacidadAlmacen - Suministros);

    public void ActualizarVisitantes()
    {
        int hectareasInst = Instalaciones.Sum(i => i.Hectareas);
        if (Hectareas == 0) return;

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
