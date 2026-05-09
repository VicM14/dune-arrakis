using Dune.Domain;
using Dune.Domain.Exceptions;
using Dune.SimulationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dune.SimulationService.Controllers;

/// <summary>
/// Controlador principal del SimulationService. Expone todas las operaciones
/// que el cliente de administración puede invocar sobre la partida activa.
///
/// Los paths se mantienen sin prefijo /api/ por compatibilidad con el código
/// cliente ya existente (AdminClient y futuro Unity).
/// </summary>
[ApiController]
public class SimulationController : ControllerBase
{
    private readonly SimulationState _state;
    private readonly IPersistenceClient _persistence;
    private readonly ILogger<SimulationController> _logger;

    private static readonly Random _rng = new();

    private const int COSTE_UNIDAD_SUMINISTRO = 5;
    private const int COSTE_DESCARTE_BENE_TLEILAX = 20000;

    public SimulationController(
        SimulationState state,
        IPersistenceClient persistence,
        ILogger<SimulationController> logger)
    {
        _state = state;
        _persistence = persistence;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────
    // CONSULTA DE ESTADO
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet("/estado-inicial")]
    public ActionResult<Partida> EstadoInicial() => Ok(_state.PartidaActual);

    // ─────────────────────────────────────────────────────────────────────
    // GESTIÓN DE PARTIDA
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost("/simulacion/iniciar-partida")]
    public async Task<IActionResult> IniciarPartida(
        [FromQuery] string nombreJugador,
        [FromQuery] string nombreEscenario,
        CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            Escenario escenario = nombreEscenario switch
            {
                "Arrakeen" => Escenario.Arrakeen(),
                "GiediPrime" => Escenario.GiediPrime(),
                "Caladan" => Escenario.Caladan(),
                _ => Escenario.Arrakeen()
            };

            var enclaveAclimatacion = new Enclave
            {
                Nombre = "Cuenca Experimental de Arrakis",
                Hectareas = 5000,
                Suministros = 20000,
                TipoEnclave = TipoActividad.ACLIMATACION,
                VisitantesMensualesBase = 0
            };

            var enclaveExhibicion = nombreEscenario switch
            {
                "Arrakeen" => new Enclave
                {
                    Nombre = "Arrakeen", Hectareas = 7700, Suministros = 10000,
                    TipoEnclave = TipoActividad.EXHIBICION, NivelAdquisitivo = NivelAdquisitivo.ALTO,
                    VisitantesMensualesBase = 1000, PoblacionVisitantes = 1000
                },
                "GiediPrime" => new Enclave
                {
                    Nombre = "Giedi Prime", Hectareas = 100, Suministros = 5000,
                    TipoEnclave = TipoActividad.EXHIBICION, NivelAdquisitivo = NivelAdquisitivo.BAJO,
                    VisitantesMensualesBase = 2000, PoblacionVisitantes = 2000
                },
                _ => new Enclave
                {
                    Nombre = "Caladan", Hectareas = 10000, Suministros = 25000,
                    TipoEnclave = TipoActividad.EXHIBICION, NivelAdquisitivo = NivelAdquisitivo.MEDIO,
                    VisitantesMensualesBase = 3000, PoblacionVisitantes = 3000
                }
            };

            _state.PartidaActual = new Partida
            {
                NombreJugador = nombreJugador,
                Solaris = escenario.SolarisIniciales,
                Escenario = escenario,
                Enclaves = new List<Enclave> { enclaveAclimatacion, enclaveExhibicion }
            };
            _state.PartidaActual.RegistroEventos.Add(
                $"Partida iniciada. Escenario: {escenario.Nombre}. Jugador: {nombreJugador}.");

            await _persistence.GuardarPartidaAsync(_state.PartidaActual, cancellationToken);
            return Ok(_state.PartidaActual);
        }
        finally { _state.Lock.Release(); }
    }

    [HttpPost("/simulacion/guardar-actual")]
    public async Task<IActionResult> GuardarActual([FromBody] Partida nuevaPartida, CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            _state.PartidaActual = nuevaPartida;
            await _persistence.GuardarPartidaAsync(_state.PartidaActual, cancellationToken);
            return Ok("Partida sincronizada.");
        }
        finally { _state.Lock.Release(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // RONDA MENSUAL
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost("/simulacion/ejecutar-ronda")]
    public async Task<IActionResult> EjecutarRonda(CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            var partida = _state.PartidaActual;
            partida.MesActual++;
            partida.RegistroEventos.Add($"--- INICIO MES {partida.MesActual} ---");

            double ingresosTotales = 0;

            foreach (var enclave in partida.Enclaves)
            {
                enclave.ActualizarVisitantes();

                int hectareasExhibicion = enclave.Instalaciones
                    .Where(i => i.Tipo == TipoActividad.EXHIBICION)
                    .Sum(i => i.Hectareas);

                foreach (var inst in enclave.Instalaciones)
                {
                    int visitantesInstalacion = 0;
                    if (inst.Tipo == TipoActividad.EXHIBICION && hectareasExhibicion > 0)
                    {
                        visitantesInstalacion = (int)((long)enclave.PoblacionVisitantes
                                                      * inst.Hectareas / hectareasExhibicion);
                    }

                    foreach (var criatura in inst.Criaturas.ToList())
                    {
                        if (criatura.Salud <= 0) continue;

                        double requerida = criatura.CalcularIngestaRequerida(inst.Tipo);
                        int requeridaInt = (int)Math.Ceiling(requerida);

                        if (inst.Suministros >= requeridaInt)
                        {
                            inst.Suministros -= requeridaInt;
                            criatura.Alimentar(requerida, inst.Tipo);
                        }
                        else if (inst.Suministros > 0)
                        {
                            double parcial = inst.Suministros;
                            inst.Suministros = 0;
                            criatura.Alimentar(parcial, inst.Tipo);
                            partida.RegistroEventos.Add(
                                $"ALERTA: alimentación parcial de {criatura.Nombre} en {inst.Nombre} ({parcial:F0}/{requerida:F0} unidades).");
                        }
                        else
                        {
                            criatura.Alimentar(0, inst.Tipo);
                            partida.RegistroEventos.Add(
                                $"ALERTA: sin suministros para {criatura.Nombre} en {inst.Nombre}.");
                        }

                        criatura.EdadActual++;

                        if (criatura.EnLetargo)
                        {
                            partida.Solaris -= COSTE_DESCARTE_BENE_TLEILAX;
                            partida.RegistroEventos.Add(
                                $"DESCARTE: {criatura.Nombre} transferida a Bene Tleilax. Coste: {COSTE_DESCARTE_BENE_TLEILAX} Solaris.");
                        }
                    }

                    inst.Criaturas.RemoveAll(c => c.EnLetargo);

                    if (inst.Tipo == TipoActividad.EXHIBICION)
                    {
                        double donacion = inst.CalcularDonacionesTotales(visitantesInstalacion, enclave.NivelAdquisitivo);
                        ingresosTotales += donacion;
                        if (donacion > 0)
                            partida.RegistroEventos.Add(
                                $"Donaciones en {inst.Nombre}: +{donacion:F2} Solaris ({visitantesInstalacion} visitantes)");
                    }

                    if (inst.Tipo == TipoActividad.ACLIMATACION &&
                        inst.Criaturas.Count < inst.CapacidadMaxima &&
                        _rng.NextDouble() < 0.20)
                    {
                        var nuevaCriatura = CrearCriaturaAleatoriaParaInstalacion(_rng, inst);
                        if (nuevaCriatura != null)
                        {
                            inst.Criaturas.Add(nuevaCriatura);
                            partida.RegistroEventos.Add(
                                $"Nueva criatura generada en {inst.Nombre}: {nuevaCriatura.Nombre}");
                        }
                    }
                }
            }

            partida.Solaris += ingresosTotales;
            partida.RegistroEventos.Add(
                $"Finanzas mes {partida.MesActual}: +{ingresosTotales:F2} ingresos por donaciones.");

            await _persistence.GuardarPartidaAsync(partida, cancellationToken);
            return Ok(partida);
        }
        finally { _state.Lock.Release(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // SUMINISTROS
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost("/simulacion/comprar-suministros")]
    public async Task<IActionResult> ComprarSuministros(
        [FromQuery] string enclaveId,
        [FromQuery] int cantidad,
        CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            if (cantidad <= 0)
                throw new InvalidEntityStateException("La cantidad debe ser mayor que 0.");

            var partida = _state.PartidaActual;
            var enclave = partida.Enclaves.FirstOrDefault(e => e.Id == enclaveId)
                ?? throw new EntityNotFoundException("Enclave", enclaveId);

            int coste = cantidad * COSTE_UNIDAD_SUMINISTRO;
            if (partida.Solaris < coste)
                throw new InsufficientFundsException(coste, partida.Solaris);

            if (enclave.Suministros + cantidad > enclave.CapacidadAlmacen)
                throw new InvalidEntityStateException(
                    $"Capacidad del almacén de {enclave.Nombre} superada. Espacio libre: {enclave.EspacioLibreEnAlmacen} unidades.");

            partida.Solaris -= coste;
            enclave.Suministros += cantidad;
            partida.RegistroEventos.Add(
                $"Compra de {cantidad} unidades de suministro para {enclave.Nombre}. Coste: {coste} Solaris.");

            await _persistence.GuardarPartidaAsync(partida, cancellationToken);
            return Ok(new { mensaje = "Suministros comprados.", coste, almacen = enclave.Suministros, solarisRestantes = partida.Solaris });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
        finally { _state.Lock.Release(); }
    }

    [HttpPost("/simulacion/mover-suministros")]
    public async Task<IActionResult> MoverSuministros(
        [FromQuery] string enclaveId,
        [FromQuery] string instalacionId,
        [FromQuery] int cantidad,
        CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            if (cantidad <= 0)
                throw new InvalidEntityStateException("La cantidad debe ser mayor que 0.");

            var partida = _state.PartidaActual;
            var enclave = partida.Enclaves.FirstOrDefault(e => e.Id == enclaveId)
                ?? throw new EntityNotFoundException("Enclave", enclaveId);

            var inst = enclave.Instalaciones.FirstOrDefault(i => i.Id == instalacionId)
                ?? throw new EntityNotFoundException("Instalación", instalacionId);

            if (enclave.Suministros < cantidad)
                throw new InvalidEntityStateException(
                    $"Suministros insuficientes en el almacén de {enclave.Nombre} (disponibles: {enclave.Suministros}).");

            if (inst.Suministros + cantidad > inst.CosteConstruccion)
                throw new InvalidEntityStateException(
                    $"La instalación {inst.Nombre} no puede almacenar más de {inst.CosteConstruccion} unidades (actual: {inst.Suministros}).");

            enclave.Suministros -= cantidad;
            inst.Suministros += cantidad;
            partida.RegistroEventos.Add(
                $"Movidas {cantidad} unidades de suministro de {enclave.Nombre} a {inst.Nombre}.");

            await _persistence.GuardarPartidaAsync(partida, cancellationToken);
            return Ok(new { mensaje = "Suministros movidos.", almacen = enclave.Suministros, stockInstalacion = inst.Suministros });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
        finally { _state.Lock.Release(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // CRIATURAS
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost("/simulacion/trasladar-criatura")]
    public async Task<IActionResult> TrasladarCriatura(
        [FromQuery] string criaturaId,
        [FromQuery] string instalacionOrigenId,
        [FromQuery] string instalacionDestinoId,
        CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            var partida = _state.PartidaActual;

            Instalacion? origen = null;
            Instalacion? destino = null;
            foreach (var enclave in partida.Enclaves)
            {
                foreach (var i in enclave.Instalaciones)
                {
                    if (i.Id == instalacionOrigenId) origen = i;
                    if (i.Id == instalacionDestinoId) destino = i;
                }
            }
            if (origen == null) throw new EntityNotFoundException("Instalación origen", instalacionOrigenId);
            if (destino == null) throw new EntityNotFoundException("Instalación destino", instalacionDestinoId);

            var criatura = origen.Criaturas.FirstOrDefault(c => c.Id == criaturaId)
                ?? throw new EntityNotFoundException("Criatura", criaturaId);

            // Reglas de la Sección 3.6 del PDF.
            if (origen.Tipo != TipoActividad.ACLIMATACION)
                throw new IncompatibleTransferException("La instalación de origen debe ser de aclimatación.");
            if (destino.Tipo != TipoActividad.EXHIBICION)
                throw new IncompatibleTransferException("La instalación de destino debe ser de exhibición.");
            if (criatura.Habitat != destino.Medio)
                throw new IncompatibleTransferException(
                    $"Medio incompatible: criatura {criatura.Habitat}, destino {destino.Medio}.");
            if (criatura.Dieta != destino.Alimentacion)
                throw new IncompatibleTransferException(
                    $"Alimentación incompatible: criatura {criatura.Dieta}, destino {destino.Alimentacion}.");
            if (criatura.EdadActual < criatura.EdadAdulta)
                throw new IncompatibleTransferException("La criatura no es adulta todavía.");
            if (criatura.Salud < 75)
                throw new IncompatibleTransferException(
                    $"Salud insuficiente para traslado: {criatura.Salud}/100 (mínimo 75).");
            if (destino.Criaturas.Count >= destino.CapacidadMaxima)
                throw new InvalidEntityStateException("La instalación de destino está completa.");

            double sigma = criatura.Habitat switch
            {
                Medio.DESIERTO => 5,
                Medio.AEREO => 15,
                Medio.SUBTERRANEO => 25,
                _ => 5
            };
            double costeTraslado = 100 * Math.Pow(3, criatura.EdadActual - criatura.EdadAdulta) * sigma;

            if (partida.Solaris < costeTraslado)
                throw new InsufficientFundsException(costeTraslado, partida.Solaris);

            partida.Solaris -= costeTraslado;
            origen.Criaturas.Remove(criatura);
            destino.Criaturas.Add(criatura);
            partida.RegistroEventos.Add(
                $"Traslado: {criatura.Nombre} de {origen.Nombre} a {destino.Nombre}. Coste: {costeTraslado:F2} Solaris.");

            await _persistence.GuardarPartidaAsync(partida, cancellationToken);
            return Ok(new { mensaje = "Traslado completado.", costeTraslado, solarisRestantes = partida.Solaris });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
        finally { _state.Lock.Release(); }
    }

    [HttpPost("/simulacion/descartar-criatura")]
    public async Task<IActionResult> DescartarCriatura(
        [FromQuery] string criaturaId,
        CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            var partida = _state.PartidaActual;
            Instalacion? contenedora = null;
            Criatura? criatura = null;

            foreach (var enclave in partida.Enclaves)
            {
                foreach (var inst in enclave.Instalaciones)
                {
                    var c = inst.Criaturas.FirstOrDefault(x => x.Id == criaturaId);
                    if (c != null) { contenedora = inst; criatura = c; break; }
                }
                if (criatura != null) break;
            }

            if (criatura == null || contenedora == null)
                throw new EntityNotFoundException("Criatura", criaturaId);

            if (partida.Solaris < COSTE_DESCARTE_BENE_TLEILAX)
                throw new InsufficientFundsException(COSTE_DESCARTE_BENE_TLEILAX, partida.Solaris);

            partida.Solaris -= COSTE_DESCARTE_BENE_TLEILAX;
            contenedora.Criaturas.Remove(criatura);
            partida.RegistroEventos.Add(
                $"DESCARTE VOLUNTARIO: {criatura.Nombre} transferida a Bene Tleilax. Coste: {COSTE_DESCARTE_BENE_TLEILAX} Solaris.");

            await _persistence.GuardarPartidaAsync(partida, cancellationToken);
            return Ok(new { mensaje = "Criatura descartada.", solarisRestantes = partida.Solaris });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
        finally { _state.Lock.Release(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // INSTALACIONES
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost("/simulacion/construir-instalacion")]
    public async Task<IActionResult> ConstruirInstalacion(
        [FromQuery] string codigoInstalacion,
        [FromQuery] string enclaveId,
        CancellationToken cancellationToken)
    {
        await _state.Lock.WaitAsync(cancellationToken);
        try
        {
            var partida = _state.PartidaActual;
            var enclave = partida.Enclaves.FirstOrDefault(e => e.Id == enclaveId)
                ?? throw new EntityNotFoundException("Enclave", enclaveId);

            Instalacion? nueva = codigoInstalacion switch
            {
                "ADR05" => new Instalacion { Codigo = "ADR05", Nombre = "Roca Sellada (Aclimatación)",      Tipo = TipoActividad.ACLIMATACION, Medio = Medio.DESIERTO,     Alimentacion = Alimentacion.RECOLECTOR, TipoRecinto = TipoRecinto.ROCA_SELLADA,    CosteConstruccion = 1000,  Hectareas = 10,  CapacidadMaxima = 5, SuministrosIniciales = 200, Suministros = 200 },
                "ADP03" => new Instalacion { Codigo = "ADP03", Nombre = "Escudo Estático (Aclimatación)",   Tipo = TipoActividad.ACLIMATACION, Medio = Medio.DESIERTO,     Alimentacion = Alimentacion.DEPREDADOR, TipoRecinto = TipoRecinto.ESCUDO_ESTATICO, CosteConstruccion = 2500,  Hectareas = 50,  CapacidadMaxima = 3, SuministrosIniciales = 300, Suministros = 300 },
                "AAV02" => new Instalacion { Codigo = "AAV02", Nombre = "Cúpula Blindada (Aclimatación)",   Tipo = TipoActividad.ACLIMATACION, Medio = Medio.AEREO,        Alimentacion = Alimentacion.DEPREDADOR, TipoRecinto = TipoRecinto.CUPULA_BLINDADA, CosteConstruccion = 5000,  Hectareas = 100, CapacidadMaxima = 2, SuministrosIniciales = 500, Suministros = 500 },
                "ASU04" => new Instalacion { Codigo = "ASU04", Nombre = "Pozo Reforzado (Aclimatación)",    Tipo = TipoActividad.ACLIMATACION, Medio = Medio.SUBTERRANEO,  Alimentacion = Alimentacion.DEPREDADOR, TipoRecinto = TipoRecinto.POZO_REFORZADO,  CosteConstruccion = 3500,  Hectareas = 25,  CapacidadMaxima = 4, SuministrosIniciales = 100, Suministros = 100 },
                "EDR02" => new Instalacion { Codigo = "EDR02", Nombre = "Roca Sellada (Exhibición)",        Tipo = TipoActividad.EXHIBICION,   Medio = Medio.DESIERTO,     Alimentacion = Alimentacion.RECOLECTOR, TipoRecinto = TipoRecinto.ROCA_SELLADA,    CosteConstruccion = 21000, Hectareas = 200, CapacidadMaxima = 2, SuministrosIniciales = 0,   Suministros = 0   },
                "EDP03" => new Instalacion { Codigo = "EDP03", Nombre = "Escudo Estático (Exhibición)",     Tipo = TipoActividad.EXHIBICION,   Medio = Medio.DESIERTO,     Alimentacion = Alimentacion.DEPREDADOR, TipoRecinto = TipoRecinto.ESCUDO_ESTATICO, CosteConstruccion = 12500, Hectareas = 300, CapacidadMaxima = 3, SuministrosIniciales = 0,   Suministros = 0   },
                "EAV02" => new Instalacion { Codigo = "EAV02", Nombre = "Cúpula Blindada (Exhibición)",     Tipo = TipoActividad.EXHIBICION,   Medio = Medio.AEREO,        Alimentacion = Alimentacion.DEPREDADOR, TipoRecinto = TipoRecinto.CUPULA_BLINDADA, CosteConstruccion = 15000, Hectareas = 200, CapacidadMaxima = 2, SuministrosIniciales = 0,   Suministros = 0   },
                "ESU03" => new Instalacion { Codigo = "ESU03", Nombre = "Pozo Reforzado (Exhibición)",      Tipo = TipoActividad.EXHIBICION,   Medio = Medio.SUBTERRANEO,  Alimentacion = Alimentacion.DEPREDADOR, TipoRecinto = TipoRecinto.POZO_REFORZADO,  CosteConstruccion = 25000, Hectareas = 400, CapacidadMaxima = 3, SuministrosIniciales = 0,   Suministros = 0   },
                _ => null
            };

            if (nueva == null)
                throw new InvalidEntityStateException($"Código de instalación desconocido: {codigoInstalacion}");

            if (partida.Solaris < nueva.CosteConstruccion)
                throw new InsufficientFundsException(nueva.CosteConstruccion, partida.Solaris);

            int hectareasUsadas = enclave.Instalaciones.Sum(i => i.Hectareas);
            if (hectareasUsadas + nueva.Hectareas > enclave.Hectareas)
                throw new InvalidEntityStateException(
                    $"Espacio insuficiente en {enclave.Nombre}. Libres: {enclave.Hectareas - hectareasUsadas} ha, necesarias: {nueva.Hectareas} ha.");

            partida.Solaris -= nueva.CosteConstruccion;
            enclave.Instalaciones.Add(nueva);
            partida.RegistroEventos.Add(
                $"Construida {nueva.Nombre} en {enclave.Nombre}. Coste: {nueva.CosteConstruccion} Solaris. Suministros iniciales: {nueva.SuministrosIniciales}.");

            await _persistence.GuardarPartidaAsync(partida, cancellationToken);
            return Ok(partida);
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
        finally { _state.Lock.Release(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPER (estático, no es endpoint)
    // ─────────────────────────────────────────────────────────────────────

    private static Criatura? CrearCriaturaAleatoriaParaInstalacion(Random rng, Instalacion inst)
    {
        var candidatas = new List<(Func<Criatura> factory, Medio habitat, Alimentacion dieta)>
        {
            (() => new GusanoDeArena       { Nombre = "Gusano de Arena Joven" },     Medio.SUBTERRANEO, Alimentacion.DEPREDADOR),
            (() => new TigraLaza           { Nombre = "Tigre Laza Joven" },          Medio.DESIERTO,    Alimentacion.DEPREDADOR),
            (() => new MuadDib             { Nombre = "Muad'Dib Joven" },            Medio.DESIERTO,    Alimentacion.RECOLECTOR),
            (() => new HalconDelDesierto   { Nombre = "Halcón del Desierto Joven" }, Medio.AEREO,       Alimentacion.DEPREDADOR),
            (() => new TruchaDeArena       { Nombre = "Trucha de Arena Joven" },     Medio.SUBTERRANEO, Alimentacion.RECOLECTOR)
        };

        var compatibles = candidatas
            .Where(t => t.habitat == inst.Medio && t.dieta == inst.Alimentacion)
            .ToList();

        if (compatibles.Count == 0) return null;

        var elegida = compatibles[rng.Next(compatibles.Count)];
        return elegida.factory();
    }
}
