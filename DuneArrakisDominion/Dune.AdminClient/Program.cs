using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using Dune.Domain;
using Dune.Domain.DTOs;

Console.WriteLine("--- DUNE: IMPERIAL COMMAND CONSOLE ---");

using var client = new HttpClient();
const string SimUrl = "http://localhost:5000";
bool salir = false;

while (!salir)
{
    Console.WriteLine("\n========================================");
    Console.WriteLine("[1] Iniciar Partida (escenario + Cuenca Experimental)");
    Console.WriteLine("[2] Ejecutar Ronda Mensual");
    Console.WriteLine("[3] Comprar Suministros (5 Solaris/unidad)");
    Console.WriteLine("[4] Mover Suministros a una Instalación");
    Console.WriteLine("[5] Construir Instalación");
    Console.WriteLine("[6] Ver Estado Detallado");
    Console.WriteLine("[7] Trasladar Criatura (aclimatación → exhibición)");
    Console.WriteLine("[8] Descartar Criatura (Bene Tleilax, 20.000 solaris)");
    Console.WriteLine("[L] Listar Partidas Guardadas");
    Console.WriteLine("[C] Cargar Partida");
    Console.WriteLine("[G] Guardar Partida");
    Console.WriteLine("[S] Salir (guarda automáticamente)");
    Console.WriteLine("========================================");
    Console.Write("Selecciona una opción: ");

    var opcion = Console.ReadLine()?.ToUpper();

    switch (opcion)
    {
        case "1": await IniciarPartida(client); break;
        case "2": await EjecutarRonda(client); break;
        case "3": await ComprarSuministros(client); break;
        case "4": await MoverSuministros(client); break;
        case "5": await ConstruirInstalacion(client); break;
        case "6": await VerEstado(client); break;
        case "7": await TrasladarCriatura(client); break;
        case "8": await DescartarCriatura(client); break;
        case "L": await ListarPartidas(client); break;
        case "C": await CargarPartida(client); break;
        case "G": await GuardarPartida(client); break;
        case "S":
            await GuardarAlSalir(client);
            salir = true;
            break;
    }
}

async Task IniciarPartida(HttpClient client)
{
    Console.WriteLine("Escenarios: Arrakeen / GiediPrime / Caladan");
    Console.Write("Selecciona escenario: ");
    string escenario = Console.ReadLine()?.Trim() ?? "Arrakeen";

    try
    {
        string nombreCodificado = Uri.EscapeDataString("Paul Atreides");
        string escenarioCodificado = Uri.EscapeDataString(escenario);

        var response = await client.PostAsync(
            $"{SimUrl}/simulacion/iniciar-partida?nombreJugador={nombreCodificado}&nombreEscenario={escenarioCodificado}",
            null);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($">> Partida iniciada en escenario {escenario}.");
            Console.WriteLine(">> Usa la opción 6 para ver el estado actual.");
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($">> Error del servidor ({(int)response.StatusCode}): {error}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($">> No se pudo conectar con el SimulationService: {ex.Message}");
        Console.WriteLine(">> Asegúrate de que Dune.SimulationService está corriendo en el puerto 5000.");
    }
}

async Task EjecutarRonda(HttpClient client)
{
    Console.WriteLine(">> Procesando ciclo planetario...");
    var response = await client.PostAsJsonAsync($"{SimUrl}/simulacion/ejecutar-ronda", new { });

    if (response.IsSuccessStatusCode)
    {
        var p = await response.Content.ReadFromJsonAsync<Partida>();
        Console.WriteLine($"\n--- INFORME MES {p?.MesActual} ---");
        Console.WriteLine($"Solaris: {p?.Solaris:F2}");
        if (p?.RegistroEventos.Count > 0)
        {
            int n = Math.Min(5, p.RegistroEventos.Count);
            for (int i = p.RegistroEventos.Count - n; i < p.RegistroEventos.Count; i++)
                Console.WriteLine($"  >> {p.RegistroEventos[i]}");
        }
    }
    else
    {
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
    }
}

async Task ComprarSuministros(HttpClient client)
{
    var partida = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    if (partida == null || partida.Enclaves.Count == 0)
    {
        Console.WriteLine(">> No hay partida activa. Usa la opción 1 primero.");
        return;
    }

    Console.WriteLine("Enclaves disponibles:");
    for (int i = 0; i < partida.Enclaves.Count; i++)
    {
        var e = partida.Enclaves[i];
        Console.WriteLine($"  [{i + 1}] {e.Nombre} — almacén {e.Suministros}/{e.Hectareas * 3}");
    }

    Console.Write("Selecciona enclave (número): ");
    if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > partida.Enclaves.Count)
    {
        Console.WriteLine(">> Selección inválida.");
        return;
    }
    string enclaveId = partida.Enclaves[idx - 1].Id;

    Console.Write("Cantidad de suministros a comprar (5 Solaris/unidad): ");
    if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0)
    {
        Console.WriteLine(">> Cantidad inválida.");
        return;
    }

    var response = await client.PostAsync(
        $"{SimUrl}/simulacion/comprar-suministros?enclaveId={Uri.EscapeDataString(enclaveId)}&cantidad={cantidad}", null);

    if (response.IsSuccessStatusCode)
        Console.WriteLine($">> {await response.Content.ReadAsStringAsync()}");
    else
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
}

