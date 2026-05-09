using Dune.SimulationService.Events;
using MediatR;

namespace Dune.SimulationService.Handlers;

/// <summary>
/// Handler que se suscribe al evento de fin de mes y escribe un resumen
/// en el log estructurado de ASP.NET Core. Demuestra el patrón
/// publish/subscribe: este handler no sabe quién publicó el evento
/// ni qué otros handlers existen.
///
/// En un sistema distribuido real, este handler podría enviar el evento
/// a un servicio de auditoría externo, a un bus de mensajes (RabbitMQ,
/// Azure Service Bus) o a un sistema de monitorización.
/// </summary>
public class EventLogHandler : INotificationHandler<SimulationMonthEndedEvent>
{
    private readonly ILogger<EventLogHandler> _logger;

    public EventLogHandler(ILogger<EventLogHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SimulationMonthEndedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[EVENTO] Mes {Mes} completado — Ingresos: {Ingresos:F2} Solaris, " +
            "Criaturas vivas: {Vivas}, Descartadas: {Descartadas}, " +
            "Solaris restantes: {Solaris:F2}, Timestamp: {Ts:O}",
            notification.MesCompletado,
            notification.IngresosMes,
            notification.CriaturasVivas,
            notification.CriaturasDescartadas,
            notification.SolarisAlFinalizar,
            notification.Timestamp);

        return Task.CompletedTask;
    }
}
