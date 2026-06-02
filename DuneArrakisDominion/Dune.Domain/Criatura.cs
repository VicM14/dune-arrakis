using System.Text.Json.Serialization;

namespace Dune.Domain;

/// <summary>
/// Clase base abstracta de todas las criaturas del juego (Sección 3.5 del PDF).
///
/// Para que System.Text.Json pueda serializar y deserializar referencias
/// polimórficas (List&lt;Criatura&gt; conteniendo subclases concretas) usamos
/// los atributos JsonDerivedType introducidos en .NET 7. Cada subclase queda
/// registrada con un discriminador "$type" que se incrustará en el JSON y
/// permitirá reconstruir la subclase correcta al deserializar.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(GusanoDeArena),     "GusanoDeArena")]
[JsonDerivedType(typeof(TigraLaza),         "TigraLaza")]
[JsonDerivedType(typeof(MuadDib),           "MuadDib")]
[JsonDerivedType(typeof(HalconDelDesierto), "HalconDelDesierto")]
[JsonDerivedType(typeof(TruchaDeArena),     "TruchaDeArena")]
public abstract class Criatura
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public double Salud { get; set; } = 100;
    public int EdadActual { get; set; } = 0;
    public int EdadAdulta { get; set; }
    public double ApetitoBase { get; set; }
    public Alimentacion Dieta { get; set; }
    public Medio Habitat { get; set; }
    public bool EnLetargo { get; set; } = false;
    public int VecesFavorita { get; set; } = 0;

    public abstract double CalcularIngestaRequerida(TipoActividad actividad);

    public void Alimentar(double cantidad, TipoActividad actividad)
    {
        double requerida = CalcularIngestaRequerida(actividad);
        double ratio = (requerida > 0) ? cantidad / requerida : 1;

        if (ratio < 0.25) Salud -= 30;
        else if (ratio < 0.75) Salud -= 20;
        else if (ratio < 1.0) Salud -= 10;
        else Salud = Math.Min(100, Salud + 5);

        if (Salud <= 0)
        {
            Salud = 0;
            EnLetargo = true;
        }
    }
}
