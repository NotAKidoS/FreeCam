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
                if (freeCamScript.shouldPersist) {
                    persistPos = freeCam.transform.position;
                    persistRot = freeCam.transform.rotation;
                }
                break;
            case PlayModeStateChange.EnteredEditMode:
                if (freeCamScript.shouldPersist) {
                    freeCam.transform.position = persistPos;
                    freeCam.transform.rotation = persistRot;
                }
                break;
            case PlayModeStateChange.ExitingEditMode:
                if (freeCamScript.useSceneViewCam) {
                    SceneView sceneCam = SceneView.lastActiveSceneView;
                    freeCam.transform.position = sceneCam.camera.transform.position;
                    freeCam.transform.rotation = sceneCam.camera.transform.rotation;
                }
                break;
            case PlayModeStateChange.EnteredPlayMode:
                if (freeCamScript.autoFixCamPriority) {
                    FreeCamEditor.FixCamPriority(freeCamScript);
                }
                break;
            }
        }
    }
}

[CustomEditor(typeof(FreeCam))]
public class FreeCamEditor: Editor {

    //default freecam prefab
    public GameObject freeCamPrefab;

    //input
    private GameObject activeFreeCam;

    //vars
    bool freeCamEnabled = false;
    bool useADContactTester = false;
    bool shouldPersist = false;
    bool autoFixCamPriority = false;
    bool useSceneViewCam = false;

    //freecam config
    public float movementSpeed = 2f;
    public float fastMovementSpeed = 5f;
    public float freeLookSensitivity = 3f;
    public float zoomSensitivity = 2f;
    public float fastZoomSensitivity = 5f;

    public override void OnInspectorGUI() {

        GUIStyle box = GUI.skin.GetStyle("box");

        using(new EditorGUI.DisabledScope(freeCamEnabled == false)) {
            EditorGUIUtility.labelWidth = 100;
            activeFreeCam = EditorGUILayout.ObjectField("In Scene:", activeFreeCam, typeof (GameObject), true) as GameObject;
            EditorGUIUtility.labelWidth = 0;
        }

        //add freeCam to scene
        using(new EditorGUI.DisabledScope(activeFreeCam != null)) {
            if (GUILayout.Button("Add FreeCam to Scene")) {
                if (!CheckForExisting()) {
                    activeFreeCam = Instantiate(freeCamPrefab, new Vector3(0f, 1f, 1.5f), new Quaternion(0f, 180f, 0f, 0f));
                    activeFreeCam.transform.SetSiblingIndex(0);
                    activeFreeCam.name = "FreeCam";
                }
            }
        }

        GUILayout.Label("FreeCam Configuration", "boldlabel");
        using(new GUILayout.VerticalScope(box)) {
            movementSpeed = EditorGUILayout.FloatField("Movement Speed", movementSpeed);
            fastMovementSpeed = EditorGUILayout.FloatField("Fast Movement Speed", fastMovementSpeed);
            freeLookSensitivity = EditorGUILayout.FloatField("Free Look Sensitivity", freeLookSensitivity);
            zoomSensitivity = EditorGUILayout.FloatField("Zoom Sensitivity", zoomSensitivity);
            fastZoomSensitivity = EditorGUILayout.FloatField("Fast Zoom Sensitivity", fastZoomSensitivity);
        }

        GUILayout.Label("PlayMode Persistance", "boldlabel");
        using(new GUILayout.VerticalScope(box)) {
            //location persist button
            useSceneViewCam = EditorGUILayout.Toggle("Start From SceneView", useSceneViewCam);
            shouldPersist = EditorGUILayout.Toggle("Persist From PlayMode", shouldPersist);
        }

        GUILayout.Label("Avatar Dynamics", "boldlabel");
        using(new GUILayout.VerticalScope(box)) {
            //ADContactTester button
            useADContactTester = EditorGUILayout.Toggle("Basic Contact Testing", useADContactTester);
            EditorGUILayout.HelpBox("Allows you to activate contact receivers via mouse click from GameView! No proximity or capsule support as of yet.", MessageType.Info);
            autoFixCamPriority = EditorGUILayout.Toggle("Autofix PhysBone Helper", autoFixCamPriority);
            EditorGUILayout.HelpBox("Temporarily disables all other cameras while regenerating PhysBoneGrabHelper to guarantee FreeCam takes priority.", MessageType.Info);
        }

        //sync all changes to FreeCam script
        if (GUI.changed) {
            FreeCam activeFreeCamScript = activeFreeCam.GetComponent < FreeCam > ();
            ADContactTester activeContactTester = activeFreeCam.GetComponent < ADContactTester > ();
            activeFreeCamScript.movementSpeed = movementSpeed;
            activeFreeCamScript.fastMovementSpeed = fastMovementSpeed;
            activeFreeCamScript.freeLookSensitivity = freeLookSensitivity;
            activeFreeCamScript.zoomSensitivity = zoomSensitivity;
            activeFreeCamScript.fastZoomSensitivity = fastZoomSensitivity;
            activeFreeCamScript.shouldPersist = shouldPersist;
            activeFreeCamScript.autoFixCamPriority = autoFixCamPriority;
            activeFreeCamScript.useSceneViewCam = useSceneViewCam;
            activeContactTester.enabled = useADContactTester;
        }
    }

    private bool CheckForExisting() {
        FreeCam[] freecamscripts = GameObject.FindObjectsOfType < FreeCam > ();
        if (freecamscripts.Count() == 1) {
            activeFreeCam = freecamscripts[0].gameObject;
            Debug.Log("[FreeCamEditor] Found existing FreeCam script in Scene!");
            return true;
        }
        return false;
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