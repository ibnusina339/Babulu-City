using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private IInteractable currentInteractable;
    private bool isInteracting;
    
    

    public void Interact(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (isInteracting)
            return;

        
        if (currentInteractable == null)
            return;

        isInteracting = true;
        currentInteractable.Interact();
        isInteracting = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        currentInteractable = other.GetComponent<IInteractable>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<IInteractable>() == currentInteractable)
            currentInteractable = null;
    }
}