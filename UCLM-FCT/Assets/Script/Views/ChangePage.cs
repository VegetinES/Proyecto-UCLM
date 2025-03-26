using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChangePage : MonoBehaviour, IPointerClickHandler
{
    public GameObject nextPage;
    public GameObject actualPage;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Image clicked");
        ActivatePage();
    }
    
    private void ActivatePage()
    {
        // Desactivar página actual y activar página a la que queremos ir
        if (actualPage != null)
        {
            actualPage.SetActive(false);
        }
        else
        {
            Debug.LogError("La página actual es null. Asegúrate de asignar la referencia en el Inspector.");
        }
        
        if (nextPage != null)
        {
            nextPage.SetActive(true);
        }
        else
        {
            Debug.LogError("La página de destino es null. Asegúrate de asignar la referencia en el Inspector.");
        }
    }
}
