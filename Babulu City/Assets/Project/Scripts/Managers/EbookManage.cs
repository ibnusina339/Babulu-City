using UnityEngine;
using UnityEngine.UI;

public class EbookManage : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject[] pages;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private int currentPageIndex = 0;

    private void OnEnable()
    {
        currentPageIndex = 0;
        ShowCurrentPage();
    }

    public void NextPage()
    {
        if (currentPageIndex >= pages.Length - 1)
            return;

        currentPageIndex++;
        ShowCurrentPage();
    }

    public void PreviousPage()
    {
        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (previousButton != null)
            previousButton.interactable = currentPageIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentPageIndex < pages.Length - 1;
    }
}