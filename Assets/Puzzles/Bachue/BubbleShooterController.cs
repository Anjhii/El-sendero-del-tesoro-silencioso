using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleShooterController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference fireAction;

    [Header("Shooting Configuration")]
    [SerializeField] private Transform cannon;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private float shootForce = 0.8f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float maxRotationAngle = 45f;

    [Header("Optional Audio")]
    [SerializeField] private AudioClip shootSound;
    private AudioSource audioSource;

    private float currentRotation = 0f;
    private bool canShoot = true;
    private float shootCooldown = 0.3f;
    private float lastShootTime;

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (fireAction != null) fireAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (fireAction != null) fireAction.action.Disable();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && shootSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        HandleRotation();
        HandleShooting();
    }

    private void HandleRotation()
    {
        if (moveAction == null || cannon == null) return;

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float horizontalInput = moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float targetRotation = currentRotation + (horizontalInput * rotationSpeed * Time.deltaTime);
            currentRotation = Mathf.Clamp(targetRotation, -maxRotationAngle, maxRotationAngle);
            
            cannon.localRotation = Quaternion.Euler(0f, currentRotation, 0f);
        }
    }

    private void HandleShooting()
    {
        if (fireAction == null || !canShoot) return;

        if (Time.time - lastShootTime < shootCooldown) return;

        if (fireAction.action.triggered || fireAction.action.ReadValue<float>() > 0.5f)
        {
            ShootBubble();
            lastShootTime = Time.time;
        }
    }

    private void ShootBubble()
    {
        if (bubblePrefab == null || spawnPoint == null) return;

        GameObject bubble = Instantiate(bubblePrefab, spawnPoint.position, spawnPoint.rotation);
        
        Rigidbody rb = bubble.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(spawnPoint.forward * shootForce, ForceMode.Impulse);
        }

        Bubble bubbleScript = bubble.GetComponent<Bubble>();
        if (bubbleScript != null)
        {
            bubbleScript.SetRandomColor();
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }
}