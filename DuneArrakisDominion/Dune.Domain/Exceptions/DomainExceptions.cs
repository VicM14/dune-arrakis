namespace Dune.Domain.Exceptions;

/// <summary>
/// Clase base de todas las excepciones de dominio del juego. Sirve como
/// punto de captura único en los controladores y handlers para distinguir
/// errores de regla de negocio de errores técnicos (HTTP, JSON, IO).
///
/// El patrón está inspirado en la jerarquía DDD clásica: cualquier clase
/// que extienda DomainException representa una violación de invariante o
/// regla del modelo, no un fallo de infraestructura.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Se lanza cuando el jugador no tiene solaris suficientes para una acción
/// (compra de suministros, construcción, traslado, descarte voluntario).
/// </summary>
public class InsufficientFundsException : DomainException
{
    public double Required { get; }
    public double Available { get; }

    public InsufficientFundsException(double required, double available)
        : base($"Solaris insuficientes. Requeridos: {required:F2}, disponibles: {available:F2}.")
    {
        Required = required;
        Available = available;
    }
}

/// <summary>
/// Se lanza cuando se referencia por id una entidad (enclave, instalación
/// o criatura) que no existe en el estado actual de la partida.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public string EntityId { get; }

    public EntityNotFoundException(string entityType, string entityId)
        : base($"{entityType} no encontrado(a): {entityId}.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Se lanza cuando una entidad no admite la operación solicitada por su
/// estado actual: instalación llena, almacén excedido, criatura en letargo,
/// suministros agotados, etc.
/// </summary>
public class InvalidEntityStateException : DomainException
{
    public InvalidEntityStateException(string message) : base(message) { }
}

/// <summary>
/// Se lanza cuando un traslado de criatura no respeta las reglas del PDF
/// (Sección 3.6): origen no es de aclimatación, destino no es de exhibición,
/// medio incompatible, dieta incompatible, criatura no adulta o salud baja.
/// </summary>
public class IncompatibleTransferException : DomainException
{
    public IncompatibleTransferException(string message) : base(message) { }
}
