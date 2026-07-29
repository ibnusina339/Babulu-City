using System;

namespace LarisID
{
    public static class LarisReviewGenerator
    {
        static readonly string[] Positive =
        {
            "Produknya sangat membantu dan mudah digunakan.",
            "Sesuai deskripsi. Kualitasnya memuaskan.",
            "Worth it untuk harganya.",
            "Saya akan membeli produk lain dari toko ini."
        };

        static readonly string[] LowAesthetic =
        {
            "Isinya berguna, tetapi desainnya masih bisa dibuat lebih menarik.",
            "Tampilan visualnya terasa kurang rapi.",
            "Desain produknya belum terlalu menarik perhatian."
        };

        static readonly string[] LowRelevance =
        {
            "Isi produk kurang sesuai dengan kebutuhan saya.",
            "Target produknya terasa kurang jelas.",
            "Beberapa bagian tidak relevan dengan yang saya cari."
        };

        static readonly string[] Professional =
        {
            "Cocok digunakan untuk kebutuhan formal.",
            "Formatnya profesional dan siap dipakai untuk bisnis.",
            "Penyajiannya rapi untuk kebutuhan kerja."
        };

        public static string Generate(LarisProduct product, int rating, Random random)
        {
            string[] pool;
            if (product.relevance < 50)
                pool = LowRelevance;
            else if (product.aesthetic < 50)
                pool = LowAesthetic;
            else if (product.professionalism >= 75)
                pool = Professional;
            else
                pool = Positive;

            return pool[random.Next(pool.Length)];
        }
    }
}
