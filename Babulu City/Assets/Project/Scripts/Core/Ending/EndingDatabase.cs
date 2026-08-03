using System.Collections.Generic;
using UnityEngine;

public class EndingDatabase : MonoBehaviour
{
    public List<EndingData> endings = new List<EndingData>();

    void Awake()
    {
        endings = new List<EndingData>
        {
            new EndingData {
                title = "Tahta Cuan Tanpa Mahkota",
                subtitle = "Ratu Kapitalis Koridor Sekolah",
                description = "Ratusan transaksi mengalir di biolink-mu setiap malam, tapi ruang kelas tak lagi mengenali namamu. Kamu membangun kerajaan bisnis digital beromset jutaan rupiah dari balik meja kelas. Namun, saat rekor saldo dompet digitalmu berada di puncak, lembar ujianmu dipenuhi garis merah. Pasanganmu menikmati hidup mewah darimu, tetapi ia merindukan sosokmu yang dulu selalu duduk di sampingnya untuk belajar bersama.",
                penjualan = StatLevel.Tinggi, prestasi = StatLevel.Rendah
            },
            new EndingData {
                title = "Penguasa Algoritma Masa Depan",
                subtitle = "Fenomena Sosial Media Sekolah",
                description = "Setiap tautan yang kamu bagikan adalah perintah bagi pasar untuk bertindak. Kamu berhasil menjinakkan media sosial dan platform social commerce. Satu unggahanmu mampu membuat stok produk habis dalam hitungan detik. Tanpa mengorbankan nilai akademikmu secara fatal, kamu dan pasanganmu menjelma menjadi pasangan paling berpengaruh yang ditakuti sekaligus dikagumi seisi sekolah.",
                penjualan = StatLevel.Tinggi, prestasi = StatLevel.Sedang
            },
            new EndingData {
                title = "Sang Arsitek Takdir Digital",
                subtitle = "Mahakarya Dua Dunia",
                description = "Menguasai pasar digital sebelum lulus SMA, menakhlukkan universitas impian tanpa cela. Sebuah kejayaan mutlak! Kamu membuktikan bahwa kekayaan dari social commerce dan predikat Bintang Pelajar bisa digenggam secara bersamaan. Kamu memimpin ekosistem bisnis digital milikmu sendiri sambil melangkah mantap menuju PTN favorit lewat jalur undangan. Kamu dan pasanganmu diabadikan dalam sejarah sekolah sebagai legenda hidup!",
                penjualan = StatLevel.Tinggi, prestasi = StatLevel.Tinggi
            },
            new EndingData {
                title = "Pemuas Kebutuhan Sesaat",
                subtitle = "Pengembara Angot-Angotan",
                description = "Hanya menyalakan mesin jualan ketika dompet menjerit, lalu kembali terlelap. Kamu menyebarkan link di bio hanya ketika butuh uang jajan tambahan untuk akhir pekan. Tanpa ambisi besar di dunia bisnis maupun akademik, kamu dan pasanganmu menjalani hari-hari SMA dengan santai dan bebas beban, meski masa depan masih menyisakan banyak tanda tanya.",
                penjualan = StatLevel.Sedang, prestasi = StatLevel.Rendah
            },
            new EndingData {
                title = "Harmony in Equilibrium",
                subtitle = "Penjaga Keseimbangan Masa Muda",
                description = "Di antara riuh lalu lintas internet dan sunyinya ruang ujian, kamu menemukan kedamaian. Kamu tidak mengejar status viral atau gelar juara umum. Pemasukan dari jualan jasa dan produk digital milikmu mengalir konsisten untuk membiayai kencan dan hobi, sementara nilai akademikmu tetap terjaga. Ini adalah kisah tentang masa remaja yang paling ideal, manis, dan harmonis bersama pasangan tercinta.",
                penjualan = StatLevel.Sedang, prestasi = StatLevel.Sedang
            },
            new EndingData {
                title = "Sang Penjaga Lentera Ilmu",
                subtitle = "Sang Cendekia Digital",
                description = "Materi PDF buatanmu menyinari jalan puluhan siswa yang tersesat di malam ujian. Kamu memanfaatkan kepintaranmu untuk merangkum seluruh kurikulum ke dalam dokumen digital yang kamu jual di Lynk.id. Keuntungan finansial bukanlah tujuan utamamu, melainkan pembuktian kualitas diri. Kamu menjadi tumpuan harapan guru dan calon mahasiswa sukses bersama pasangan pilihanmu.",
                penjualan = StatLevel.Sedang, prestasi = StatLevel.Tinggi
            },
            new EndingData {
                title = "Tragedi Sang Spekulan",
                subtitle = "Runtuhnya Dominasi Semu",
                description = "Akun terblokir, tautan hangus, dan sisa nilai rapormu tak sanggup menyelamatkanmu. Strategi pemasaran yang agresif dan melanggar aturan membuat toko digitalmu ditutup paksa. Di saat yang sama, kelalaianmu belajar membuatmu dipanggil oleh pimpinan sekolah. Hubunganmu dengan pasangan berada di ambang kehancuran di tengah puing-puing kegagalanmu.",
                penjualan = StatLevel.Rendah, prestasi = StatLevel.Rendah
            },
            new EndingData {
                title = "Monumen Keindahan Sepi",
                subtitle = "Estetika Yang Tak Terjamah",
                description = "Sebuah mahakarya visual di balik bio, yang hanya dikunjungi oleh angin malam. Kamu menghabiskan waktu berpekan-pekan merancang tampilan halaman landing-page yang super artistik dan elegan. Namun, kelemahan pada strategi pemasaran membuat bisnis ini sepi pembeli. Meskipun demikian, reputasimu sebagai kreator estetik tetap diakui di sekolah, dan pasanganmu dengan setia bangga menjadi pendukung utamamu.",
                penjualan = StatLevel.Rendah, prestasi = StatLevel.Sedang
            },
            new EndingData {
                title = "Legenda Yang Terlupakan",
                subtitle = "Sang Visioner Di Waktu Yang Salah",
                description = "Kamu merancang sistem yang terlalu canggih untuk dipahami oleh zamanmu. Dibanding menggunakan platform populer, kamu memilih mengoding situs social commerce milikmu sendiri dari nol dengan fitur melampaui usiamu. Pasar SMA tidak siap dan memilih tempat biasa, membuatmu gagal secara finansial. Namun, kejeniusanmu memukau para pakar dan membukakan pintu beasiswa penuh ke akademi teknologi terkemuka!",
                penjualan = StatLevel.Rendah, prestasi = StatLevel.Tinggi
            },
        };
    }

    // Ambil ending sesuai kombinasi penjualan & prestasi
    public EndingData GetEnding(StatLevel penjualan, StatLevel prestasi)
    {
        return endings.Find(e => e.penjualan == penjualan && e.prestasi == prestasi);
    }
}
