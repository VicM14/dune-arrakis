using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputNombre;
    [SerializeField] private TMP_Dropdown dropdownEscenario;
    [SerializeField] private Button btnNuevaPartida;
    [SerializeField] private Button btnCargarPartida;
    [SerializeField] private TMP_Text txtError;

    void OnEnable()
    {
        GameManager.OnError += MostrarError;
        GameManager.OnEstadoActualizado += _ => IrAJuego();
    }
    void OnDisable()
    {
        GameManager.OnError -= MostrarError;
        GameManager.OnEstadoActualizado -= _ => IrAJuego();
    }

    public void OnNuevaPartida()
    {
        string[] escenarios = { "Arrakeen", "GiediPrime", "Caladan" };
        string nombre = inputNombre.text.Trim();
        if (string.IsNullOrEmpty(nombre)) { txtError.text = "Escribe tu nombre."; return; }
        GameManager.Instance.IniciarPartida(nombre, escenarios[dropdownEscenario.value]);
    }

    public void OnCargarPartida() => GameManager.Instance.CargarPartidaGuardada();

    private void IrAJuego() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameView");

    private void MostrarError(string msg) => txtError.text = msg;
}
