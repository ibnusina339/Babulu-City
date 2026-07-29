using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProdukLM
{
    public static class StatsCalculator
    {
        const int AffinityScore = 10;
        const int ConflictScore = -10;

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
            float qualityMax = 0;
            float relevanceRaw = 0;
            float relevanceMax = 0;

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
                    int pairScore = hasConflict ? ConflictScore : hasAffinity ? AffinityScore : 0;

                    qualityRaw += pairScore;
                    qualityMax += AffinityScore;

                    if (RelevancePairs.Contains(Key(a.slot, b.slot)))
                    {
                        relevanceRaw += pairScore;
                        relevanceMax += AffinityScore;
                    }
                }
            }

            int quality = Normalize(qualityRaw, qualityMax);
            int relevansi = Normalize(relevanceRaw, relevanceMax);

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

        // Raw score bisa negatif (banyak conflict), jadi di-mapping ke rentang 0-100
        // dengan asumsi rentang teoretis dari -max sampai +max.
        static int Normalize(float raw, float max)
        {
            if (max <= 0) return 50; // nggak ada pasangan relevan sama sekali -> netral
            float normalized = (raw / max + 1f) / 2f * 100f;
            return Mathf.Clamp(Mathf.RoundToInt(normalized), 0, 100);
        }

        static bool Contains(CardData[] cards, CardData target) =>
            cards != null && cards.Contains(target);
    }
}
