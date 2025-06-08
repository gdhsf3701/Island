using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float fastSpeed = 10f;
    
    [Header("Look")]
    public float lookSpeed = 2f;
    public float maxLookAngle = 80f;
    
    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoomDistance = 1f;
    public float maxZoomDistance = 50f;
    
    [Header("Collision")]
    public float collisionDistance = 0.5f;
    public LayerMask collisionLayers = -1;
    
    private Camera cam;
    private float verticalRotation = 0;
    private bool isLooking = false;
    private bool isPanning = false;
    private Vector3 lastPanPosition;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.None;
    }
    
    void Update()
    {
        HandleMovement();
        HandleLook();
        HandlePan();
        HandleZoom();
    }
    
    void HandleMovement()
    {
        // 속도 결정
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : moveSpeed;
        
        // 이동 방향 계산
        Vector3 move = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.Q)) move -= transform.up;
        if (Input.GetKey(KeyCode.E)) move += transform.up;
        
        // 이동 적용 (콜라이더 체크 포함)
        if (move != Vector3.zero)
        {
            Vector3 newPos = transform.position + move.normalized * speed * Time.deltaTime;
            
            // 간단한 콜라이더 체크
            if (!Physics.CheckSphere(newPos, collisionDistance, collisionLayers))
            {
                transform.position = newPos;
            }
            else
            {
                // 막혔을 때는 뒤로 살짝 이동
                transform.position -= transform.forward * Time.deltaTime;
            }
        }
    }
    
    void HandleLook()
    {
        // 우클릭으로 시점 조작
        if (Input.GetMouseButtonDown(1))
        {
            isLooking = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isLooking = false;
        }
        
        if (isLooking)
        {
            // 마우스 입력
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
            
            // 좌우 회전 (Y축)
            transform.Rotate(0, mouseX, 0);
            
            // 상하 회전 (X축, 제한 있음)
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            
            // 회전 적용
            transform.localRotation = Quaternion.Euler(verticalRotation, transform.localEulerAngles.y, 0);
        }
    }
    
    void HandlePan()
    {
        // 마우스 휠 버튼(중간 버튼)으로 팬 이동
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastPanPosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }
        
        if (isPanning)
        {
            Vector3 deltaPos = Input.mousePosition - lastPanPosition;
            
            // 유니티 씬뷰처럼 마우스 이동 방향과 반대로 움직임
            Vector3 move = Vector3.zero;
            move -= transform.right * deltaPos.x * 0.005f;  // 마우스 오른쪽 → 카메라 왼쪽
            move -= transform.up * deltaPos.y * 0.005f;     // 마우스 위 → 카메라 위
            
            Vector3 newPos = transform.position + move;
            
            // 콜라이더 체크
            if (!Physics.CheckSphere(newPos, collisionDistance, collisionLayers))
            {
                transform.position = newPos;
            }
            
            lastPanPosition = Input.mousePosition;
        }
    }
    
    void HandleZoom()
    {
        // 마우스 휠로 카메라를 앞뒤로 이동
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0)
        {
            Vector3 zoomDirection = transform.forward * scroll * zoomSpeed;
            Vector3 newPos = transform.position + zoomDirection;
            
            // 콜라이더 체크
            if (!Physics.CheckSphere(newPos, collisionDistance, collisionLayers))
            {
                transform.position = newPos;
            }
            else
            {
                // 막혔을 때는 살짝만 이동
                Vector3 safeZoom = transform.forward * scroll * (zoomSpeed * 0.1f);
                Vector3 safePos = transform.position + safeZoom;
                if (!Physics.CheckSphere(safePos, collisionDistance, collisionLayers))
                {
                    transform.position = safePos;
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 충돌 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionDistance);
    }
}