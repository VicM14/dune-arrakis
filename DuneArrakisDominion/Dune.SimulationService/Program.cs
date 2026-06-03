using Dune.SimulationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SimulationState>();
builder.Services.AddHttpClient<IPersistenceClient, PersistenceClient>()
    .AddStandardResilienceHandler(options =>
    {
        // Reintenta hasta 3 veces con backoff exponencial si el PersistenceService
        // no responde o devuelve 5xx. Cubre fallos parciales en sistemas distribuidos
        // (Sección 3.9 del PDF: "políticas de reintento, indisponibilidad temporal").
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
    });
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.NotificationPublisherType = typeof(MediatR.NotificationPublishers.TaskWhenAllPublisher);
});
builder.Services.AddCors(options =>
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// BOOTSTRAP — recuperar la partida guardada al arrancar (Sección 2.5 del PDF:
// sincronización de estado entre servicios distribuidos).
// Si el PersistenceService no está disponible o no hay partida, se continúa
// con una partida vacía — tolerancia a fallos parciales.
// ─────────────────────────────────────────────────────────────────────────────
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var persistenceClient = app.Services.GetRequiredService<IPersistenceClient>();
    var state = app.Services.GetRequiredService<SimulationState>();
    var partida = await persistenceClient.CargarUltimaAsync();
    if (partida != null)
    {
        state.PartidaActual = partida;
        logger.LogInformation("[BOOTSTRAP] Partida cargada: {Nombre}, mes {Mes}.",
            partida.NombreJugador, partida.MesActual);
    }
    else
    {
        logger.LogInformation("[BOOTSTRAP] Sin partida guardada — iniciando estado vacío.");
    }
}
catch (Exception ex)
{
    logger.LogWarning(ex, "[BOOTSTRAP] Fallo al conectar con PersistenceService — estado vacío.");
}

app.UseCors("AllowUnity");
app.MapControllers();
app.Run();