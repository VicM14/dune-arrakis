using Dune.SimulationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SimulationState>();
builder.Services.AddHttpClient<IPersistenceClient, PersistenceClient>();
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
    var partida = await persistenceClient.CargarPartidaAsync();
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
