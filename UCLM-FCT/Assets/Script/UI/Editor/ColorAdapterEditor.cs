#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorAdapter))]
public class ColorAdapterEditor : Editor
{
    private SerializedProperty applyOnStartProp;
    private SerializedProperty listenToGlobalChangesProp;
    private SerializedProperty baseColorProp;
    private SerializedProperty changeAlphaInsteadOfColorProp;
    private SerializedProperty minAlphaPercentProp;
    
    private void OnEnable()
    {
        applyOnStartProp = serializedObject.FindProperty("applyOnStart");
        listenToGlobalChangesProp = serializedObject.FindProperty("listenToGlobalChanges");
        baseColorProp = serializedObject.FindProperty("baseColor");
        changeAlphaInsteadOfColorProp = serializedObject.FindProperty("changeAlphaInsteadOfColor");
        minAlphaPercentProp = serializedObject.FindProperty("minAlphaPercent");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        ColorAdapter adapter = (ColorAdapter)target;
        
        EditorGUILayout.PropertyField(applyOnStartProp, new GUIContent("Aplicar al inicio", "Si es true, aplicará el color automáticamente al inicio"));
        EditorGUILayout.PropertyField(listenToGlobalChangesProp, new GUIContent("Escuchar cambios globales", "Si es true, se actualizará cuando cambie la intensidad global"));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Modo de cambio", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(changeAlphaInsteadOfColorProp, new GUIContent("Cambiar opacidad en vez de color", "Si es true, cambiará la opacidad (Alpha) en lugar del color (RGB)"));
        
        // Solo mostrar el control de Alpha mínimo si está activado el cambio de Alpha
        if (changeAlphaInsteadOfColorProp.boolValue)
        {
            EditorGUILayout.PropertyField(minAlphaPercentProp, new GUIContent("Opacidad mínima (%)", "Porcentaje de opacidad para el nivel 1 de intensidad"));
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Color Base", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(baseColorProp, new GUIContent("Color Base", "Color base a partir del cual se generará el degradado"));
        
        if (GUILayout.Button("Capturar color actual"))
        {
            // Este botón llamará a la inicialización para capturar el color actual
            Undo.RecordObject(adapter, "Capture Current Color");
            adapter.Initialize();
            EditorUtility.SetDirty(adapter);
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Vista previa", EditorStyles.boldLabel);
        
        // Mostrar vista previa del degradado
        GUILayout.BeginHorizontal();
        for (int i = 1; i <= 5; i++)
        {
            Color previewColor;
            if (changeAlphaInsteadOfColorProp.boolValue)
            {
                previewColor = GetPreviewAlphaColor(adapter.GetBaseColor(), i, minAlphaPercentProp.floatValue);
            }
            else
            {
                previewColor = GetPreviewColor(adapter.GetBaseColor(), i);
            }
            
            EditorGUILayout.ColorField(new GUIContent($"Nivel {i}"), previewColor, false, true, false, GUILayout.Height(30));
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Aplicar con intensidad actual"))
        {
            // Este botón aplicará el color con la intensidad actual
            Undo.RecordObject(adapter, "Apply Color");
            adapter.ApplyColorWithIntensity(adapter.GetCurrentIntensity());
            EditorUtility.SetDirty(adapter);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private Color GetPreviewColor(Color baseColor, int intensity)
    {
        // Conversión a enteros para facilitar los cálculos
        int r = Mathf.RoundToInt(baseColor.r * 255);
        int g = Mathf.RoundToInt(baseColor.g * 255);
        int b = Mathf.RoundToInt(baseColor.b * 255);
        
        // Los factores para los 5 niveles de intensidad
        double[] factores = { 0.33, 0.5, 0.67, 0.83, 1.0 };
        
        // Asegurarse de que la intensidad esté en el rango correcto
        intensity = Mathf.Clamp(intensity, 1, 5);
        
        // Interpolar entre blanco (255,255,255) y el color base según el factor
        double factor = factores[intensity - 1];
        int newR = Interpolar(255, r, factor);
        int newG = Interpolar(255, g, factor);
        int newB = Interpolar(255, b, factor);
        
        // Crear y devolver el nuevo color
        return new Color(newR / 255f, newG / 255f, newB / 255f, baseColor.a);
    }
    
    private Color GetPreviewAlphaColor(Color baseColor, int intensity, float minAlphaPercent)
    {
        // Los factores para los 5 niveles de intensidad
        float[] factores = { minAlphaPercent / 100f, 0.5f, 0.67f, 0.83f, 1.0f };
        
        // Asegurarse de que la intensidad esté en el rango correcto
        intensity = Mathf.Clamp(intensity, 1, 5);
        
        // Calcular nuevo Alpha basado en la intensidad
        float newAlpha = factores[intensity - 1];
        
        // Crear y devolver el nuevo color
        return new Color(baseColor.r, baseColor.g, baseColor.b, newAlpha);
    }
    
    private int Interpolar(int c1, int c2, double factor)
    {
        return (int)(c1 + (c2 - c1) * factor);
    }
}
#endif