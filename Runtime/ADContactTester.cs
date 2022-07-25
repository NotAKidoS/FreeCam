using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone;
using VRC.Dynamics;

public class ADContactTester : MonoBehaviour
{

    private Camera currentCamera;
    private VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver currentContact;

    void Start()
    {
        //exactly what vrchat does by default to detect camera in-editor... bruh
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera.isActiveAndEnabled)
            {
                this.currentCamera = camera;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            AttemptClick();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            AttemptReleaseClick();
        }
    }
    
    void OnDisable()
    {
        AttemptReleaseClick();
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
        if (currentContact == null) {
            //get all active contacts in scene 
            VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver[] contactReceivers = GameObject.FindObjectsOfType<VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver>();
            //get mouse raycast
            Ray mouseRay = this.currentCamera.ScreenPointToRay (Input.mousePosition);
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
} 