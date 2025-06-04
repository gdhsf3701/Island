using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float distance = 15f;
    public float zoomSpeed = 20f;
    public float rotationSpeed = 5f;
    public float panSpeed = 0.1f;
    public float minY = -7f;
    public float maxY = 100f;

    private Vector3 lastMousePos;

    void LateUpdate()
    {
        // 회전 (우클릭)
        if (Input.GetMouseButton(1))
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = -Input.GetAxis("Mouse Y") * rotationSpeed;
            transform.RotateAround(target.position, Vector3.up, rotX);
            transform.RotateAround(target.position, transform.right, rotY);
        }

        // 이동 (휠 클릭)
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            Vector3 move = -transform.right * delta.x - transform.up * delta.y;
            transform.position += move * panSpeed;
            target.position += move * panSpeed;
        }

        // 줌 (휠)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Vector3 zoom = transform.forward * scroll * zoomSpeed * Time.deltaTime;
        Vector3 nextPos = transform.position + zoom;
        if (nextPos.y > minY)
            transform.position = nextPos;

        lastMousePos = Input.mousePosition;
    }
}