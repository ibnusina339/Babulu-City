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

            var products = save?.marketplace?.products;
            long revenue = products != null
                ? System.Linq.Enumerable.Sum(products, product => product?.revenue ?? 0L)
                : 0L;
            int unitsSold = products != null
                ? System.Linq.Enumerable.Sum(products, product => product?.sales ?? 0)
                : 0;
            long balance = save?.marketplace?.balance ?? 0L;

            int studyTier = sessions <= 1 ? 0 : sessions <= 3 ? 1 : 2;
            int salesTier = revenue < 500000 ? 0 : revenue <= 1500000 ? 1 : 2;
            // Nomor 1-3 = prestasi rendah, 4-6 = prestasi sedang,
            // 7-9 = prestasi tinggi; di dalamnya diurutkan menurut penjualan.
            int endingNumber = studyTier * 3 + salesTier + 1;

            SetText("Ending number", $"ENDING KE-{endingNumber}");
            SetText("Ending Title", Titles[salesTier, studyTier]);
            SetText("Ending deskripsi", Descriptions[salesTier, studyTier]);
            SetText("Jumlah Bimbel", $"Bimbel: {sessions}/4");

            // Scene ENDING hanya punya satu slot teks untuk angka, jadi saldo
            // akhir dan jumlah produk terjual ikut ditampilkan di sini tanpa
            // menambah GameObject baru.
            SetText("jumlah Pendapatan",
                $"Pendapatan: Rp {revenue:N0}\n" +
                $"Saldo Akhir: Rp {balance:N0}\n" +
                $"Produk Terjual: {unitsSold}");

            // Nama objek di scene adalah "Credit BUtton"; pencarian lama yang
            // hanya mencari "Credit Button" tidak pernah menemukannya sehingga
            // tombolnya tidak berfungsi.
            Button creditButton = FindTransform("Credit BUtton", "Credit Button")
                ?.GetComponent<Button>();
            if (creditButton != null)
            {
                creditButton.onClick.RemoveAllListeners();
                creditButton.onClick.AddListener(() => SceneManager.LoadScene("Credits"));
            }
            else
            {
                Debug.LogWarning("Tombol Credit tidak ditemukan di scene ENDING.", this);
            }
        }

        static void SetText(string objectName, string value)
        {
            TMP_Text text = FindTransform(objectName)?.GetComponent<TMP_Text>();
            if (text != null)
                text.text = value;
        }

        static Transform FindTransform(params string[] acceptedNames)
        {
            foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                foreach (string acceptedName in acceptedNames)
                    if (item.name.Equals(acceptedName, System.StringComparison.OrdinalIgnoreCase))
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
