using Dune.Domain;
using MediatR;

namespace Dune.SimulationService.Events;

/// <summary>
/// Evento publicado al finalizar la ejecución de una ronda mensual.
///
/// Implementa INotification de MediatR, lo que permite que cualquier número
/// de INotificationHandler&lt;SimulationMonthEndedEvent&gt; reaccione de forma
/// independiente y paralela (configurado con TaskWhenAllPublisher).
///
/// Este patrón publish/subscribe in-process cubre directamente los apartados
/// 2.2 (comunicación orientada a eventos) y 2.4 (sistemas de mensajes) del PDF.
/// </summary>
public class SimulationMonthEndedEvent : INotification
{
    /// <summary>Mes que acaba de finalizar.</summary>
    public int MesCompletado { get; init; }

    /// <summary>Ingresos por donaciones de exhibición durante el mes.</summary>
    public double IngresosMes { get; init; }

    /// <summary>Número total de criaturas vivas en todos los enclaves tras el mes.</summary>
    public int CriaturasVivas { get; init; }

    /// <summary>Número de criaturas descartadas (letargo) durante el mes.</summary>
    public int CriaturasDescartadas { get; init; }

    /// <summary>Solaris del jugador al terminar el mes.</summary>
    public double SolarisAlFinalizar { get; init; }

    /// <summary>Marca temporal UTC del evento.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