async Task MoverSuministros(HttpClient client)
{
    var partida = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    if (partida == null || partida.Enclaves.Count == 0)
    {
        Console.WriteLine(">> No hay partida activa.");
        return;
    }

    Console.WriteLine("Enclaves disponibles:");
    for (int i = 0; i < partida.Enclaves.Count; i++)
    {
        var e = partida.Enclaves[i];
        Console.WriteLine($"  [{i + 1}] {e.Nombre} — almacén: {e.Suministros}, instalaciones: {e.Instalaciones.Count}");
    }
    Console.Write("Enclave (número): ");
    if (!int.TryParse(Console.ReadLine(), out int eIdx) || eIdx < 1 || eIdx > partida.Enclaves.Count) return;
    var enclave = partida.Enclaves[eIdx - 1];

    if (enclave.Instalaciones.Count == 0)
    {
        Console.WriteLine(">> Este enclave no tiene instalaciones todavía.");
        return;
    }

    Console.WriteLine("Instalaciones del enclave:");
    for (int i = 0; i < enclave.Instalaciones.Count; i++)
    {
        var ins = enclave.Instalaciones[i];
        Console.WriteLine($"  [{i + 1}] {ins.Nombre} — stock interno: {ins.Suministros}/{ins.CosteConstruccion}");
    }
    Console.Write("Instalación destino (número): ");
    if (!int.TryParse(Console.ReadLine(), out int iIdx) || iIdx < 1 || iIdx > enclave.Instalaciones.Count) return;
    var inst = enclave.Instalaciones[iIdx - 1];

    Console.Write("Cantidad a mover: ");
    if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0) return;

    var response = await client.PostAsync(
        $"{SimUrl}/simulacion/mover-suministros?enclaveId={Uri.EscapeDataString(enclave.Id)}&instalacionId={Uri.EscapeDataString(inst.Id)}&cantidad={cantidad}", null);

    if (response.IsSuccessStatusCode)
        Console.WriteLine($">> {await response.Content.ReadAsStringAsync()}");
    else
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
}

