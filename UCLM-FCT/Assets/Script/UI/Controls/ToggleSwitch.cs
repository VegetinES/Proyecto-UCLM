using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Slider slider;
    
    [SerializeField] bool isOn = true;
        
    private void Start()
    {
        // Configuración inicial
        slider.interactable = false; // Se desactiva la interración inicial del slider
        UpdateSliderValue();
    }
        
    // Este método se llama cuando el usuario hace clic en el objeto
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleState();
    }
        
    // Cambia el estado del switch
    public void ToggleState()
    {
        isOn = !isOn;
        UpdateSliderValue();
        
        Debug.Log("El switch está ahora: " + (isOn ? "ON" : "OFF"));
    }
        
    // Actualiza visualmente el slider con el valor
    private void UpdateSliderValue()
    {
        slider.value = isOn ? 1 : 0;
    }
        
    // Método público para obtener el estado actual del slider
    public bool IsOn()
    {
        return isOn;
    }
        
    // Método público para establecer el estado del switch
    public void SetState(bool state)
    {
        isOn = state;
        UpdateSliderValue();
    }
}
