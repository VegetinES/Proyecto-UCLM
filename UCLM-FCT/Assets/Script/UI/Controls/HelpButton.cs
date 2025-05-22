using UnityEngine;
using UnityEngine.EventSystems;

public class HelpButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float helpDuration = 3.0f; // Duración de la ayuda en segundos
    
    private BoardManager boardManager;
    private bool helpIsActive = false;
    
    private void Start()
    {
        // Obtener referencia al BoardManager en la escena
        boardManager = FindFirstObjectByType<BoardManager>();
        
        if (boardManager == null)
        {
            Debug.LogError("HelpButton: No se pudo encontrar el BoardManager");
            // No es necesario deshabilitar el botón ya que el script está directamente en el objeto
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (helpIsActive || boardManager == null) return;
        
        // Registrar el uso de ayuda en el StatisticsManager
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.RegisterHelpUsed();
        }
        
        // Mostrar las casillas sugeridas en el tablero
        boardManager.ShowHint();
        
        // Evitar múltiples activaciones mientras está activa la ayuda
        helpIsActive = true;
        
        // Desactivar temporalmente este objeto para que no se pueda hacer clic
        this.enabled = false;
        
        // Programar la reactivación después de un tiempo
        Invoke("ReactivateHelp", helpDuration);
        
        Debug.Log("HelpButton: Ayuda activada");
    }
    
    private void ReactivateHelp()
    {
        helpIsActive = false;
        
        // Reactivar el componente
        this.enabled = true;
        
        Debug.Log("HelpButton: Ayuda reactivada");
    }
}