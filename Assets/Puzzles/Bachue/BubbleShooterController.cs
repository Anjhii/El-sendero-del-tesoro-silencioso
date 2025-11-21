using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleShooterController : MonoBehaviour
{
    [Header("Input Actions")] 
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference fireAction;

    [Header("References")]
    public Transform cannonRoot;
    public Transform spawnPoint;
    public LineRenderer aimLine;
    public GameObject bubblePrefab;

    [Header("Settings")]
    public float rotationSpeed = 80f;
    public float maxRotationAngle = 60f;
    public float aimDistance = 3f;
    public float shootForce = 25f;
    public LayerMask wallMask;
    public int maxBounces = 2;

    private float cannonAngle = 0f;
    private GameObject loadedBubble;

    private void OnEnable()
    {
        moveAction?.action.Enable();
        fireAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        fireAction?.action.Disable();
    }

    private void Start()
    {
        LoadBubble();
    }

    private void Update()
    {
        HandleRotation();
        UpdateAimLine();
        HandleShoot();
    }

    private void HandleRotation()
    {
        float x = moveAction.action.ReadValue<Vector2>().x;
        if (Mathf.Abs(x) < 0.01f) return;

        cannonAngle += x * rotationSpeed * Time.deltaTime;

        // Normalización (-180, +180)
        if (cannonAngle > 180f) cannonAngle -= 360f;
        if (cannonAngle < -180f) cannonAngle += 360f;

        cannonAngle = Mathf.Clamp(cannonAngle, -maxRotationAngle, maxRotationAngle);

        if (cannonRoot)
            cannonRoot.localRotation = Quaternion.Euler(0, 0, cannonAngle);
    }

    private void UpdateAimLine()
    {
        if (!aimLine || !spawnPoint) return;

        Vector3 origin = spawnPoint.position;
        
        // 🔥 Calcular dirección basada en la rotación del cañón
        // El ángulo 0° apunta hacia arriba (90° en Unity 2D)
        float angleRad = (90f + cannonAngle) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f).normalized;

        aimLine.positionCount = 1;
        aimLine.SetPosition(0, origin);

        float remaining = aimDistance;
        int index = 1;

        for (int i = 0; i < maxBounces; i++)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, remaining, wallMask))
            {
                aimLine.positionCount = index + 1;
                aimLine.SetPosition(index, hit.point);

                remaining -= hit.distance;
                origin = hit.point;

                dir = Vector3.Reflect(dir, hit.normal);
                dir.z = 0;
                dir.Normalize();

                index++;
            }
            else
            {
                aimLine.positionCount = index + 1;
                aimLine.SetPosition(index, origin + dir * remaining);
                break;
            }
        }
    }

    private void HandleShoot()
    {
        if (fireAction.action.triggered)
            Shoot();
    }

    private void LoadBubble()
    {
        if (!bubblePrefab || !spawnPoint) return;

        loadedBubble = Instantiate(bubblePrefab, spawnPoint.position, Quaternion.identity, spawnPoint);

        if (loadedBubble.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (loadedBubble.TryGetComponent(out Bubble b))
            b.SetRandomColor();
    }

    private void Shoot()
    {
        if (!loadedBubble) return;

        loadedBubble.transform.SetParent(null);

        if (!loadedBubble.TryGetComponent(out Rigidbody rb))
        {
            Destroy(loadedBubble);
            LoadBubble();
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 🔥 Calcular dirección de disparo basada en la rotación del cañón
        float angleRad = (90f + cannonAngle) * Mathf.Deg2Rad;
        Vector3 fireDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f).normalized;

        rb.velocity = fireDirection * shootForce;

        loadedBubble = null;
        LoadBubble();
    }
}