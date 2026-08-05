using System;
using UnityEngine.SceneManagement;

namespace BabuluCity.Core
{
    /// <summary>
    /// RuntimeInitializeOnLoadMethod hanya berjalan sekali saat aplikasi mulai,
    /// bukan setiap kali scene dimuat. Komponen yang dipasang lewat bootstrap
    /// karena itu ikut hilang saat scene dimuat ulang, misalnya ketika pemain
    /// kembali dari StartScreen ke Main untuk kedua kalinya.
    ///
    /// Helper ini menjalankan installer sekali saat aplikasi mulai dan sekali
    /// lagi setiap scene selesai dimuat. Installer wajib idempotent: aman
    /// dipanggil berkali-kali dan berhenti sendiri bila komponennya sudah ada.
    /// </summary>
    public static class SceneBootstrap
    {
        public static void RunOnEverySceneLoad(Action install)
        {
            if (install == null)
                return;

            SceneManager.sceneLoaded += (loadedScene, loadMode) => install();
            install();
        }
    }
}
