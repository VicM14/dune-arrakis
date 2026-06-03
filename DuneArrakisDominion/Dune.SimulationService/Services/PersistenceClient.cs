using Dune.Domain;
using Dune.Domain.DTOs;

namespace Dune.SimulationService.Services;

/// <summary>
/// Cliente HTTP del servicio de persistencia. Encapsula las llamadas a los
/// endpoints /persistir/* para que los controladores no necesiten conocer la
/// URL ni el formato de las peticiones. La URL base se inyecta por
/// configuración (Services:PersistenceServiceUrl en appsettings.json).
/// </summary>
public interface IPersistenceClient
{
    Task<bool> GuardarPartidaAsync(Partida partida, CancellationToken cancellationToken = default);
    Task<Partida?> CargarUltimaAsync(CancellationToken cancellationToken = default);
    Task<Partida?> CargarPorIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<PartidaResumenDTO>> ListarPartidasAsync(CancellationToken cancellationToken = default);
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

    public async Task<Partida?> CargarUltimaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/persistir/cargar-ultima", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Partida>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo cargar la última partida del PersistenceService.");
            return null;
        }
    }

    public async Task<Partida?> CargarPorIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/persistir/cargar/{Uri.EscapeDataString(id)}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Partida>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo cargar la partida {Id} del PersistenceService.", id);
            return null;
        }
    }

    public async Task<List<PartidaResumenDTO>> ListarPartidasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lista = await _http.GetFromJsonAsync<List<PartidaResumenDTO>>(
                $"{_baseUrl}/persistir/listar", cancellationToken);
            return lista ?? new List<PartidaResumenDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo listar las partidas del PersistenceService.");
            return new List<PartidaResumenDTO>();
        }
    }
}