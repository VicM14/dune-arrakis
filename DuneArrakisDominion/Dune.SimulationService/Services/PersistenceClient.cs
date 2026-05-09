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
}

public class PersistenceClient : IPersistenceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PersistenceClient> _logger;
    private const string PersistenceUrl = "http://localhost:5032/persistir/guardar";

    public PersistenceClient(HttpClient http, ILogger<PersistenceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> GuardarPartidaAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(PersistenceUrl, partida, cancellationToken);
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
}
