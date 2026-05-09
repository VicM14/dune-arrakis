using Dune.SimulationService.Events;
using Dune.SimulationService.Services;
using MediatR;

namespace Dune.SimulationService.Handlers;

/// <summary>
/// Handler que reacciona al evento de fin de mes guardando automáticamente
/// el estado actual de la partida en el PersistenceService.
///
/// Este handler demuestra consistencia eventual: la ronda se completa
/// primero (el controller devuelve Ok), y la persistencia ocurre como
/// efecto secundario asíncrono del evento. Si la persistencia falla,
/// el estado en memoria sigue siendo correcto; solo se pierde la
/// posibilidad de recuperación ante un crash.
///
/// En la sección 2.7 del PDF: "muchas arquitecturas modernas adoptan
/// consistencia eventual y propagan cambios mediante eventos o mensajería".
/// </summary>
public class AutoPersistenceHandler : INotificationHandler<SimulationMonthEndedEvent>
{
    private readonly SimulationState _state;
    private readonly IPersistenceClient _persistence;
    private readonly ILogger<AutoPersistenceHandler> _logger;

    public AutoPersistenceHandler(
        SimulationState state,
        IPersistenceClient persistence,
        ILogger<AutoPersistenceHandler> logger)
    {
        _state = state;
        _persistence = persistence;
        _logger = logger;
    }

    public async Task Handle(SimulationMonthEndedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[PERSISTENCIA] Guardando estado tras el mes {Mes}...",
            notification.MesCompletado);

        bool ok = await _persistence.GuardarPartidaAsync(_state.PartidaActual, cancellationToken);

        if (ok)
            _logger.LogInformation("[PERSISTENCIA] Estado guardado correctamente.");
        else
            _logger.LogWarning("[PERSISTENCIA] Guardado falló — el estado en memoria sigue siendo válido (consistencia eventual).");
    }
}
