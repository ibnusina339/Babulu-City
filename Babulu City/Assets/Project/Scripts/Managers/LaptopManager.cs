using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LaptopManager : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private GameObject desktop;
    [SerializeField] private GameObject ebookWindow;

    [Header("Icons")]
    [SerializeField] private GameObject EBook;

    [Header("Cursor")]
    [SerializeField] private CursorManager cursorManager;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;

    [SerializeField] private PlayerInput playerInput;

    private bool laptopOpened;

    private void Start()
    {
        desktop.SetActive(false);
        ebookWindow.SetActive(false);
    }

    public void OpenLaptop()
    {
        playerInput.SwitchCurrentActionMap("UI");
        laptopOpened = true;

        desktop.SetActive(true);
        EBook.SetActive(true);

        cursorManager.EnableCursor();

        playerMovement.StopMovement();
    }

    public void CloseLaptop()
    {
        playerInput.SwitchCurrentActionMap("Player");
        laptopOpened = false;

        desktop.SetActive(false);
        ebookWindow.SetActive(false);

        cursorManager.DisableCursor();

        playerMovement.ResumeMovement();
    }

    public void OpenEbook()
    {
        ebookWindow.SetActive(true);
    }

    public void CloseEbook()
    {
        ebookWindow.SetActive(false);
    }

    // Dipakai untuk tombol Esc, di-bind di action map "UI"
    public void Cancel(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        HandleClose();
    }

    // Dipakai untuk tombol E, juga di-bind di action map "UI"
    // Supaya E berfungsi menutup laptop/ebook saat laptop sudah terbuka
    public void Interact(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // Kalau laptop belum terbuka, tombol ini yang membuka laptop
        // (dipanggil dari trigger/interact di dunia game, action map "Player")
        if (!laptopOpened)
        {
            OpenLaptop();
            return;
        }

        // Kalau laptop sudah terbuka, tombol E berfungsi sama seperti Esc
        HandleClose();
    }

    private void HandleClose()
    {
        if (!laptopOpened)
            return;

        // Prioritas: tutup window dulu
        if (ebookWindow.activeSelf)
        {
            CloseEbook();
            return;
        }

        // Kalau tidak ada window yang terbuka, tutup laptop
        CloseLaptop();
    }
}