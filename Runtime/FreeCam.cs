using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// A simple free camera to be added to a Unity game object.
/// 
/// Keys:
///	wasd / arrows	- movement
///	q/e 			- up/down (local space)
///	r/f 			- up/down (world space)
///	pageup/pagedown	- up/down (world space)
///	hold shift		- enable fast movement mode
///	right mouse  	- enable free look
///	mouse			- free look / rotation
///     
/// </summary>
public class FreeCam: MonoBehaviour {
    /// <summary>
    /// Normal speed of camera movement.
    /// </summary>
    public float movementSpeed = 2f;

    /// <summary>
    /// Speed of camera movement when shift is held down,
    /// </summary>
    public float fastMovementSpeed = 5f;

    /// <summary>
    /// Sensitivity for free look.
    /// </summary>
    public float freeLookSensitivity = 3f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    public float zoomSensitivity = 2f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    public float fastZoomSensitivity = 5f;

    /// <summary>
    /// Set to true when free looking (on right mouse button).
    /// </summary>
    private bool looking = false;

    public bool persistFromPlayMode = false;
    public bool autoFixPhysBoneHelper = false;
    public bool startFromSceneView = false;
    public bool contactTesting = false;

    private Camera currentCamera;
    private VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver currentContact;

    void Start() {
        currentCamera = GetComponent < Camera > ();
    }

    void Update() {

        var view = currentCamera.ScreenToViewportPoint(Input.mousePosition);
        var isOutside = view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;

        if (!isOutside) {

            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
                transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
                transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
                transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
                transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.E)) {
                transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.Q)) {
                transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp)) {
                transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown)) {
                transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (looking) {
                float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
                float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }

            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis != 0) {
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                transform.position = transform.position + transform.forward * axis * zoomSensitivity;
            }

            if (Input.GetKeyDown(KeyCode.Mouse1)) {
                StartLooking();
            } else if (Input.GetKeyUp(KeyCode.Mouse1)) {
                StopLooking();
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                AttemptClick();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                AttemptReleaseClick();
            }

        }
    }

    void OnDisable() {
        StopLooking();
        AttemptReleaseClick();
    }

    /// <summary>
    /// Enable free looking.
    /// </summary>
    public void StartLooking() {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Disable free looking.
    /// </summary>
    public void StopLooking() {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    //stolen from the internet
    bool hit_sphere(Vector3 center, float radius, Ray r)
    {
        Vector3 oc = r.origin - center;
        float a = Vector3.Dot(r.direction, r.direction);
        float b = 2.0f * Vector3.Dot(oc, r.direction);
        float c = Vector3.Dot(oc,oc) - radius*radius;
        float discriminant = b*b - 4*a*c;
        return (discriminant>0);
    }

    public void AttemptClick()
    {
        if (contactTesting) {
            if (currentContact == null) {
                //get all active contacts in scene 
                VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver[] contactReceivers = GameObject.FindObjectsOfType<VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver>();
                //get mouse raycast
                Ray mouseRay = currentCamera.ScreenPointToRay (Input.mousePosition);
                //loop through every contact and check if one collides
                foreach (VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver contact in contactReceivers)
                {
                    //convert contact local pos to world pos
                    var globalPos = contact.position + contact.gameObject.transform.position;
                    if ( hit_sphere(globalPos, contact.radius, mouseRay) ) {
                        //take control of contact
                        contact.paramValue = 1f;
                        contact.SetParameter(1f);
                        contact.paramAccess.floatVal = 1f;
                        currentContact = contact;
                        Debug.Log("[ADContactTester] Taken paramAccess");
                    }
                }
            }
        }
    }

    public void AttemptReleaseClick()
    {
        if (currentContact != null) {
            //release control
            currentContact.paramValue = 0f;
            currentContact.SetParameter(0f);
            currentContact.paramAccess.floatVal = 0f;
            currentContact = null;
            Debug.Log("[ADContactTester] Released paramAccess");
        }
    }

    //camera priority fix

    public void ResetCams() {
        StartCoroutine(ResetAllCameras());
    }

    IEnumerator ResetAllCameras() {
        //disable all cameras but self
        List < Camera > alteredCamList = new List < Camera > ();
        foreach(Camera cam in Camera.allCameras) {
            if (cam != currentCamera) {
                if (cam.isActiveAndEnabled) {
                    cam.enabled = false;
                    alteredCamList.Add(cam);
                }
            }
        }
        Debug.Log("[FreeCam] Disabled all active cameras but self.");
        //this is to wait for the vrc sdk to select freecam
        yield
        return new WaitForSeconds(0.1f);

        //reenable the other cameras
        foreach(Camera cam in alteredCamList) {
            cam.enabled = true;
        }

        Debug.Log("[FreeCam] Reenabled all modified cameras!");
    }

}