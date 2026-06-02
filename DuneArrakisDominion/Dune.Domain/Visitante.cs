namespace Dune.Domain;

/// <summary>
/// Visitante de un enclave de exhibición. En el modelo del PDF un visitante
/// tiene poco estado individual: hereda el nivel adquisitivo del enclave que
/// visita (Sección 3.3) y dona en función del estado de la criatura que elige
/// como favorita (Sección 3.4).
///
/// En la implementación actual no instanciamos visitantes individuales para
/// el cálculo de donaciones — se calcula directamente sobre la población total
/// del enclave. Esta clase queda como tipo de dominio por si en el futuro
/// queremos modelado más fino (perfiles de visitante, comportamientos, etc.).
/// </summary>
public class Visitante
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NivelAdquisitivo Nivel { get; set; }

    public Visitante(NivelAdquisitivo nivel)
    {
        Nivel = nivel;
    }

    /// <summary>Constructor sin parámetros para deserialización.</summary>
    public Visitante() { }
}
