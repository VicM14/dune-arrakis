using Dune.Domain;
using System.Net.Http.Json;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors("AllowUnity");

// ─────────────────────────────────────────────────────────────────────────────
// ESTADO Y CONCURRENCIA
// ─────────────────────────────────────────────────────────────────────────────
SemaphoreSlim _simLock = new SemaphoreSlim(1, 1);
var partidaActual = new Partida();

// Coste fijo por unidad de suministro (Sección 3.3 del PDF).
const int COSTE_UNIDAD_SUMINISTRO = 5;

// Coste fijo de descarte vía Bene Tleilax (Sección 3.6 del PDF).
const int COSTE_DESCARTE_BENE_TLEILAX = 20000;

// ─────────────────────────────────────────────────────────────────────────────
// ─────────────────────────────────────────────────────────────────────────────
// HELPERS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Crea una criatura aleatoria entre las que son COMPATIBLES con el medio y la
/// alimentación de la instalación destino. Si ninguna especie de las cinco del
/// PDF cumple con la combinación (no debería ocurrir con las 8 instalaciones
/// y 5 criaturas del PDF), devuelve null.
/// </summary>
static Criatura? CrearCriaturaAleatoriaParaInstalacion(Random rng, Instalacion inst)
{
    // Lista de fábricas con cada especie del PDF y sus rasgos.
    var candidatas = new List<(Func<Criatura> factory, Medio habitat, Alimentacion dieta)>
    {
        (() => new GusanoDeArena       { Nombre = "Gusano de Arena Joven" },     Medio.SUBTERRANEO, Alimentacion.DEPREDADOR),
        (() => new TigraLaza           { Nombre = "Tigre Laza Joven" },          Medio.DESIERTO,    Alimentacion.DEPREDADOR),
        (() => new MuadDib             { Nombre = "Muad'Dib Joven" },            Medio.DESIERTO,    Alimentacion.RECOLECTOR),
        (() => new HalconDelDesierto   { Nombre = "Halcón del Desierto Joven" }, Medio.AEREO,       Alimentacion.DEPREDADOR),
        (() => new TruchaDeArena       { Nombre = "Trucha de Arena Joven" },     Medio.SUBTERRANEO, Alimentacion.RECOLECTOR)
    };

    // Filtramos por compatibilidad (Sección 3.4 del PDF: cada instalación está
    // preparada para un medio concreto y un patrón de alimentación específico).
    var compatibles = candidatas
        .Where(t => t.habitat == inst.Medio && t.dieta == inst.Alimentacion)
        .ToList();

    if (compatibles.Count == 0) return null;

    var elegida = compatibles[rng.Next(compatibles.Count)];
    return elegida.factory();
}

// ─────────────────────────────────────────────────────────────────────────────
// ENDPOINTS
// ─────────────────────────────────────────────────────────────────────────────

app.MapGet("/estado-inicial", () => Results.Ok(partidaActual));

app.MapPost("/simulacion/guardar-actual", async (Partida nuevaPartida, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        partidaActual = nuevaPartida;
        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);
        return Results.Ok("Partida sincronizada.");
    }
    finally { _simLock.Release(); }
});