async Task ConstruirInstalacion(HttpClient client)
{
    var partida = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    if (partida == null || partida.Enclaves.Count == 0)
    {
        Console.WriteLine(">> No hay partida activa.");
        return;
    }

    Console.WriteLine("Códigos disponibles:");
    Console.WriteLine("  ACLIMATACIÓN: ADR05 (1000), ADP03 (2500), AAV02 (5000), ASU04 (3500)");
    Console.WriteLine("  EXHIBICIÓN:   EDR02 (21000), EDP03 (12500), EAV02 (15000), ESU03 (25000)");
    Console.Write("Código: ");
    string codigo = Console.ReadLine()?.Trim().ToUpper() ?? "";

    Console.WriteLine("Enclaves:");
    for (int i = 0; i < partida.Enclaves.Count; i++)
        Console.WriteLine($"  [{i + 1}] {partida.Enclaves[i].Nombre}");
    Console.Write("Enclave (número): ");
    if (!int.TryParse(Console.ReadLine(), out int eIdx) || eIdx < 1 || eIdx > partida.Enclaves.Count) return;
    string enclaveId = partida.Enclaves[eIdx - 1].Id;

    var response = await client.PostAsync(
        $"{SimUrl}/simulacion/construir-instalacion?codigoInstalacion={Uri.EscapeDataString(codigo)}&enclaveId={Uri.EscapeDataString(enclaveId)}", null);

    if (response.IsSuccessStatusCode)
        Console.WriteLine(">> Instalación construida.");
    else
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
}

async Task VerEstado(HttpClient client)
{
    var p = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    Console.WriteLine($"\n--- ESTADO DEL DOMINIO ---");
    Console.WriteLine($"Partida: {p?.IdPartida}");
    Console.WriteLine($"Jugador: {p?.NombreJugador} | Mes: {p?.MesActual} | Solaris: {p?.Solaris:F2}");
    Console.WriteLine($"Escenario: {p?.Escenario?.Nombre ?? "-"}");

    foreach (var e in p?.Enclaves ?? new())
    {
        Console.WriteLine($"\n[{e.TipoEnclave}] {e.Nombre}");
        Console.WriteLine($"  Hectáreas: {e.Hectareas} | Almacén: {e.Suministros}/{e.Hectareas * 3} | Visitantes: {e.PoblacionVisitantes} | Nivel: {e.NivelAdquisitivo}");
        if (e.Instalaciones.Count == 0)
        {
            Console.WriteLine("  (sin instalaciones)");
            continue;
        }
        foreach (var i in e.Instalaciones)
        {
            Console.WriteLine($"  · {i.Nombre} [{i.Tipo}] — Stock: {i.Suministros}/{i.CosteConstruccion} | Criaturas: {i.Criaturas.Count}/{i.CapacidadMaxima}");
            foreach (var c in i.Criaturas.OrderByDescending(c => c.Salud))
                Console.WriteLine($"      - {c.Nombre} | Salud: {c.Salud:F0} | Edad: {c.EdadActual}/{c.EdadAdulta}");
        }
    }

    // Registro de eventos en orden cronológico (Sección 3.8 del PDF: el centro
    // de mando debe mostrar los eventos cronológicamente).
    var eventos = p?.RegistroEventos ?? new();
    Console.WriteLine($"\n--- REGISTRO DE EVENTOS ({eventos.Count}) ---");
    if (eventos.Count == 0)
    {
        Console.WriteLine("  (sin eventos)");
    }
    else
    {
        int desde = Math.Max(0, eventos.Count - 20);
        if (desde > 0) Console.WriteLine($"  ... ({desde} eventos anteriores omitidos)");
        for (int i = desde; i < eventos.Count; i++)
            Console.WriteLine($"  [{i + 1}] {eventos[i]}");
    }
}

