using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(CustomTiledImage))]
public class CustomTiledImageEditor : ImageEditor {
    private SerializedProperty m_FlipHorizontal;
    private SerializedProperty m_FlipVertical;

    protected override void OnEnable() {
        base.OnEnable();
        
        m_FlipHorizontal = serializedObject.FindProperty("m_FlipHorizontal");
        m_FlipVertical = serializedObject.FindProperty("m_FlipVertical");
    }
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AlleyLabs Customized Stuffs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_FlipHorizontal, new GUIContent("Flip Horizontal When Tiled"));
        EditorGUILayout.PropertyField(m_FlipVertical, new GUIContent("Flip Vertical When Tiled"));
        
//        serializedObject.Update();
        serializedObject.ApplyModifiedProperties();
    }
}
