using System;

namespace BabuluCity.Tutorial
{
    /// <summary>Aksi pemain yang dapat ditahan selama tutorial berjalan.</summary>
    public enum TutorialAction
    {
        CloseLaptop,
        CloseProdukLM,
        CloseLarisID,
        OpenProdukLM,
        OpenLarisID
    }

    /// <summary>
    /// Satu-satunya penghubung antara tutorial dan sistem lain. Selama tutorial
    /// hari pertama masih menunggu satu tugas selesai, aksi yang membatalkan
    /// tugas itu ditahan dan chat "Cannot Leave" ditampilkan.
    ///
    /// Di luar tutorial <see cref="Handler"/> bernilai null sehingga
    /// <see cref="Blocks"/> selalu false dan semua tombol bekerja seperti biasa.
    /// </summary>
    public static class TutorialGuard
    {
        /// <summary>Diisi <see cref="FirstDayTutorialController"/> saat tutorial aktif.</summary>
        public static Func<TutorialAction, bool> Handler;

        public static bool Blocks(TutorialAction action) => Handler != null && Handler(action);
    }
}
