using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChangePage : MonoBehaviour, IPointerClickHandler
{
    // CAMBIOS POR HACER:
    /*
     * Hacer un script exclusivo para las páginas de Login, Register y Profile, pues se tiene que
     * detectar si hay una sesión iniciada o no
     *
     * Evaluar si es bueno: en una misma Imagen que actúe de página que se pongan todos los elementos de Profile,
     * Login y Register, y que solo se activen si hay una sesión iniciada y demás
     */
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
