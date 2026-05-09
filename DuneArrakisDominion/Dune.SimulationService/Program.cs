using Dune.SimulationService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// SERVICIOS (Dependency Injection)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Estado de la simulación: Singleton (una sola partida activa por proceso).
builder.Services.AddSingleton<SimulationState>();

// Cliente del servicio de persistencia: registrado como HttpClient tipado.
builder.Services.AddHttpClient<IPersistenceClient, PersistenceClient>();

// MediatR: registro automático de todos los handlers del assembly.
// TaskWhenAllPublisher ejecuta todos los INotificationHandler en PARALELO
// cuando se publica un evento, sin que el publicador conozca a los suscriptores.
// Esto implementa el patrón publish/subscribe in-process (Sección 2.2 del PDF).
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.NotificationPublisherType = typeof(MediatR.NotificationPublishers.TaskWhenAllPublisher);
});

// CORS para Unity y cualquier frontend.
builder.Services.AddCors(options =>
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// MIDDLEWARE
// ─────────────────────────────────────────────────────────────────────────────
app.UseCors("AllowUnity");
app.MapControllers();

app.Run();
