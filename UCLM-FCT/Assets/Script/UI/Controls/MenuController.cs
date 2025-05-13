using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    
    private void Start()
    {
        // Asignar listeners a los botones
        level1Button.onClick.AddListener(OnLevel1ButtonClick);
        level2Button.onClick.AddListener(OnLevel2ButtonClick);
        level3Button.onClick.AddListener(OnLevel3ButtonClick);
    }
    
    public void OnLevel1ButtonClick()
    {
        LevelLoader.LoadLevel(1);
    }

    public void OnLevel2ButtonClick()
    {
        LevelLoader.LoadLevel(2);
    }

    public void OnLevel3ButtonClick()
    {
        LevelLoader.LoadLevel(3);
    }
}