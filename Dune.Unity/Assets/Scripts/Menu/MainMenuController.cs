using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI errorText;
    public Button btnCargar;

    void Start()
    {
        if (errorText != null) errorText.text = "";

        // Asegurarse de que GameManager existe en la escena
        if (GameManager.Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        GameManager.OnError += MostrarError;
        GameManager.OnEstadoActualizado += OnPartidaCargada;
    }

    void OnDestroy()
    {
        GameManager.OnError -= MostrarError;
        GameManager.OnEstadoActualizado -= OnPartidaCargada;
    }

    public void OnNuevaPartidaClick()
    {
        SceneManager.LoadScene("ScenarioSelect");
    }

    public void OnCargarPartidaClick()
    {
        if (errorText != null) errorText.text = "Conectando...";
        if (btnCargar != null) btnCargar.interactable = false;
        GameManager.Instance.CargarPartidaGuardada();
    }

    private void OnPartidaCargada(PartidaData partida)
    {
        SceneManager.LoadScene("GameView");
    }

    private void MostrarError(string msg)
    {
        if (errorText != null) errorText.text = msg;
        if (btnCargar != null) btnCargar.interactable = true;
    }
}
