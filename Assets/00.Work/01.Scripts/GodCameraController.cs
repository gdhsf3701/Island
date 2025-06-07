using UnityEngine;

public class AllInOneCameraController : MonoBehaviour
{
    public Transform pivot;
    public Camera cam;

    [Header("Zoom")]
    public float zoomSpeed = 200f;
    public float minDistance = 5f, maxDistance = 50f;

    [Header("Rotation & Pitch")]
    public float rotationSpeed = 200f, pitchSpeed = 150f;
    public float minPitch = -60f, maxPitch = 80f;

    [Header("Panning")]
    public float panSpeed = 20f;

    private float distance, yaw, pitch;
    private float wheelVelocity;
    private Vector3 lastMousePos;

    void Start()
    {
        cam = cam ?? Camera.main;
        Vector3 dir = transform.position - pivot.position;
        distance = dir.magnitude;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        lastMousePos = Input.mousePosition;
    }

    void LateUpdate()
    {
        HandleZoom();             // 휠 스크롤 줌
        HandleRotationPitch();    // 우클릭 회전 + 피치
        HandleLeftDragPan();      // 좌클릭 드래그 패닝 ⇨ 핵심 기능
        ApplyCameraTransform();

        lastMousePos = Input.mousePosition;
    }

    // 좌버튼 드래그 → 반대방향으로 카메라 이동
    void HandleLeftDragPan()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            Vector3 pos1 = cam.ScreenToViewportPoint(lastMousePos);
            Vector3 pos2 = cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = pos2 - pos1;

            Vector3 move = new Vector3(-direction.x * panSpeed, -direction.y * panSpeed, 0f);
            transform.Translate(move, Space.Self);
            pivot.Translate(move, Space.Self);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f) wheelVelocity += scroll * zoomSpeed;

        wheelVelocity = Mathf.MoveTowards(wheelVelocity, 0f, Time.deltaTime * zoomSpeed * 10f);
        distance = Mathf.Clamp(distance - wheelVelocity * Time.deltaTime, minDistance, maxDistance);
    }

    void HandleRotationPitch()
    {
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * pitchSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void ApplyCameraTransform()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = pivot.position + rot * Vector3.back * distance;
        transform.position = desiredPos;
        transform.rotation = rot;
    }
}
