using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnclavePanelUI : MonoBehaviour
{
    [Header("Enclave Aclimatacion")]
    public TextMeshProUGUI textoNombreAclim;
    public TextMeshProUGUI textoSuministrosAclim;
    public TextMeshProUGUI textoInstalacionesAclim;

    [Header("Enclave Exhibicion")]
    public TextMeshProUGUI textoNombreExhib;
    public TextMeshProUGUI textoSuministrosExhib;
    public TextMeshProUGUI textoVisitantes;
    public TextMeshProUGUI textoInstalacionesExhib;

    public void Actualizar(PartidaData partida)
    {
        if (partida.Enclaves == null) return;

        foreach (var enclave in partida.Enclaves)
        {
            bool esAclim = enclave.TipoEnclave == 0; // 0 = ACLIMATACION

            if (esAclim)
            {
                if (textoNombreAclim != null)
                    textoNombreAclim.text = enclave.Nombre.ToUpper();
                if (textoSuministrosAclim != null)
                    textoSuministrosAclim.text = $"Suministros: {enclave.Suministros} / {enclave.CapacidadAlmacen}";
                if (textoInstalacionesAclim != null)
                    textoInstalacionesAclim.text = $"Instalaciones: {enclave.Instalaciones?.Count ?? 0}";
            }
            else
            {
                if (textoNombreExhib != null)
                    textoNombreExhib.text = enclave.Nombre.ToUpper();
                if (textoSuministrosExhib != null)
                    textoSuministrosExhib.text = $"Suministros: {enclave.Suministros} / {enclave.CapacidadAlmacen}";
                if (textoVisitantes != null)
                    textoVisitantes.text = $"Visitantes: {enclave.PoblacionVisitantes}";
                if (textoInstalacionesExhib != null)
                    textoInstalacionesExhib.text = $"Instalaciones: {enclave.Instalaciones?.Count ?? 0}";
            }
        }
    }
}
