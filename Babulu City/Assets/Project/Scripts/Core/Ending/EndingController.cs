using BabuluCity.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.Ending
{
    public sealed class EndingController : MonoBehaviour
    {
        static readonly string[,] Titles =
        {
            { "Langkah Pertama", "Mulai Dikenal", "Pedagang Gigih" },
            { "Pelajar Konsisten", "Seimbang dan Bertumbuh", "Strategi Cemerlang" },
            { "Ilmu sebagai Modal", "Kreator Berprestasi", "Bintang Digital Babulu" }
        };

        static readonly string[,] Descriptions =
        {
            {
                "Kamu baru memulai perjalanan. Belajar dan penjualan masih rendah, tetapi pengalaman pertama sudah menjadi modal penting.",
                "Walau waktu belajar belum maksimal, tokomu mulai menemukan pembeli dan arah bisnis yang menjanjikan.",
                "Penjualanmu sangat kuat. Tantangan berikutnya adalah menyeimbangkan kesuksesan bisnis dengan prestasi belajar."
            },
            {
                "Kamu cukup konsisten mengikuti bimbel, tetapi strategi toko masih perlu diasah agar hasilnya ikut meningkat.",
                "Prestasi dan bisnis berkembang bersama. Kamu berhasil menjaga ritme yang sehat selama satu minggu.",
                "Belajar yang konsisten mendukung keputusan bisnis yang tajam dan menghasilkan penjualan tinggi."
            },
            {
                "Prestasi belajarmu sangat baik. Sekarang saatnya memakai pengetahuan itu untuk memperkuat strategi penjualan.",
                "Kamu menuntaskan seluruh bimbel dan membangun toko yang sehat. Fondasi masa depanmu sangat kuat.",
                "Kamu menguasai pembelajaran sekaligus bisnis digital. Inilah pencapaian tertinggi perjalananmu di Babulu City."
            }
        };

        void Awake()
        {
            BabuluGameSaveData save = GameSaveManager.ReadSave();
            int sessions = Mathf.Clamp(save?.completedStudySessions ?? 0, 0, 4);
            long revenue = save?.marketplace?.products != null
                ? System.Linq.Enumerable.Sum(save.marketplace.products, product => product?.revenue ?? 0L)
                : 0L;

            int studyTier = sessions <= 1 ? 0 : sessions <= 3 ? 1 : 2;
            int salesTier = revenue < 500000 ? 0 : revenue <= 1500000 ? 1 : 2;
            int endingNumber = studyTier * 3 + salesTier + 1;

            SetText("Ending number", $"ENDING KE-{endingNumber}");
            SetText("Ending Title", Titles[studyTier, salesTier]);
            SetText("Ending deskripsi", Descriptions[studyTier, salesTier]);
            SetText("Jumlah Bimbel", $"TOTAL BIMBEL  {sessions}/4");
            SetText("jumlah Pendapatan", $"TOTAL PENDAPATAN  Rp {revenue:N0}");

            Button creditButton = FindTransform("Credit Button")?.GetComponent<Button>();
            if (creditButton != null)
            {
                creditButton.onClick.RemoveAllListeners();
                creditButton.onClick.AddListener(() => SceneManager.LoadScene("Credits"));
            }
        }

        static void SetText(string objectName, string value)
        {
            TMP_Text text = FindTransform(objectName)?.GetComponent<TMP_Text>();
            if (text != null)
                text.text = value;
        }

        static Transform FindTransform(string objectName)
        {
            foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (item.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                    return item;
            return null;
        }
    }

    static class EndingBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SceneManager.GetActiveScene().name != "ENDING" ||
                Object.FindAnyObjectByType<EndingController>() != null)
                return;
            new GameObject("Ending Controller").AddComponent<EndingController>();
        }
    }
}
