using UnityEngine;
public class LaptopInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private LaptopManager manager;

    public void Interact()
    {
        manager.OpenLaptop();
    }
}