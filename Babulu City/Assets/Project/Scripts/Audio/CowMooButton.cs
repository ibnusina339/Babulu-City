using UnityEngine;
using UnityEngine.UI;

namespace BabuluCity.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class CowMooButton : MonoBehaviour
    {
        [Header("Suara Sapi")]
        [SerializeField] AudioClip mooClip;
        [SerializeField, Range(0f, 1f)] float volume = 1f;

        Button cowButton;
        AudioSource audioSource;

        void Awake()
        {
            cowButton = GetComponent<Button>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            cowButton.onClick.AddListener(PlayMoo);
        }

        void OnDestroy()
        {
            if (cowButton != null)
                cowButton.onClick.RemoveListener(PlayMoo);
        }

        public void PlayMoo()
        {
            if (mooClip == null)
            {
                Debug.LogWarning("Audio Moo belum dipasang pada tombol Cow.", this);
                return;
            }

            // Abaikan klik berikutnya sampai suara moo yang sekarang selesai.
            if (audioSource.isPlaying)
                return;

            audioSource.clip = mooClip;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
