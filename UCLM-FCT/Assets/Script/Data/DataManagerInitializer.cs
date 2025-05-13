using UnityEngine;

public class DataManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        if (DataManager.Instance == null)
        {
            GameObject go = new GameObject("DataManager");
            go.AddComponent<DataManager>();
            Debug.Log("DataManagerInitializer: DataManager creado");
        }
    }
}