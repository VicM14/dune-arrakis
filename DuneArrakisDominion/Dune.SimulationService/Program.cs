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

// ----- ESTADO Y CONCURRENCIA -----
SemaphoreSlim _simLock = new SemaphoreSlim(1, 1);
var partidaActual = new Partida();

// ----- HELPER -----
static Criatura CrearCriaturaAleatoria(Random rng)
{
    int tipo = rng.Next(0, 5);
    return tipo switch
    {
        0 => new GusanoDeArena { Nombre = "Gusano de Arena Joven" },
        1 => new TigraLaza { Nombre = "Tigre Laza Joven" },
        2 => new MuadDib { Nombre = "Muad'Dib Joven" },
        3 => new HalconDelDesierto { Nombre = "Halcón del Desierto Joven" },
        _ => new TruchaDeArena { Nombre = "Trucha de Arena Joven" }
    };
}

// ----- ENDPOINTS -----

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
        double gastosTotales = 0;
        Random rng = new Random();

        foreach (var enclave in partidaActual.Enclaves)
        {
            // 1. Actualizar población de visitantes (Sección 3.3)
            enclave.ActualizarVisitantes();

            foreach (var inst in enclave.Instalaciones)
            {
                // 2. Generar visitantes reales para este mes
                inst.VisitantesActuales.Clear();
                int numVisitantes = Math.Min(enclave.PoblacionVisitantes / 10, 50);
                for (int i = 0; i < numVisitantes; i++)
                {
                    var nivel = (NivelAdquisitivo)rng.Next(0, 3);
                    inst.VisitantesActuales.Add(new Visitante(nivel));
                }

                // 3. Costes de mantenimiento (Sección 3.4)
                gastosTotales += inst.CalcularCosteMantenimiento();

                // 4. Alimentación de criaturas — .ToList() para evitar excepción si se modifica la lista
                foreach (var criatura in inst.Criaturas.ToList())
                {
                    if (criatura.Salud > 0)
                    {
                        double requerida = criatura.CalcularIngestaRequerida(inst.Tipo);
                        double costeAgua = requerida * 0.2;
                        double costeEspecia = requerida * 0.1;

                        if (partidaActual.StockAgua >= costeAgua && partidaActual.StockEspecia >= costeEspecia)
                        {
                            partidaActual.StockAgua -= costeAgua;
                            partidaActual.StockEspecia -= costeEspecia;
                            criatura.Alimentar(requerida, inst.Tipo);
                        }
                        else
                        {
                            // Penalización por falta de recursos (Sección 3.5)
                            criatura.Alimentar(0, inst.Tipo);
                            partidaActual.RegistroEventos.Add(
                                $"ALERTA: Falta de recursos para {criatura.Nombre}");
                        }

                        criatura.EdadActual++;

                        // Descarte de criaturas en letargo (Sección 3.6 — coste fijo 20.000 solaris)
                        if (criatura.EnLetargo)
                        {
                            double costeDescarte = 20000;
                            partidaActual.Solaris -= costeDescarte;
                            partidaActual.RegistroEventos.Add(
                                $"DESCARTE: {criatura.Nombre} transferida a Bene Tleilax. Coste: {costeDescarte} Solaris.");
                        }
                    }
                }

                // 5. Retirar criaturas en letargo ANTES de calcular donaciones y reproducción
                inst.Criaturas.RemoveAll(c => c.EnLetargo);

                // 6. Donaciones — UNA sola vez por instalación, fuera del foreach de criaturas (Sección 3.4)
                if (inst.Tipo == TipoActividad.EXHIBICION)
                {
                    double donacion = inst.CalcularDonacionesTotales();
                    ingresosTotales += donacion;
                    if (donacion > 0)
                        partidaActual.RegistroEventos.Add(
                            $"Donaciones en {inst.Nombre}: +{donacion:F2} Solaris");
                }

                // 7. Reproducción/clonación — UNA sola vez por instalación, fuera del foreach de criaturas (Sección 3.4 — 20%)
                if (inst.Tipo == TipoActividad.ACLIMATACION &&
                    inst.Criaturas.Count < inst.CapacidadMaxima &&
                    rng.NextDouble() < 0.20)
                {
                    var nuevaCriatura = CrearCriaturaAleatoria(rng);
                    inst.Criaturas.Add(nuevaCriatura);
                    partidaActual.RegistroEventos.Add(
                        $"Nueva criatura generada en {inst.Nombre}: {nuevaCriatura.Nombre}");
                }
            }
        }

        // 8. Balance económico final
        partidaActual.Solaris += (ingresosTotales - gastosTotales);
        partidaActual.RegistroEventos.Add(
            $"Finanzas mes {partidaActual.MesActual}: +{ingresosTotales:F2} ingresos | -{gastosTotales:F2} gastos");

        // 9. Persistencia automática
        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});

