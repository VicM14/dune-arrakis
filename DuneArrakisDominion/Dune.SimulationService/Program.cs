using Dune.Domain;
using System.Net.Http.Json;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowUnity");

// ----- ESTADO Y CONCURRENCIA -----
SemaphoreSlim _simLock = new SemaphoreSlim(1, 1);
var partidaActual = new Partida();

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

// LÓGICA DE RONDA AVANZADA (Entrega Final)
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
            // 1. Actualizar Población de Visitantes (Algoritmo 3.3)
            enclave.ActualizarVisitantes();

            foreach (var inst in enclave.Instalaciones)
            {
                // 2. Generar Visitantes Reales para este mes (Niveles Adquisitivos)
                inst.VisitantesActuales.Clear();
                int numVisitantes = Math.Min(enclave.PoblacionVisitantes / 10, 50); // Capacidad simulada
                for (int i = 0; i < numVisitantes; i++)
                {
                    var nivel = (NivelAdquisitivo)rng.Next(0, 3);
                    inst.VisitantesActuales.Add(new Visitante(nivel));
                }

                // 3. Costes de Mantenimiento (Sección 3.6)
                gastosTotales += inst.CalcularCosteMantenimiento();

                foreach (var criatura in inst.Criaturas)
                {
                    if (criatura.Salud > 0)
                    {
                        // 4. Consumo de Recursos (Agua y Especia)
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
                            partidaActual.RegistroEventos.Add($"¡ALERTA! Falta de recursos para {criatura.Nombre}");
                        }

                        // 5. Donaciones (Sección 3.4 con factor de visitante)
                        if (inst.Tipo == TipoActividad.EXHIBICION)
                        {
                            double donacion = inst.CalcularDonacionesTotales(enclave.Nivel);
                            ingresosTotales += donacion;
                        }

                        criatura.EdadActual++;
                    }
                }
            }
        }

        // 6. Balance Económico Final
        partidaActual.Solaris += (ingresosTotales - gastosTotales);
        partidaActual.RegistroEventos.Add($"Finanzas: +{ingresosTotales:F2} Solaris | -{gastosTotales:F2} Gastos");

        // 7. Persistencia Automática
        var client = clientFactory.CreateClient();
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});

// Endpoint para comprar suministros (Necesario para la UI de Unity)
app.MapPost("/simulacion/comprar-recursos", async (double agua, double especia, IHttpClientFactory clientFactory) =>
{
    await _simLock.WaitAsync();
    try
    {
        double coste = (agua * 2) + (especia * 10);
        if (partidaActual.Solaris < coste) return Results.BadRequest("Solaris insuficientes.");

        partidaActual.Solaris -= coste;
        partidaActual.StockAgua += agua;
        partidaActual.StockEspecia += especia;

        return Results.Ok(partidaActual);
    }
    finally { _simLock.Release(); }
});

app.Run();
