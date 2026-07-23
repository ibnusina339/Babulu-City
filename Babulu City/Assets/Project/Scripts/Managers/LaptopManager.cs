using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class LaptopManager : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private GameObject desktop;
    [SerializeField] private GameObject ebookWindow;
    
    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInput playerInput;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera playerCam;
    [SerializeField] private CinemachineCamera laptopCam;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    private bool laptopOpened;
    private Coroutine showDesktopCoroutine;

    private void Start()
    {
        desktop.SetActive(false);
        ebookWindow.SetActive(false);

        SetCameraPriority(laptopCam, inactivePriority);
        SetCameraPriority(playerCam, activePriority);
    }

    public void OpenLaptop()
    {
        playerInput.SwitchCurrentActionMap("UI");
        laptopOpened = true;

        playerMovement.StopMovement();

        SetCameraPriority(laptopCam, activePriority);
        SetCameraPriority(playerCam, inactivePriority);

        if (showDesktopCoroutine != null)
            StopCoroutine(showDesktopCoroutine);

        showDesktopCoroutine = StartCoroutine(ShowDesktopWhenBlendDone());
    }

    private IEnumerator ShowDesktopWhenBlendDone()
    {
        // Tunggu satu frame dulu, supaya Brain sempat mendeteksi perubahan Priority
        // dan mulai proses blending sebelum kita cek IsBlending
        yield return null;

        // Selama Brain masih dalam proses blend menuju laptopCam, terus tunggu
        while (cinemachineBrain != null && cinemachineBrain.IsBlending)
        {
            yield return null;
        }

        desktop.SetActive(true);
        showDesktopCoroutine = null;
    }

    public void CloseLaptop()
    {
        playerInput.SwitchCurrentActionMap("Player");
        laptopOpened = false;

        if (showDesktopCoroutine != null)
        {
            StopCoroutine(showDesktopCoroutine);
            showDesktopCoroutine = null;
        }

        desktop.SetActive(false);
        ebookWindow.SetActive(false);

        playerMovement.ResumeMovement();

        SetCameraPriority(laptopCam, inactivePriority);
        SetCameraPriority(playerCam, activePriority);
    }

    public void OpenEbook()
    {
        ebookWindow.SetActive(true);
    }

    public void CloseEbook()
    {
        ebookWindow.SetActive(false);
    }

    public void Cancel(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        HandleClose();
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!laptopOpened)
        {
            OpenLaptop();
            return;
        }

        HandleClose();
    }

    private void HandleClose()
    {
        if (!laptopOpened)
            return;

        if (ebookWindow.activeSelf)
        {
            CloseEbook();
            return;
        }

        CloseLaptop();
    }

    private void SetCameraPriority(CinemachineCamera cam, int priority)
    {
        if (cam == null)
            return;

        cam.Priority = priority;
    }
}