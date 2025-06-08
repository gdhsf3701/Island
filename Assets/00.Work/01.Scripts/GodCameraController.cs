using UnityEngine;

public class SmartCollisionCamera : MonoBehaviour
{
    public Transform pivot;
    public Camera cam;
    public LayerMask collisionMask;  // 벽/물체용 콜라이더 레이어

    [Header("Zoom")]
    public float zoomSpeed = 200f;
    public float minDistance = 5f, maxDistance = 50f;

    [Header("Rotation")]
    public float rotationSpeed = 200f, pitchSpeed = 150f;
    public float minPitch = -60f, maxPitch = 80f;

    [Header("Pan")]
    public float panSpeed = 0.5f;

    private float distance, yaw, pitch;
    private float wheelVelocity;
    private Vector3 lastMousePos;

    void Start()
    {
        cam = cam ?? Camera.main;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        distance = Vector3.Distance(transform.position, pivot.position);
        lastMousePos = Input.mousePosition;
    }

    void LateUpdate()
    {
        HandleZoom();
        HandleRotation();
        HandleMMBDragPan();
        ApplyTransformWithCollision();

        lastMousePos = Input.mousePosition;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f)
            wheelVelocity += scroll * zoomSpeed;

        wheelVelocity = Mathf.MoveTowards(wheelVelocity, 0f, Time.deltaTime * zoomSpeed * 10f);
        distance = Mathf.Clamp(distance - wheelVelocity * Time.deltaTime, minDistance, maxDistance);
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * pitchSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void HandleMMBDragPan()
    {
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            Vector3 offset = cam.ScreenToViewportPoint(delta);
            Vector3 move = new Vector3(-offset.x * panSpeed, -offset.y * panSpeed, 0f);
            Vector3 worldMove = transform.TransformDirection(move);
            pivot.position += worldMove;
        }
    }

    void ApplyTransformWithCollision()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredWorldPos = pivot.position + rot * Vector3.back * distance;

        // 카메라와 pivot 사이에 충돌체가 있다면 카메라를 충돌지점 바로 앞에 배치
        if (Physics.Linecast(pivot.position, desiredWorldPos, out RaycastHit hit, collisionMask))
        {
            transform.position = hit.point + hit.normal * 0.1f;
        }
        else
        {
            transform.position = desiredWorldPos;
        }

        transform.rotation = rot;
    }
}
