using UnityEngine;

public class ManageComponents : MonoBehaviour
{
    public GameObject welcomeDialog;          // Prefab or instance of the welcome dialog
    public float distanceFromUser = 0.5f;      // Desired distance in front of user
    public float verticalOffset = 0.0f;        // Additional height offset
    public float followSmoothing = 8f;         // Position smoothing speed
    public float rotateSmoothing = 12f;        // Rotation smoothing speed
    public bool lockToHorizontalPlane = true;  // Ignore camera pitch for placement

    private Transform _camera;
    private Vector3 _targetPos;

    void Start()
    {
        if (Camera.main != null)
        {
            _camera = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("ManageComponents: No main camera tagged. Dialog follow disabled.");
        }
    }

    void LateUpdate()
    {
        if (_camera == null || welcomeDialog == null) return;

        // Determine forward vector (optionally flattened to horizontal plane)
        Vector3 forward = _camera.forward;
        if (lockToHorizontalPlane)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = _camera.rotation * Vector3.forward; // fallback
        }
        forward.Normalize();

        // Target position: camera position + forward * distance + optional vertical offset
        _targetPos = _camera.position + forward * distanceFromUser;
        _targetPos.y += verticalOffset;

        // Smooth movement
        welcomeDialog.transform.position = Vector3.Lerp(welcomeDialog.transform.position, _targetPos, Time.deltaTime * followSmoothing);

        // Smooth rotation to face the camera
        Vector3 dirToCam = _camera.position - welcomeDialog.transform.position;
        if (dirToCam.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToCam.normalized, Vector3.up);
            welcomeDialog.transform.rotation = Quaternion.Slerp(welcomeDialog.transform.rotation, targetRot, Time.deltaTime * rotateSmoothing);
        }
    }
}
