using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnclavePanelUI : MonoBehaviour
{
    [Header("Enclave Aclimatación")]
    public TextMeshProUGUI textoNombreAclim;
    public TextMeshProUGUI textoSuministrosAclim;
    public TextMeshProUGUI textoInstalacionesAclim;

    [Header("Enclave Exhibición")]
    public TextMeshProUGUI textoNombreExhib;
    public TextMeshProUGUI textoSuministrosExhib;
    public TextMeshProUGUI textoVisitantes;
    public TextMeshProUGUI textoInstalacionesExhib;

    [Header("Lista de instalaciones")]
    public Transform contenedorInstalaciones;
    public GameObject prefabInstalacionBtn;

    private List<GameObject> botonesInstalacion = new();

    // Para el flujo de traslado
    private bool modoSeleccionDestino = false;
    private CriaturaData criaturaEnTraslado = null;
    private InstalacionData instalacionOrigen = null;

    public void Actualizar(PartidaData partida)
    {
        if (partida.Enclaves == null) return;

        foreach (var enclave in partida.Enclaves)
        {
            bool esAclim = enclave.TipoEnclave == 0;

            if (esAclim)
            {
                if (textoNombreAclim != null) textoNombreAclim.text = enclave.Nombre.ToUpper();
                if (textoSuministrosAclim != null) textoSuministrosAclim.text = $"Sumin: {enclave.Suministros}/{enclave.CapacidadAlmacen}";
                if (textoInstalacionesAclim != null) textoInstalacionesAclim.text = $"Instalaciones: {enclave.Instalaciones?.Count ?? 0}";
            }
            else
            {
                if (textoNombreExhib != null) textoNombreExhib.text = enclave.Nombre.ToUpper();
                if (textoSuministrosExhib != null) textoSuministrosExhib.text = $"Sumin: {enclave.Suministros}/{enclave.CapacidadAlmacen}";
                if (textoVisitantes != null) textoVisitantes.text = $"Visitantes: {enclave.PoblacionVisitantes}";
                if (textoInstalacionesExhib != null) textoInstalacionesExhib.text = $"Instalaciones: {enclave.Instalaciones?.Count ?? 0}";
            }
        }

        GenerarBotonesInstalaciones(partida);
    }

    private void GenerarBotonesInstalaciones(PartidaData partida)
    {
        if (contenedorInstalaciones == null || prefabInstalacionBtn == null) return;

        foreach (var go in botonesInstalacion) Destroy(go);
        botonesInstalacion.Clear();

        foreach (var enclave in partida.Enclaves)
        {
            if (enclave.Instalaciones == null) continue;
            foreach (var inst in enclave.Instalaciones)
            {
                var go = Instantiate(prefabInstalacionBtn, contenedorInstalaciones);
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = $"{inst.Codigo} [{inst.Criaturas?.Count ?? 0}/{inst.CapacidadMaxima}]";

                var btn = go.GetComponent<Button>();
                var instCaptura = inst;
                btn?.onClick.AddListener(() => OnInstalacionClick(instCaptura));

                botonesInstalacion.Add(go);
            }
        }
    }

    private void OnInstalacionClick(InstalacionData inst)
    {
        if (modoSeleccionDestino)
        {
            // El usuario está seleccionando destino para traslado
            if (inst.Tipo == 1) // EXHIBICION
            {
                GameManager.Instance.TrasladarCriatura(
                    criaturaEnTraslado.Id,
                    instalacionOrigen.Id,
                    inst.Id);
                DesactivarModoSeleccionDestino();
            }
            else
            {
                UIManager.Instance?.enclavePanel?.gameObject
                    .GetComponentInChildren<TextMeshProUGUI>()?.gameObject
                    .SetActive(false);
                Debug.Log("Selecciona una instalación de EXHIBICIÓN como destino.");
            }
            return;
        }

        // Modo normal: mostrar detalle de la instalación
        UIManager.Instance?.detailPanel?.MostrarInstalacion(inst);

        // Si tiene criaturas, mostrar la primera con más salud
        if (inst.Criaturas != null && inst.Criaturas.Count > 0)
        {
            var mejorCriatura = inst.Criaturas[0];
            foreach (var c in inst.Criaturas)
                if (c.Salud > mejorCriatura.Salud) mejorCriatura = c;
            UIManager.Instance?.detailPanel?.MostrarCriatura(mejorCriatura);
        }
    }

    public void ActivarModoSeleccionDestino(CriaturaData criatura)
    {
        modoSeleccionDestino = true;
        criaturaEnTraslado = criatura;

        // Encontrar la instalación de origen de la criatura
        var partida = GameManager.Instance.PartidaActual;
        if (partida?.Enclaves != null)
        {
            foreach (var enclave in partida.Enclaves)
                foreach (var inst in enclave.Instalaciones ?? new System.Collections.Generic.List<InstalacionData>())
                    foreach (var c in inst.Criaturas ?? new System.Collections.Generic.List<CriaturaData>())
                    {
                        if (c.Id == criatura.Id) instalacionOrigen = inst;
                    }
        }
    }

    private void DesactivarModoSeleccionDestino()
    {
        modoSeleccionDestino = false;
        criaturaEnTraslado = null;
        instalacionOrigen = null;
    }
}
