using Dune.Domain;
using System.Net.Http.Json;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowUnity");

// --- GESTIÓN DE CONCURRENCIA Y ESTADO ---
// Semáforo para asegurar que solo se procese una ronda a la vez (Sincronización)
SemaphoreSlim _simLock = new SemaphoreSlim(1, 1);

// Estado de la partida en memoria
var partidaActual = new Partida();

// --- ENDPOINTS ---

// Endpoint para ejecutar la ronda mensual (Lógica Principal Entrega 3)
app.MapPost("/simulacion/ejecutar-ronda", async (IHttpClientFactory clientFactory) =>
{
    // 1. Concurrencia: Esperar si hay otra simulación en curso
    await _simLock.WaitAsync();

    try
    {
        partidaActual.MesActual++;
        partidaActual.RegistroEventos.Add($"--- Mes {partidaActual.MesActual} ---");

        foreach (var enclave in partidaActual.Enclaves)
        {
            foreach (var inst in enclave.Instalaciones)
            {
                foreach (var criatura in inst.Criaturas)
                {
                    if (criatura.Salud > 0)
                    {
                        // Lógica de Alimentación (Fórmulas Sección 3.5)
                        double comidaDisponible = 100; // Simulado
                        criatura.Alimentar(comidaDisponible, inst.Tipo);

                        // Lógica de Donaciones (Sección 3.4)
                        if (inst.Tipo == TipoActividad.EXHIBICION)
                        {
                            double donacion = inst.CalcularDonacion(criatura, enclave.Nivel);
                            partidaActual.Solaris += donacion;
                            partidaActual.RegistroEventos.Add($"Donación: {donacion:F2} Solaris de {criatura.Nombre}");
                        }

                        criatura.EdadActual++;
                    }
                }
            }
            // Algoritmo de Visitantes (Sección 3.3)
            enclave.PoblacionVisitantes = (int)(enclave.PoblacionVisitantes * 1.05);
        }

        // 2. Consistencia: Guardado automático en el PersistenceService
        var client = clientFactory.CreateClient();
        // Asegúrate de que el puerto 5032 sea el de tu PersistenceService
        await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partidaActual);

        return Results.Ok(partidaActual);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error en la simulación: {ex.Message}");
    }
    finally
    {
        _simLock.Release(); // Liberar el bloqueo siempre
    }
});

app.MapGet("/estado-inicial", () => Results.Ok(partidaActual));

app.Run();


