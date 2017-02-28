using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameControl))]
public class GameSessionInspector : Editor {

    string[] presetChoices = new[] { "default", "amplified", "custom" };
    int presetChoice = 0;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        GameControl gameSession = (GameControl)target;

        if (GUILayout.Button("Regenerate map")) {
            gameSession.generateMap();
        }

        presetChoice = EditorGUILayout.Popup(presetChoice, presetChoices);
        gameSession.setPreset(presetChoices[presetChoice]);
       
        // Save the changes back to the object
        EditorUtility.SetDirty(target);
    }
}

[CustomEditor(typeof(WaterOverlay))]
public class WaterOverlayInspector : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (GUILayout.Button("Regenerate water")) {
            WaterOverlay waterOverlay = (WaterOverlay)target;
            waterOverlay.regenerateVoronoi(false);
        }
    }
}
