namespace Dune.Domain;

/// <summary>
/// Tipos de recinto físico de una instalación (Sección 3.4 del PDF).
/// Cada código de instalación tiene un tipo de recinto fijado por la tabla:
///   ROCA_SELLADA     → ADR05, EDR02 (medio DESIERTO + RECOLECTOR)
///   ESCUDO_ESTATICO  → ADP03, EDP03 (medio DESIERTO + DEPREDADOR)
///   CUPULA_BLINDADA  → AAV02, EAV02 (medio AÉREO + DEPREDADOR)
///   POZO_REFORZADO   → ASU04, ESU03 (medio SUBTERRÁNEO + DEPREDADOR)
/// </summary>
public enum TipoRecinto
{
    ROCA_SELLADA,
    ESCUDO_ESTATICO,
    CUPULA_BLINDADA,
    POZO_REFORZADO
}