app.MapPost("/simulacion/ejecutar-ronda", async (IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        partidaActual.MesActual++;
        partidaActual.RegistroEventos.Add($"--- INICIO MES {partidaActual.MesActual} ---");

        double ingresosTotales = 0;
        Random rng = new Random();

        foreach (var enclave in partidaActual.Enclaves)
        {
            // 1. Actualizar la población de visitantes del enclave (Sección 3.3).
            enclave.ActualizarVisitantes();

            foreach (var inst in enclave.Instalaciones)
            {
                // 2. Generar visitantes virtuales según la población del enclave.
                //    NOTA: en el commit 6 esto se simplificará para que TODOS los
                //    visitantes hereden el NivelAdquisitivo del enclave.
                inst.VisitantesActuales.Clear();
                int numVisitantes = Math.Min(enclave.PoblacionVisitantes / 10, 50);
                for (int i = 0; i < numVisitantes; i++)
                {
                    inst.VisitantesActuales.Add(new Visitante(enclave.NivelAdquisitivo));
                }

                // 3. Alimentación de criaturas — los suministros salen del stock
                //    INTERNO de la instalación (Sección 3.4). Si no hay, la criatura
                //    no come y aplica la penalización por subalimentación.
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
                        // Alimentación parcial: consumimos lo que hay.
                        double parcial = inst.Suministros;
                        inst.Suministros = 0;
                        criatura.Alimentar(parcial, inst.Tipo);
                        partidaActual.RegistroEventos.Add(
                            $"ALERTA: alimentación parcial de {criatura.Nombre} en {inst.Nombre} ({parcial:F0}/{requerida:F0} unidades).");
                    }
                    else
                    {
                        // No hay suministros: la criatura come 0 y se penaliza.
                        criatura.Alimentar(0, inst.Tipo);
                        partidaActual.RegistroEventos.Add(
                            $"ALERTA: sin suministros para {criatura.Nombre} en {inst.Nombre}.");
                    }

                    criatura.EdadActual++;

                    // Descarte de criaturas en letargo (Sección 3.6 — coste fijo 20.000).
                    if (criatura.EnLetargo)
                    {
                        partidaActual.Solaris -= COSTE_DESCARTE_BENE_TLEILAX;
                        partidaActual.RegistroEventos.Add(
                            $"DESCARTE: {criatura.Nombre} transferida a Bene Tleilax. Coste: {COSTE_DESCARTE_BENE_TLEILAX} Solaris.");
                    }
                }

                // 4. Retirar criaturas en letargo ANTES de calcular donaciones y reproducción.
                inst.Criaturas.RemoveAll(c => c.EnLetargo);

                // 5. Donaciones — solo en exhibición (Sección 3.4).
                if (inst.Tipo == TipoActividad.EXHIBICION)
                {
                    double donacion = inst.CalcularDonacionesTotales(enclave.NivelAdquisitivo);
                    ingresosTotales += donacion;
                    if (donacion > 0)
                        partidaActual.RegistroEventos.Add(
                            $"Donaciones en {inst.Nombre}: +{donacion:F2} Solaris");
                }

                // 6. Reproducción/clonación — solo en aclimatación, 20% de probabilidad
                //    si hay capacidad libre, y SOLO entre las especies compatibles
                //    con el medio y la alimentación de la instalación (Sección 3.4).
                if (inst.Tipo == TipoActividad.ACLIMATACION &&
                    inst.Criaturas.Count < inst.CapacidadMaxima &&
                    rng.NextDouble() < 0.20)
                {
                    var nuevaCriatura = CrearCriaturaAleatoriaParaInstalacion(rng, inst);
                    if (nuevaCriatura != null)
                    {
                        inst.Criaturas.Add(nuevaCriatura);
                        partidaActual.RegistroEventos.Add(
                            $"Nueva criatura generada en {inst.Nombre}: {nuevaCriatura.Nombre}");
                    }
                }
            }
        }

        // 7. Balance final del mes.
        partidaActual.Solaris += ingresosTotales;
        partidaActual.RegistroEventos.Add(
            $"Finanzas mes {partidaActual.MesActual}: +{ingresosTotales:F2} ingresos por donaciones.");

        // 8. Persistencia automática.
        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});

// Compra de suministros al almacén general de un ENCLAVE concreto
// (Sección 3.3: coste fijo 5 solaris/unidad, capacidad máxima del almacén = 3 × hectáreas).
app.MapPost("/simulacion/comprar-suministros", async (string enclaveId, int cantidad, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        if (cantidad <= 0)
            return Results.BadRequest("La cantidad debe ser mayor que 0.");

        var enclave = partidaActual.Enclaves.FirstOrDefault(e => e.Id == enclaveId);
        if (enclave == null)
            return Results.NotFound("Enclave no encontrado.");

        int coste = cantidad * COSTE_UNIDAD_SUMINISTRO;
        if (partidaActual.Solaris < coste)
            return Results.BadRequest($"Solaris insuficientes. Coste: {coste}, disponibles: {partidaActual.Solaris}.");

        if (enclave.Suministros + cantidad > enclave.CapacidadAlmacen)
            return Results.BadRequest(
                $"Capacidad del almacén de {enclave.Nombre} superada. Espacio libre: {enclave.EspacioLibreEnAlmacen} unidades.");

        partidaActual.Solaris -= coste;
        enclave.Suministros += cantidad;
        partidaActual.RegistroEventos.Add(
            $"Compra de {cantidad} unidades de suministro para {enclave.Nombre}. Coste: {coste} Solaris.");

        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(new { mensaje = "Suministros comprados.", coste, almacen = enclave.Suministros, solarisRestantes = partidaActual.Solaris });
    }
    finally { _simLock.Release(); }
});