app.MapPost("/simulacion/comprar-recursos", async (double agua, double especia, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        double coste = (agua * 2) + (especia * 10);
        if (partidaActual.Solaris < coste)
            return Results.BadRequest("Solaris insuficientes.");

        // Validar límite de almacén por enclave (Sección 3.3: máximo = 3 × hectáreas)
        foreach (var enclave in partidaActual.Enclaves)
        {
            int capacidadMaxima = enclave.Hectareas * 3;
            if ((partidaActual.StockAgua + agua + partidaActual.StockEspecia + especia) > capacidadMaxima)
                return Results.BadRequest(
                    $"Capacidad máxima del almacén superada en {enclave.Nombre}. Máximo: {capacidadMaxima} unidades.");
        }

        partidaActual.Solaris -= coste;
        partidaActual.StockAgua += agua;
        partidaActual.StockEspecia += especia;

        return Results.Ok(partidaActual);
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

        // Validaciones del enunciado (Sección 3.6)
        if (criatura.EdadActual < criatura.EdadAdulta)
            return Results.BadRequest("La criatura no es adulta todavía.");

        if (criatura.Salud < 75)
            return Results.BadRequest($"Salud insuficiente para traslado: {criatura.Salud}/100 (mínimo 75).");

        if (destino.Criaturas.Count >= destino.CapacidadMaxima)
            return Results.BadRequest("La instalación de destino está completa.");

        // Calcular coste de traslado (Sección 3.6)
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

        // Ejecutar traslado
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
        // Seleccionar escenario
        Escenario escenario = nombreEscenario switch
        {
            "Arrakeen" => Escenario.Arrakeen(),
            "GiediPrime" => Escenario.GiediPrime(),
            "Caladan" => Escenario.Caladan(),
            _ => Escenario.Arrakeen()
        };

        // Enclave de aclimatación — común a todos los escenarios (Sección 3.2)
        var enclaveAclimatacion = new Enclave
        {
            Nombre = "Cuenca Experimental de Arrakis",
            Hectareas = 5000,
            Suministros = 20000,
            TipoEnclave = TipoActividad.ACLIMATACION,
            VisitantesMensualesBase = 0
        };

        // Enclave de exhibición según escenario
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
            StockAgua = 1000,
            StockEspecia = 500,
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

        // Tabla completa de instalaciones (Sección 3.4 del PDF)
        Instalacion? nueva = codigoInstalacion switch
        {
            "ADR05" => new Instalacion { Nombre = "Roca Sellada (Aclimatación)", Tipo = TipoActividad.ACLIMATACION, CosteConstruccion = 1000, Hectareas = 10, CapacidadMaxima = 5 },
            "ADP03" => new Instalacion { Nombre = "Escudo Estático (Aclimatación)", Tipo = TipoActividad.ACLIMATACION, CosteConstruccion = 2500, Hectareas = 50, CapacidadMaxima = 3 },
            "AAV02" => new Instalacion { Nombre = "Cúpula Blindada (Aclimatación)", Tipo = TipoActividad.ACLIMATACION, CosteConstruccion = 5000, Hectareas = 100, CapacidadMaxima = 2 },
            "ASU04" => new Instalacion { Nombre = "Pozo Reforzado (Aclimatación)", Tipo = TipoActividad.ACLIMATACION, CosteConstruccion = 3500, Hectareas = 25, CapacidadMaxima = 4 },
            "EDR02" => new Instalacion { Nombre = "Roca Sellada (Exhibición)", Tipo = TipoActividad.EXHIBICION, CosteConstruccion = 21000, Hectareas = 200, CapacidadMaxima = 2 },
            "EDP03" => new Instalacion { Nombre = "Escudo Estático (Exhibición)", Tipo = TipoActividad.EXHIBICION, CosteConstruccion = 12500, Hectareas = 300, CapacidadMaxima = 3 },
            "EAV02" => new Instalacion { Nombre = "Cúpula Blindada (Exhibición)", Tipo = TipoActividad.EXHIBICION, CosteConstruccion = 15000, Hectareas = 200, CapacidadMaxima = 2 },
            "ESU03" => new Instalacion { Nombre = "Pozo Reforzado (Exhibición)", Tipo = TipoActividad.EXHIBICION, CosteConstruccion = 25000, Hectareas = 400, CapacidadMaxima = 3 },
            _ => null
        };

        if (nueva == null)
            return Results.BadRequest($"Código de instalación desconocido: {codigoInstalacion}");

        if (partidaActual.Solaris < nueva.CosteConstruccion)
            return Results.BadRequest(
                $"Solaris insuficientes. Coste: {nueva.CosteConstruccion}, disponibles: {partidaActual.Solaris}");

        // Validar que haya hectáreas libres en el enclave
        int hectareasUsadas = enclave.Instalaciones.Sum(i => i.Hectareas);
        if (hectareasUsadas + nueva.Hectareas > enclave.Hectareas)
            return Results.BadRequest(
                $"Espacio insuficiente en {enclave.Nombre}. Libres: {enclave.Hectareas - hectareasUsadas} ha, necesarias: {nueva.Hectareas} ha.");

        partidaActual.Solaris -= nueva.CosteConstruccion;
        enclave.Instalaciones.Add(nueva);
        partidaActual.RegistroEventos.Add(
            $"Construida {nueva.Nombre} en {enclave.Nombre}. Coste: {nueva.CosteConstruccion} Solaris.");

        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});
app.Run();