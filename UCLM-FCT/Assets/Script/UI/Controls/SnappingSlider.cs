using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SnappingSlider : MonoBehaviour, IEndDragHandler, IPointerDownHandler
{
    private Slider slider;
    private int[] snapValues = { 1, 2, 3, 4, 5 }; // Los valores a los que saltará (de 1 a 5)
    
    void Start()
    {
        slider = GetComponent<Slider>();
        
        // Aseguramos que el slider tiene los valores correctos
        slider.minValue = 1;
        slider.maxValue = 5;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToClosestValue();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        SnapToClosestValue();
    }
    
    private void SnapToClosestValue()
    {
        // Encontrar el valor más cercano al valor actual del slider
        float currentValue = slider.value;
        float closestValue = FindClosestValue(currentValue);
        
        // Asignar el valor más cercano al slider
        slider.value = closestValue;
    }
    
    // Método para encontrar el valor más aproximado del slider según donde se ponga
    private float FindClosestValue(float value)
    {
        float closestValue = snapValues[0];
        float closestDistance = Mathf.Abs(value - closestValue);
        
        for (int i = 1; i < snapValues.Length; i++)
        {
            float distance = Mathf.Abs(value - snapValues[i]);
            if (distance < closestDistance)
            {
                closestValue = snapValues[i];
                closestDistance = distance;
            }
        }
        
        return closestValue;
    }
}