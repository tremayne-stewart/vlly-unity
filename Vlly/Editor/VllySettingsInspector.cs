// https://docs.unity3d.com/ScriptReference/Editor.OnInspectorGUI.html

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace vlly.editor {
    [CustomEditor(typeof(VllySettings))]

    internal class VllySettingsInspector : Editor {
      internal static class Styles {
          public  static readonly GUIStyle _labelStyle = new GUIStyle(EditorStyles.label) {
              name = "url-label",
              richText = true,
              normal = new GUIStyleState { textColor = new Color(79 / 255f, 128 / 255f, 248 / 255f) },
          };
      }
      public static readonly GUIContent _getApiKeyText = new GUIContent("Get Api Key");
      public static readonly string _getApiKeyUri = "https://www.usevlly.com";
      public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        DisplayLink(_getApiKeyText, _getApiKeyUri, 0);

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.BeginVertical(), new Color(0.4f, 0.4f, 0.4f));
        EditorGUILayout.LabelField(
          new GUIContent("DistinctId", 
            "The current distinct ID that will be sent in API calls."), 
          new GUIContent(VllyStorage.DistinctId));
        EditorGUILayout.EndVertical();
      }

      private void DisplayLink(GUIContent text, string uri, int leftMargin)
      {
        var labelStyle = Styles._labelStyle;
        var size = labelStyle.CalcSize(text);
        var uriRect = GUILayoutUtility.GetRect(text, labelStyle);
        uriRect.x += leftMargin;
        uriRect.width = size.x;
        if (GUI.Button(uriRect, text, labelStyle)) {
            System.Diagnostics.Process.Start(uri);
        }
        EditorGUIUtility.AddCursorRect(uriRect, MouseCursor.Link);
        EditorGUI.DrawRect(new Rect(uriRect.x, uriRect.y + uriRect.height - 1, uriRect.width, 1), labelStyle.normal.textColor);
      }
    }
}