async Task TrasladarCriatura(HttpClient client)
{
    var partida = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    if (partida == null || partida.Enclaves.Count == 0)
    {
        Console.WriteLine(">> No hay partida activa.");
        return;
    }

    // Listar criaturas vivas en instalaciones de aclimatación.
    var candidatas = new List<(Criatura c, Instalacion origen, Enclave e)>();
    foreach (var e in partida.Enclaves)
        foreach (var i in e.Instalaciones.Where(x => x.Tipo == TipoActividad.ACLIMATACION))
            foreach (var c in i.Criaturas.Where(x => x.Salud > 0))
                candidatas.Add((c, i, e));

    if (candidatas.Count == 0)
    {
        Console.WriteLine(">> No hay criaturas en aclimatación.");
        return;
    }

    Console.WriteLine("Criaturas en aclimatación:");
    for (int idx = 0; idx < candidatas.Count; idx++)
    {
        var (c, i, e) = candidatas[idx];
        string adulta = c.EdadActual >= c.EdadAdulta ? "ADULTA" : "joven";
        Console.WriteLine($"  [{idx + 1}] {c.Nombre} ({adulta}) | salud {c.Salud:F0} edad {c.EdadActual}/{c.EdadAdulta} | en {i.Nombre} ({e.Nombre})");
    }
    Console.Write("Criatura (número): ");
    if (!int.TryParse(Console.ReadLine(), out int cIdx) || cIdx < 1 || cIdx > candidatas.Count) return;
    var (criatura, origen, _) = candidatas[cIdx - 1];

    // Listar instalaciones de exhibición compatibles.
    var destinos = partida.Enclaves
        .SelectMany(e => e.Instalaciones)
        .Where(i => i.Tipo == TipoActividad.EXHIBICION)
        .ToList();

    if (destinos.Count == 0)
    {
        Console.WriteLine(">> No hay instalaciones de exhibición construidas.");
        return;
    }

    Console.WriteLine("Instalaciones de exhibición:");
    for (int idx = 0; idx < destinos.Count; idx++)
    {
        var d = destinos[idx];
        bool compat = d.Medio == criatura.Habitat && d.Alimentacion == criatura.Dieta;
        string flag = compat ? "compatible" : "incompatible";
        Console.WriteLine($"  [{idx + 1}] {d.Nombre} | {d.Medio}/{d.Alimentacion} | {d.Criaturas.Count}/{d.CapacidadMaxima} | {flag}");
    }
    Console.Write("Destino (número): ");
    if (!int.TryParse(Console.ReadLine(), out int dIdx) || dIdx < 1 || dIdx > destinos.Count) return;
    var destino = destinos[dIdx - 1];

    var response = await client.PostAsync(
        $"{SimUrl}/simulacion/trasladar-criatura?criaturaId={Uri.EscapeDataString(criatura.Id)}&instalacionOrigenId={Uri.EscapeDataString(origen.Id)}&instalacionDestinoId={Uri.EscapeDataString(destino.Id)}",
        null);

    if (response.IsSuccessStatusCode)
        Console.WriteLine($">> {await response.Content.ReadAsStringAsync()}");
    else
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
}

async Task DescartarCriatura(HttpClient client)
{
    var partida = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    if (partida == null || partida.Enclaves.Count == 0)
    {
        Console.WriteLine(">> No hay partida activa.");
        return;
    }

    var todas = new List<(Criatura c, Instalacion i, Enclave e)>();
    foreach (var e in partida.Enclaves)
        foreach (var i in e.Instalaciones)
            foreach (var c in i.Criaturas)
                todas.Add((c, i, e));

    if (todas.Count == 0)
    {
        Console.WriteLine(">> No hay criaturas para descartar.");
        return;
    }

    Console.WriteLine("Criaturas (descartar cuesta 20.000 solaris):");
    for (int idx = 0; idx < todas.Count; idx++)
    {
        var (c, i, e) = todas[idx];
        Console.WriteLine($"  [{idx + 1}] {c.Nombre} | salud {c.Salud:F0} | en {i.Nombre} ({e.Nombre})");
    }
    Console.Write("Criatura (número): ");
    if (!int.TryParse(Console.ReadLine(), out int cIdx) || cIdx < 1 || cIdx > todas.Count) return;
    var seleccion = todas[cIdx - 1];

    Console.Write($"¿Confirmar descarte de {seleccion.c.Nombre}? (S/N): ");
    if (Console.ReadLine()?.Trim().ToUpper() != "S") { Console.WriteLine(">> Cancelado."); return; }

    var response = await client.PostAsync(
        $"{SimUrl}/simulacion/descartar-criatura?criaturaId={Uri.EscapeDataString(seleccion.c.Id)}", null);

    if (response.IsSuccessStatusCode)
        Console.WriteLine($">> {await response.Content.ReadAsStringAsync()}");
    else
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
}