// Movimiento gratuito de suministros del almacén general del enclave al stock
// interno de una instalación (Sección 3.3: gratuito; tope = coste de construcción).
app.MapPost("/simulacion/mover-suministros", async (string enclaveId, string instalacionId, int cantidad, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        if (cantidad <= 0)
            return Results.BadRequest("La cantidad debe ser mayor que 0.");

        var enclave = partidaActual.Enclaves.FirstOrDefault(e => e.Id == enclaveId);
        if (enclave == null)
            return Results.NotFound("Enclave no encontrado.");

        var inst = enclave.Instalaciones.FirstOrDefault(i => i.Id == instalacionId);
        if (inst == null)
            return Results.NotFound("Instalación no encontrada en este enclave.");

        if (enclave.Suministros < cantidad)
            return Results.BadRequest(
                $"Suministros insuficientes en el almacén de {enclave.Nombre} (disponibles: {enclave.Suministros}).");

        // Tope: ninguna instalación puede superar en suministros el valor de su coste de construcción.
        if (inst.Suministros + cantidad > inst.CosteConstruccion)
            return Results.BadRequest(
                $"La instalación {inst.Nombre} no puede almacenar más de {inst.CosteConstruccion} unidades (actual: {inst.Suministros}).");

        enclave.Suministros -= cantidad;
        inst.Suministros += cantidad;
        partidaActual.RegistroEventos.Add(
            $"Movidas {cantidad} unidades de suministro de {enclave.Nombre} a {inst.Nombre}.");

        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(new { mensaje = "Suministros movidos.", almacen = enclave.Suministros, stockInstalacion = inst.Suministros });
    }
    finally { _simLock.Release(); }
});

app.MapPost("/simulacion/trasladar-criatura", async (string criaturaId, string instalacionOrigenId, string instalacionDestinoId, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        Instalacion? origen = null;
        Instalacion? destino = null;

        foreach (var enclave in partidaActual.Enclaves)
        {
            foreach (var inst in enclave.Instalaciones)
            {
                if (inst.Id == instalacionOrigenId) origen = inst;
                if (inst.Id == instalacionDestinoId) destino = inst;
            }
        }

        if (origen == null) return Results.NotFound("Instalación de origen no encontrada.");
        if (destino == null) return Results.NotFound("Instalación de destino no encontrada.");

        var criatura = origen.Criaturas.FirstOrDefault(c => c.Id == criaturaId);
        if (criatura == null) return Results.NotFound("Criatura no encontrada en la instalación de origen.");

        // Sección 3.6 del PDF: traslados sólo de ACLIMATACION → EXHIBICION.
        if (origen.Tipo != TipoActividad.ACLIMATACION)
            return Results.BadRequest("La instalación de origen debe ser de aclimatación.");

        if (destino.Tipo != TipoActividad.EXHIBICION)
            return Results.BadRequest("La instalación de destino debe ser de exhibición.");

        // Compatibilidad criatura ↔ instalación destino (Sección 3.4 del PDF).
        if (criatura.Habitat != destino.Medio)
            return Results.BadRequest(
                $"Medio incompatible: la criatura es de medio {criatura.Habitat} y la instalación destino es de medio {destino.Medio}.");

        if (criatura.Dieta != destino.Alimentacion)
            return Results.BadRequest(
                $"Alimentación incompatible: la criatura es {criatura.Dieta} y la instalación destino es {destino.Alimentacion}.");

        if (criatura.EdadActual < criatura.EdadAdulta)
            return Results.BadRequest("La criatura no es adulta todavía.");

        if (criatura.Salud < 75)
            return Results.BadRequest($"Salud insuficiente para traslado: {criatura.Salud}/100 (mínimo 75).");

        if (destino.Criaturas.Count >= destino.CapacidadMaxima)
            return Results.BadRequest("La instalación de destino está completa.");

        double sigma = criatura.Habitat switch
        {
            Medio.DESIERTO => 5,
            Medio.AEREO => 15,
            Medio.SUBTERRANEO => 25,
            _ => 5
        };
        double costeTraslado = 100 * Math.Pow(3, criatura.EdadActual - criatura.EdadAdulta) * sigma;

        if (partidaActual.Solaris < costeTraslado)
            return Results.BadRequest(
                $"Solaris insuficientes. Coste: {costeTraslado:F2}, disponibles: {partidaActual.Solaris:F2}.");

        partidaActual.Solaris -= costeTraslado;
        origen.Criaturas.Remove(criatura);
        destino.Criaturas.Add(criatura);
        partidaActual.RegistroEventos.Add(
            $"Traslado: {criatura.Nombre} de {origen.Nombre} a {destino.Nombre}. Coste: {costeTraslado:F2} Solaris.");

        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(new { mensaje = "Traslado completado.", costeTraslado, solarisRestantes = partidaActual.Solaris });
    }
    finally { _simLock.Release(); }
});

