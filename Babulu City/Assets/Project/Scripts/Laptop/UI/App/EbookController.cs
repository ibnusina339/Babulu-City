using System;
using UnityEngine;

public class EbookManager : MonoBehaviour
{
    [SerializeField] private GameObject ebookWindow;

    private void Start()
    {
        ebookWindow.SetActive(false);
    }

    public void OpenEbook()
    {
        ebookWindow.SetActive(true);
    }

    public void CloseEbook()
    {
        ebookWindow.SetActive(false);
    }
}