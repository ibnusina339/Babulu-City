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

    void Start()
    {
        laptopUI.SetActive(false);
    }
    
    public void Interact()
    {
        Debug.Log("Interact dipanggil!");
        if(!opened)
        {
            StartCoroutine(OpenLaptop());
        }
    }
    public void Cancel(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (opened)
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

        laptopUI.SetActive(true);
    }

    public void CloseLaptop()
    {
        laptopUI.SetActive(false);
        
        playerCamera.Priority = 10;
        laptopCamera.Priority = 0;

        playerMovement.enabled = true;

        opened = false;
    }
}