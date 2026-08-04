using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.Ending
{
    public sealed class CreditsController : MonoBehaviour
    {
        void Awake()
        {
            Button back = FindAnyObjectByType<Button>();
            if (back == null)
                return;
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(() => SceneManager.LoadScene("StartScreen"));
        }
    }

    static class CreditsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SceneManager.GetActiveScene().name != "Credits" ||
                Object.FindAnyObjectByType<CreditsController>() != null)
                return;
            new GameObject("Credits Controller").AddComponent<CreditsController>();
        }
    }
}
