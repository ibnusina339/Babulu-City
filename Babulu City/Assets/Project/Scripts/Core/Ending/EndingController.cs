using BabuluCity.Core;
using BabuluCity.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.Ending
{
    public sealed class EndingController : MonoBehaviour
    {
        // Rows = Penjualan (Rendah, Sedang, Tinggi), Columns = Prestasi (Rendah, Sedang, Tinggi)
        static readonly string[,] Titles =
        {
            { "Masa depan Yang Suram", "Monumen Keindahan Sepi", "Anak OSN Salah Kegiatan" },
            { "Pemuas Kebutuhan Sesaat", "Harmony in Equilibrium", "Sang Penjaga Lentera Ilmu" },
            { "Tahta Cuan Tanpa Mahkota", "Penguasa Algoritma Masa Depan", "CEO Muda Yang Genius" }
        };

        static readonly string[,] Descriptions =
        {
            {
                "Akun terblokir, tautan hangus, dan sisa nilai rapormu tak sanggup menyelamatkanmu.",
                "Sebuah mahakarya visual di balik bio, yang hanya dikunjungi oleh angin malam.",
                "Walau kamu pintar, sayangnya toko kamu sepi. Mungkin sebaiknya kamu fokus akademik saja."
            },
            {
                "Hanya menyalakan mesin jualan ketika dompet menjerit, lalu kembali terlelap.",
                "Di antara riuh lalu lintas internet dan sunyinya ruang ujian, kamu menemukan kedamaian.",
                "Materi PDF buatanmu menyinari jalan puluhan siswa yang tersesat di malam ujian."
            },
            {
                "Puluhan transaksi mengalir di biolink-mu setiap malam, tapi raportmu berdarah (nilai merah).",
                "Setiap tautan yang kamu bagikan adalah perintah bagi pasar untuk bertindak.",
                "Menguasai pasar digital sebelum lulus SMA, menakhlukkan universitas impian tanpa cela."
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
            int salesTier = revenue < 500000 ? 0 : revenue < 1500000 ? 1 : 2;
            int endingNumber = salesTier * 3 + studyTier + 1;

            SetText("Ending number", $"ENDING KE-{endingNumber}");
            SetText("Ending Title", Titles[salesTier, studyTier]);
            SetText("Ending deskripsi", Descriptions[salesTier, studyTier]);
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
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            if (SceneManager.GetActiveScene().name != "ENDING" ||
                Object.FindAnyObjectByType<EndingController>() != null)
                return;
            new GameObject("Ending Controller").AddComponent<EndingController>();
        }
    }
}
