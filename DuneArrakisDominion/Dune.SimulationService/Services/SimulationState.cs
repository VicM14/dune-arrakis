using Dune.Domain;

namespace Dune.SimulationService.Services;

/// <summary>
/// Estado en memoria del SimulationService. Encapsula la partida activa y el
/// mecanismo de exclusión mutua que serializa todas las operaciones de
/// simulación para garantizar la consistencia del estado durante una ronda
/// o una acción del jugador.
///
/// Registrado como Singleton en el contenedor DI: hay una única instancia de
/// SimulationState por proceso. Es la fuente autoritativa del estado del juego.
/// </summary>
public class SimulationState
{
    /// <summary>Partida actualmente activa en el servicio.</summary>
    public Partida PartidaActual { get; set; } = new Partida();

    /// <summary>
    /// Semáforo binario que serializa las operaciones de simulación.
    /// Cada endpoint hace WaitAsync() al entrar y Release() en finally.
    /// Esto garantiza que dos peticiones concurrentes no provoquen condiciones
    /// de carrera sobre PartidaActual.
    /// </summary>
    public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
}
