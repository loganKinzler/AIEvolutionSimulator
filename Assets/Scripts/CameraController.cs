using System;
using Unity.VisualScripting;
using UnityEngine;//
using UnityEngine.InputSystem;//
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Interactions;//

public class CameraController : MonoBehaviour
{   
    // references
    public GameObject managerObject;
    private SceneManager managerScript;
    public GameObject cameraHolder;

    // inputs
    private bool rightButtonIsDown = false;


    // camera
    [Header("Camera")]
    [SerializeField] private float cameraDistance = 3f;
    [SerializeField] private float cameraScrollDelta = 1f;
    [SerializeField] private float cameraDragSpeed = 2f;
    private GameObject playerCamera;
    
    // limits
    private (float, float) camDistLimits = (4f, 8f);
    private (float, float) camPhiLimits = (10f, 80f);

    // data
    private int actorIndex = 0;


    void Start()
    {
        managerScript = managerObject.GetComponent<SceneManager>();
        playerCamera = cameraHolder.GetComponentsInChildren<Transform>()[0].gameObject;

        SetupCamera();
    }


    // CAMERA MOVEMENT
    private void SetupCamera() {
        playerCamera.transform.localPosition = cameraDistance*Vector3.forward;

        playerCamera.transform.localPosition = Mathf.Clamp(
            cameraDistance, camDistLimits.Item1,camDistLimits.Item2)*Vector3.back;
    }

    public void MoveCamera() {
        cameraHolder.transform.parent = managerScript.GetActor(actorIndex).GetBody().transform;
    }


    // INPUT SYSTEM
    public void Next(InputAction.CallbackContext context) {
        if (!context.started) return;
        actorIndex = managerScript.GetNextIndex(actorIndex, true);
        MoveCamera();
    }

    public void Previous(InputAction.CallbackContext context) {
        if (!context.started) return;
        actorIndex = managerScript.GetNextIndex(actorIndex, false);
        MoveCamera();
    }

    public void Zoom(InputAction.CallbackContext context) {
        if (!context.started) return;

        cameraDistance = Mathf.Clamp(cameraDistance - context.ReadValue<Vector2>().y*cameraScrollDelta,
            camDistLimits.Item1, camDistLimits.Item2);

        playerCamera.transform.localPosition = cameraDistance*Vector3.back;
    }

    public void Look(InputAction.CallbackContext context) {
        if (!rightButtonIsDown) return;

        Vector2 dragDelta = context.ReadValue<Vector2>() * cameraDragSpeed;
        Vector3 currentRotation = cameraHolder.transform.rotation.eulerAngles;

        playerCamera.transform.localPosition = Vector3.zero;

        cameraHolder.transform.rotation = Quaternion.Euler(
            Mathf.Clamp(currentRotation.x + dragDelta.y,
                camPhiLimits.Item1, camPhiLimits.Item2),
            currentRotation.y - dragDelta.x,
            0
        );

        playerCamera.transform.localPosition = cameraDistance*Vector3.back;
    }

    public void RightButton(InputAction.CallbackContext context) {
        if (context.started) rightButtonIsDown = true;
        if (context.canceled) rightButtonIsDown = false;
    }
}
