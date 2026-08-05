using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProdukLM
{
    public static class StatsCalculator
    {
        // Kalibrasi skor hubungan antar slot. Nilai default dapat diubah dari
        // Inspector lewat ProjectFlowManager tanpa menyentuh script ini.
        public const int DefaultAffinityScore = 20;
        public const int DefaultNeutralScore = 5;
        public const int DefaultConflictScore = -15;

        public static int AffinityScore { get; set; } = DefaultAffinityScore;
        public static int NeutralScore { get; set; } = DefaultNeutralScore;
        public static int ConflictScore { get; set; } = DefaultConflictScore;

        // Relevansi berfokus pada rantai keputusan utama Prompt Builder.
        // Quality tetap memeriksa seluruh pasangan kartu (cross connection).
        static readonly HashSet<(SlotType, SlotType)> RelevancePairs = new()
        {
            Key(SlotType.ProductType, SlotType.Purpose),
            Key(SlotType.Purpose, SlotType.Audience),
            Key(SlotType.Audience, SlotType.ContentFocus),
            Key(SlotType.ContentFocus, SlotType.Style),
            Key(SlotType.Style, SlotType.AIOptimization),
        };

        static (SlotType, SlotType) Key(SlotType a, SlotType b) =>
            (int)a < (int)b ? (a, b) : (b, a);

        public static StatsResult Calculate(ProjectState state)
        {
            var filled = state.selectedCards
                .Select((card, index) => (card, slot: (SlotType)index))
                .Where(x => x.card != null)
                .ToArray();

            if (filled.Length < 2)
                return default;

            float qualityRaw = 0;
            int qualityPairs = 0;
            float relevanceRaw = 0;
            int relevancePairs = 0;

            // j selalu mulai dari i + 1, jadi setiap pasangan kartu hanya
            // diperiksa satu kali dan skornya tidak pernah ditambahkan dobel.
            for (int i = 0; i < filled.Length; i++)
            {
                for (int j = i + 1; j < filled.Length; j++)
                {
                    var a = filled[i];
                    var b = filled[j];

                    bool hasAffinity = Contains(a.card.affinityCards, b.card) ||
                                       Contains(b.card.affinityCards, a.card);
                    bool hasConflict = Contains(a.card.conflictCards, b.card) ||
                                       Contains(b.card.conflictCards, a.card);
                    // Conflict diprioritaskan jika data dua kartu tidak sengaja memuat kedua relasi.
                    int pairScore = hasConflict ? ConflictScore
                        : hasAffinity ? AffinityScore
                        : NeutralScore;

                    qualityRaw += pairScore;
                    qualityPairs++;

                    if (RelevancePairs.Contains(Key(a.slot, b.slot)))
                    {
                        relevanceRaw += pairScore;
                        relevancePairs++;
                    }
                }
            }

            int quality = Normalize(qualityRaw, qualityPairs);
            int relevansi = Normalize(relevanceRaw, relevancePairs);

            // Nilai Jual memakai kualitas keseluruhan dan kekuatan rantai prompt utama.
            int nilaiJual = Mathf.RoundToInt(
                quality * 0.60f +
                relevansi * 0.40f);

            return new StatsResult
            {
                Quality = quality,
                Relevansi = relevansi,
                NilaiJual = nilaiJual,
            };
        }

        // Skor mentah berada di antara semua-conflict dan semua-affinity, dan
        // rentangnya tidak simetris (-15 vs +20). Pemetaan memakai batas nyata
        // kedua ujung itu supaya kombinasi netral tidak ikut bergeser saat
        // nilai kalibrasi diubah.
        static int Normalize(float raw, int pairCount)
        {
            if (pairCount <= 0) return 50; // nggak ada pasangan relevan sama sekali -> netral

            float worst = ConflictScore * pairCount;
            float best = AffinityScore * pairCount;
            if (best - worst <= Mathf.Epsilon) return 50;

            float normalized = (raw - worst) / (best - worst) * 100f;
            return Mathf.Clamp(Mathf.RoundToInt(normalized), 0, 100);
        }

        static bool Contains(CardData[] cards, CardData target) =>
            cards != null && cards.Contains(target);
    }
}
