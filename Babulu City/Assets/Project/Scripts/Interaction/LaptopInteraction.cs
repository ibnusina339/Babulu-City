using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class LaptopInteraction : MonoBehaviour, IInteractable
{
    public GameObject laptopUI;

    [Header("Camera")]
    public CinemachineCamera playerCamera;
    public CinemachineCamera laptopCamera;

    [Header("Player")]
    public PlayerMovement playerMovement;

    [SerializeField] private float cameraTransitionTime = 0.6f;

    private bool opened;
    private Coroutine openRoutine;

    void Start()
    {
        laptopUI.SetActive(false);
    }

    public void Interact()
    {
        if (opened || openRoutine != null)
            return;

        openRoutine = StartCoroutine(OpenLaptop());
    }

    public void Cancel(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        CloseLaptop();
    }

    IEnumerator OpenLaptop()
    {
        opened = true;

        playerMovement.StopMovement();
        playerMovement.enabled = false;

        playerCamera.Priority = 0;
        laptopCamera.Priority = 10;

        yield return new WaitForSeconds(cameraTransitionTime);

        openRoutine = null;

        // Kalau sudah ditutup sebelum transisi selesai, jangan tampilkan UI
        if (!opened)
            yield break;

        laptopUI.SetActive(true);
    }

    public void CloseLaptop()
    {
        // Hentikan coroutine kalau masih berjalan
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        opened = false;

        laptopUI.SetActive(false);

        playerCamera.Priority = 10;
        laptopCamera.Priority = 0;

        playerMovement.enabled = true;
    }
}