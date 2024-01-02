using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Avatar), true)]
public class AvatarEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorUtility.SetDirty(target);
        base.OnInspectorGUI();

        if (((Avatar)target).Calibrated)
        {
            if (((Avatar)target).calibrationData)
            {
                if (GUILayout.Button("Store Calibration Data"))
                {
                    if (Application.isPlaying)
                        Write();
                    else
                        Debug.LogError("Must be in play mode to write calibration data.");
                }
            }

            if (GUILayout.Button("Store to New Calibration Data"))
            {
                ((Avatar)target).calibrationData = PersistentCalibrationData.CreateData((target).name + GUID.Generate().ToString());
                if (Application.isPlaying)
                    Write();
                ((Avatar)target).calibrationData.Dirty();
            }
        }
        else
        {
            GUILayout.Space(10);
            GUILayout.Label("You must calibrate this avatar 1-time,\nfor persistent calibration options to be available.");
        }
        
    }

    private void Write()
    {
        ((Avatar)target).StoreCalibration();
        EditorUtility.DisplayDialog("Message", "Wrote calibration data successfully. You can stop play mode now.\n" +
            "Make sure that you assign the newly created data after stopping play mode.", "Ok");
    }
}