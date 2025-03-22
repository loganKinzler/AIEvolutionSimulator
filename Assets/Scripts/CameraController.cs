using UnityEngine;//
using UnityEngine.InputSystem;//
using UnityEngine.UI;//
using TMPro;//

public class CameraController : MonoBehaviour
{   
    // references
    public GameObject managerObject;
    private SceneManager managerScript;

    public GameObject cameraHolder;
    public GameObject idleHolder;
    public GameObject nameLabel;

    // inputs
    private bool rightButtonIsDown = false;


    // camera
    [Header("Camera")]
    [SerializeField] private float cameraScrollSpeed = 1f;
    [SerializeField] private float cameraDragSpeed = 2f;
    [SerializeField] private float cameraMoveSpeed = 2f;
    [SerializeField] private float cameraIdleMoveSpeed = 0.5f;
    
    private GameObject playerCamera;
    private float cameraDistance = 3f;
    private float idleDistance = 12f;
    private Vector2 idleVelocity = new Vector2(0,0);

    // limits
    [Header("Spectating Limits")]
    [SerializeField] private (float, float) camDistLimits = (1f, 4f);
    [SerializeField] private (float, float) camPhiLimits = (10f, 80f);
    
    [Header("Idling Limits")]
    [SerializeField] private (float, float) camIdleZoomLimits = (5f,24f);
    [SerializeField] private Vector2 camIdleMoveLimits = new Vector2(12,12);


    // data
    private int actorIndex = 0;
    private bool spectating = false;

    void Start()
    {
        managerScript = managerObject.GetComponent<SceneManager>();
        playerCamera = cameraHolder.transform.GetChild(0).gameObject;

        SetupCamera();
        IdleCamera();
    }

    void Update()
    {
        if (spectating) return;

        Vector3 deltaIdlePos = new Vector3(idleVelocity.x, 0, idleVelocity.y) * cameraIdleMoveSpeed * Time.deltaTime;
        idleHolder.transform.localPosition += deltaIdlePos;
        Vector2 idlePlaneLimits = GetIdleMoveLimits() * idleHolder.transform.localScale;


        idleHolder.transform.localPosition = new Vector3(
            Mathf.Clamp(idleHolder.transform.localPosition.x,
                -idlePlaneLimits.x, idlePlaneLimits.x),

            Mathf.Clamp(idleHolder.transform.localPosition.y,
                camIdleZoomLimits.Item1, camIdleZoomLimits.Item2),

            Mathf.Clamp(idleHolder.transform.localPosition.z,
                -idlePlaneLimits.y, idlePlaneLimits.y)
        );
    }


    // CAMERA MOVEMENT
    private void SetupCamera() {
        managerScript.transform.localPosition = Vector3.zero;
        playerCamera.transform.localPosition = cameraDistance*Vector3.back;

        playerCamera.transform.localPosition = Mathf.Clamp(
            cameraDistance, camDistLimits.Item1,camDistLimits.Item2)*Vector3.back;
    }

    public void MoveCamera() {
        nameLabel.GetComponent<TextMeshProUGUI>().text = managerScript.GetActor(actorIndex).gameObject.name;
        
        playerCamera.transform.parent = cameraHolder.transform;
        playerCamera.transform.localRotation = Quaternion.Euler(Vector3.zero);

        cameraHolder.transform.parent = managerScript.GetActor(actorIndex).GetBody().transform;
        cameraHolder.transform.localPosition = Vector3.zero;
        
        playerCamera.transform.localPosition = cameraDistance*Vector3.back;
    }

    public void IdleCamera() {
        playerCamera.transform.position = idleHolder.transform.position;
        playerCamera.transform.rotation = idleHolder.transform.rotation;
        playerCamera.transform.parent = idleHolder.transform;
    }

    public Vector2 GetIdleMoveLimits() {
        return camIdleMoveLimits * (1 - Mathf.InverseLerp(
            camIdleZoomLimits.Item1 / idleHolder.transform.parent.localScale.y,
            camIdleZoomLimits.Item2 / idleHolder.transform.parent.localScale.y,
            idleHolder.transform.localPosition.y
        ));
    }

    // INPUT SYSTEM
    public void Next(InputAction.CallbackContext context) {
        if (!context.started) return;
        spectating = true;

        nameLabel.transform.parent.gameObject.SetActive(true);
        actorIndex = managerScript.GetNextIndex(actorIndex, true);
        MoveCamera();
    }

    public void Previous(InputAction.CallbackContext context) {
        if (!context.started) return;
        spectating = true;
        
        nameLabel.transform.parent.gameObject.SetActive(true);
        actorIndex = managerScript.GetNextIndex(actorIndex, false);
        MoveCamera();
    }

    public void Cancel(InputAction.CallbackContext context) {
        if (!context.started) return;
        spectating = false;
        idleVelocity = Vector2.zero;

        nameLabel.transform.parent.gameObject.SetActive(false);

        IdleCamera();
    }

    public void Move(InputAction.CallbackContext context) {
        if (spectating) return;
        idleVelocity = context.ReadValue<Vector2>();
    }

    private void SpectatorZoom(float zoom) {
        cameraDistance = Mathf.Clamp(cameraDistance - zoom * cameraScrollSpeed * Time.deltaTime,
            camDistLimits.Item1, camDistLimits.Item2);

        playerCamera.transform.localPosition = cameraDistance*Vector3.back;

    }

    private void IdleZoom(float zoom) {
        idleDistance = Mathf.Clamp(idleDistance - zoom * cameraScrollSpeed * Time.deltaTime,
            camIdleZoomLimits.Item1 / idleHolder.transform.parent.localScale.y,
            camIdleZoomLimits.Item2 / idleHolder.transform.parent.localScale.y);

        idleHolder.transform.localPosition = new Vector3(
            idleHolder.transform.localPosition.x,
            idleDistance,
            idleHolder.transform.localPosition.z
        );
    }

    public void Zoom(InputAction.CallbackContext context) {
        if (!context.started) return;
        float contextY = context.ReadValue<Vector2>().y;

        if (spectating) SpectatorZoom(contextY);
        else IdleZoom(contextY);
    }

    public void Look(InputAction.CallbackContext context) {
        if (!rightButtonIsDown || !spectating) return;

        Vector2 dragDelta = context.ReadValue<Vector2>() * cameraDragSpeed * Time.deltaTime;
        Vector3 currentRotation = cameraHolder.transform.rotation.eulerAngles;

        cameraHolder.transform.rotation = Quaternion.Euler(
            Mathf.Clamp(currentRotation.x - dragDelta.y,
                camPhiLimits.Item1, camPhiLimits.Item2),
            currentRotation.y + dragDelta.x,
            0
        );

        playerCamera.transform.localPosition = cameraDistance*Vector3.back;
    }

    public void RightButton(InputAction.CallbackContext context) {
        if (context.started) rightButtonIsDown = true;
        if (context.canceled) rightButtonIsDown = false;
    }
}