async Task ListarPartidas(HttpClient client)
{
    try
    {
        var lista = await client.GetFromJsonAsync<List<PartidaResumenDTO>>($"{SimUrl}/simulacion/listar-partidas");
        if (lista == null || lista.Count == 0)
        {
            Console.WriteLine(">> No hay partidas guardadas.");
            return;
        }
        Console.WriteLine("\n--- PARTIDAS GUARDADAS ---");
        for (int i = 0; i < lista.Count; i++)
        {
            var r = lista[i];
            Console.WriteLine($"  [{i + 1}] {r.NombreJugador} | {r.EscenarioNombre} | Mes {r.MesActual} | {r.Solaris:F0} solaris");
            Console.WriteLine($"      id: {r.IdPartida} | guardada: {r.FechaModificacion.ToLocalTime():g}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($">> No se pudo contactar con el SimulationService: {ex.Message}");
    }
}

async Task CargarPartida(HttpClient client)
{
    List<PartidaResumenDTO>? lista;
    try
    {
        lista = await client.GetFromJsonAsync<List<PartidaResumenDTO>>($"{SimUrl}/simulacion/listar-partidas");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($">> No se pudo contactar con el SimulationService: {ex.Message}");
        return;
    }

    if (lista == null || lista.Count == 0)
    {
        Console.WriteLine(">> No hay partidas guardadas para cargar.");
        return;
    }

    Console.WriteLine("Partidas disponibles:");
    for (int i = 0; i < lista.Count; i++)
    {
        var r = lista[i];
        Console.WriteLine($"  [{i + 1}] {r.NombreJugador} | {r.EscenarioNombre} | Mes {r.MesActual} | guardada {r.FechaModificacion.ToLocalTime():g}");
    }
    Console.Write("Selecciona partida (número, o Enter para la más reciente): ");
    string entrada = Console.ReadLine()?.Trim() ?? "";

    string url = $"{SimUrl}/simulacion/cargar-partida";
    if (!string.IsNullOrEmpty(entrada))
    {
        if (!int.TryParse(entrada, out int idx) || idx < 1 || idx > lista.Count)
        {
            Console.WriteLine(">> Selección inválida.");
            return;
        }
        url += $"?id={Uri.EscapeDataString(lista[idx - 1].IdPartida)}";
    }

    var response = await client.PostAsync(url, null);
    if (response.IsSuccessStatusCode)
    {
        var p = await response.Content.ReadFromJsonAsync<Partida>();
        Console.WriteLine($">> Partida cargada: {p?.NombreJugador}, mes {p?.MesActual}, {p?.Solaris:F2} solaris.");
    }
    else
    {
        Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
    }
}

async Task GuardarPartida(HttpClient client)
{
    try
    {
        var response = await client.PostAsync($"{SimUrl}/simulacion/guardar", null);
        if (response.IsSuccessStatusCode)
            Console.WriteLine($">> {await response.Content.ReadAsStringAsync()}");
        else
            Console.WriteLine($">> Error: {await response.Content.ReadAsStringAsync()}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($">> No se pudo guardar (servicio no disponible): {ex.Message}");
    }
}

async Task GuardarAlSalir(HttpClient client)
{
    Console.WriteLine(">> Guardando partida antes de salir...");
    try
    {
        var response = await client.PostAsync($"{SimUrl}/simulacion/guardar", null);
        if (response.IsSuccessStatusCode)
            Console.WriteLine(">> Estado guardado. Hasta pronto.");
        else
            Console.WriteLine(">> No había partida activa que guardar. Saliendo.");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine(">> No se pudo guardar (servicio no disponible). Saliendo igualmente.");
    }
}