app.MapPost("/simulacion/iniciar-partida", async (string nombreJugador, string nombreEscenario, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        Escenario escenario = nombreEscenario switch
        {
            "Arrakeen" => Escenario.Arrakeen(),
            "GiediPrime" => Escenario.GiediPrime(),
            "Caladan" => Escenario.Caladan(),
            _ => Escenario.Arrakeen()
        };

        // Cuenca Experimental de Arrakis — enclave de aclimatación común (Sección 3.2 / 3.3).
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
                Nombre = "Arrakeen",
                Hectareas = 7700,
                Suministros = 10000,
                TipoEnclave = TipoActividad.EXHIBICION,
                NivelAdquisitivo = NivelAdquisitivo.ALTO,
                VisitantesMensualesBase = 1000,
                PoblacionVisitantes = 1000
            },
            "GiediPrime" => new Enclave
            {
                Nombre = "Giedi Prime",
                Hectareas = 100,
                Suministros = 5000,
                TipoEnclave = TipoActividad.EXHIBICION,
                NivelAdquisitivo = NivelAdquisitivo.BAJO,
                VisitantesMensualesBase = 2000,
                PoblacionVisitantes = 2000
            },
            _ => new Enclave
            {
                Nombre = "Caladan",
                Hectareas = 10000,
                Suministros = 25000,
                TipoEnclave = TipoActividad.EXHIBICION,
                NivelAdquisitivo = NivelAdquisitivo.MEDIO,
                VisitantesMensualesBase = 3000,
                PoblacionVisitantes = 3000
            }
        };

        partidaActual = new Partida
        {
            NombreJugador = nombreJugador,
            Solaris = escenario.SolarisIniciales,
            Escenario = escenario,
            Enclaves = new List<Enclave> { enclaveAclimatacion, enclaveExhibicion }
        };

        partidaActual.RegistroEventos.Add(
            $"Partida iniciada. Escenario: {escenario.Nombre}. Jugador: {nombreJugador}.");

        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});

app.MapPost("/simulacion/construir-instalacion", async (string codigoInstalacion, string enclaveId, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        var enclave = partidaActual.Enclaves.FirstOrDefault(e => e.Id == enclaveId);
        if (enclave == null)
            return Results.NotFound("Enclave no encontrado.");

        // Tabla completa de instalaciones (Sección 3.4 del PDF).
        // Cada instalación nace con SuministrosIniciales según la tabla.
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
            return Results.BadRequest($"Código de instalación desconocido: {codigoInstalacion}");

        if (partidaActual.Solaris < nueva.CosteConstruccion)
            return Results.BadRequest(
                $"Solaris insuficientes. Coste: {nueva.CosteConstruccion}, disponibles: {partidaActual.Solaris}");

        int hectareasUsadas = enclave.Instalaciones.Sum(i => i.Hectareas);
        if (hectareasUsadas + nueva.Hectareas > enclave.Hectareas)
            return Results.BadRequest(
                $"Espacio insuficiente en {enclave.Nombre}. Libres: {enclave.Hectareas - hectareasUsadas} ha, necesarias: {nueva.Hectareas} ha.");

        partidaActual.Solaris -= nueva.CosteConstruccion;
        enclave.Instalaciones.Add(nueva);
        partidaActual.RegistroEventos.Add(
            $"Construida {nueva.Nombre} en {enclave.Nombre}. Coste: {nueva.CosteConstruccion} Solaris. Suministros iniciales: {nueva.SuministrosIniciales}.");

        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});

app.Run();
