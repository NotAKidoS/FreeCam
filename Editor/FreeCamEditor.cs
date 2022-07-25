// using System.Collections.Concurrent;
// using System.Threading.Tasks;
// using System.Text;
using static System.Math;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections;

[InitializeOnLoadAttribute]
public class FreeCamPersistHandler {
    // register an event handler when the class is initialized
    static FreeCamPersistHandler() {
        EditorApplication.playModeStateChanged -= PlayModeState;
        EditorApplication.playModeStateChanged += PlayModeState;
    }

    static Vector3 persistPos;
    static Quaternion persistRot;
    static Camera sceneCam;
    static GameObject freeCam;
    static FreeCam freeCamScript;

    private static void PlayModeState(PlayModeStateChange state) {
        //find freecam
        if (freeCam == null) {
            FreeCam[] freecamscripts = Object.FindObjectsOfType < FreeCam > ();
            if (freecamscripts.Count() == 1) {
                freeCam = freecamscripts[0].gameObject;
                freeCamScript = freecamscripts[0];
            }
        } else {
            switch (state) {
            case PlayModeStateChange.ExitingPlayMode:
                if (freeCamScript.persistFromPlayMode) {
                    persistPos = freeCam.transform.position;
                    persistRot = freeCam.transform.rotation;
                }
                break;
            case PlayModeStateChange.EnteredEditMode:
                if (freeCamScript.persistFromPlayMode) {
                    freeCam.transform.position = persistPos;
                    freeCam.transform.rotation = persistRot;
                }
                break;
            case PlayModeStateChange.ExitingEditMode:
                if (freeCamScript.startFromSceneView) {
                    SceneView sceneCam = SceneView.lastActiveSceneView;
                    freeCam.transform.position = sceneCam.camera.transform.position;
                    freeCam.transform.rotation = sceneCam.camera.transform.rotation;
                }
                break;
            case PlayModeStateChange.EnteredPlayMode:
                if (freeCamScript.autoFixPhysBoneHelper) {
                    FreeCamEditor.FixCamPriority(freeCamScript);
                }
                break;
            }
        }
    }
}

[CustomEditor(typeof(FreeCam))]
public class FreeCamEditor: Editor {

    //avatardynamics
    private SerializedProperty contactTesting;
    private SerializedProperty autoFixPhysBoneHelper;

    //persistance
    private SerializedProperty startFromSceneView;
    private SerializedProperty persistFromPlayMode;

    //freecam config
    private SerializedProperty movementSpeed;
    private SerializedProperty fastMovementSpeed;
    private SerializedProperty freeLookSensitivity;
    private SerializedProperty zoomSensitivity;
    private SerializedProperty fastZoomSensitivity;

    private void OnEnable() {
        //avatardynamics
        contactTesting = serializedObject.FindProperty("contactTesting");
        autoFixPhysBoneHelper = serializedObject.FindProperty("autoFixPhysBoneHelper");
        //persistance
        persistFromPlayMode = serializedObject.FindProperty("persistFromPlayMode");
        startFromSceneView = serializedObject.FindProperty("startFromSceneView");
        //configuration
        movementSpeed = serializedObject.FindProperty("movementSpeed");
        fastMovementSpeed = serializedObject.FindProperty("fastMovementSpeed");
        freeLookSensitivity = serializedObject.FindProperty("freeLookSensitivity");
        zoomSensitivity = serializedObject.FindProperty("zoomSensitivity");
        fastZoomSensitivity = serializedObject.FindProperty("fastZoomSensitivity");
    }

    public override void OnInspectorGUI() {
        serializedObject.UpdateIfDirtyOrScript();

        GUIStyle box = GUI.skin.GetStyle("box");

        GUILayout.Label("FreeCam Configuration", "boldlabel");
        using(new GUILayout.VerticalScope(box)) {
            EditorGUILayout.PropertyField(movementSpeed);
            EditorGUILayout.PropertyField(fastMovementSpeed);
            EditorGUILayout.PropertyField(freeLookSensitivity);
            EditorGUILayout.PropertyField(zoomSensitivity);
            EditorGUILayout.PropertyField(fastZoomSensitivity);
        }

        GUILayout.Label("PlayMode Persistance", "boldlabel");
        using(new GUILayout.VerticalScope(box)) {
            //location persist button
            EditorGUILayout.PropertyField(startFromSceneView);
            EditorGUILayout.PropertyField(persistFromPlayMode);
        }

        GUILayout.Label("Avatar Dynamics", "boldlabel");
        using(new GUILayout.VerticalScope(box)) {
            //ADContactTester button
            EditorGUILayout.PropertyField(contactTesting);
            EditorGUILayout.HelpBox("Allows you to activate contact receivers via mouse click from GameView! No proximity or capsule support as of yet.", MessageType.Info);
            EditorGUILayout.PropertyField(autoFixPhysBoneHelper);
            EditorGUILayout.HelpBox("Temporarily disables all other cameras while regenerating PhysBoneGrabHelper to guarantee FreeCam takes priority.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    public static void FixCamPriority(FreeCam activeFreeCamScript) {
        //have FreeCam handle toggling cameras as we need to wait() 0.1s
        activeFreeCamScript.ResetCams();

        //find the damn thing, find its parents, destroy the child, create a replacement
        VRC.SDK3.Dynamics.PhysBone.PhysBoneGrabHelper[] physbonegrabber = GameObject.FindObjectsOfType < VRC.SDK3.Dynamics.PhysBone.PhysBoneGrabHelper > ();
        var PhysBoneManagerObj = physbonegrabber[0].gameObject;
        Destroy(PhysBoneManagerObj.GetComponent < VRC.SDK3.Dynamics.PhysBone.PhysBoneGrabHelper > ());
        PhysBoneManagerObj.AddComponent < VRC.SDK3.Dynamics.PhysBone.PhysBoneGrabHelper > ();
    }
}