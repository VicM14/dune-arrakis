using Dune.Domain;

namespace Dune.SimulationService.Services;

/// <summary>
/// Cliente HTTP del servicio de persistencia. Encapsula la llamada al
/// endpoint /persistir/guardar para que los controladores no necesiten
/// conocer la URL ni el formato de la petición.
///
/// La URL del PersistenceService está hardcodeada por ahora; en el commit 9
/// se moverá a appsettings.json y se inyectará por configuración.
/// </summary>
public interface IPersistenceClient
{
    Task<bool> GuardarPartidaAsync(Partida partida, CancellationToken cancellationToken = default);
    Task<Partida?> CargarPartidaAsync(CancellationToken cancellationToken = default);
}

public class PersistenceClient : IPersistenceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PersistenceClient> _logger;
    private readonly string _baseUrl;

    public PersistenceClient(HttpClient http, ILogger<PersistenceClient> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _baseUrl = config["Services:PersistenceServiceUrl"]
            ?? throw new InvalidOperationException("Falta la clave de configuración Services:PersistenceServiceUrl.");
    }

    public async Task<bool> GuardarPartidaAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/persistir/guardar", partida, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Persistencia devolvió código {Code}", response.StatusCode);
                return false;
            }
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar con el PersistenceService.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al guardar la partida.");
            return false;
        }
    }

    public async Task<Partida?> CargarPartidaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/persistir/cargar", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Partida>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo cargar la partida del PersistenceService.");
            return null;
        }
    }
}
