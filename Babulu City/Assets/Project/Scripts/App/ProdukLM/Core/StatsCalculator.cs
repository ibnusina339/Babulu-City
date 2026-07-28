using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProdukLM
{
    public static class StatsCalculator
    {
        const int AffinityScore = 10;
        const int ConflictScore = -10;

        // Bobot per pasangan SlotType, per stat. Key dinormalisasi (urutan slot kecil dulu).
        // Bebas di-tuning sambil playtest -- ini cuma titik awal yang masuk akal.
        static readonly Dictionary<(SlotType, SlotType), float> EstetikaWeight = new()
        {
            [Key(SlotType.ProductType, SlotType.Style)] = 1.0f,
            [Key(SlotType.Style, SlotType.AIOptimization)] = 1.0f,
            [Key(SlotType.ProductType, SlotType.AIOptimization)] = 0.8f,
            [Key(SlotType.Purpose, SlotType.Style)] = 0.4f,
            [Key(SlotType.Audience, SlotType.Style)] = 0.4f,
            [Key(SlotType.ContentFocus, SlotType.Style)] = 0.4f,
        };

        static readonly Dictionary<(SlotType, SlotType), float> ProfesionalismeWeight = new()
        {
            [Key(SlotType.Audience, SlotType.Style)] = 1.0f,
            [Key(SlotType.Audience, SlotType.ContentFocus)] = 1.0f,
            [Key(SlotType.Style, SlotType.ContentFocus)] = 1.0f,
            [Key(SlotType.Purpose, SlotType.Style)] = 0.6f,
            [Key(SlotType.Audience, SlotType.AIOptimization)] = 0.8f,
            [Key(SlotType.ContentFocus, SlotType.AIOptimization)] = 0.6f,
        };

        const float DefaultWeight = 0.5f; // dipakai kalau pasangan slot nggak ada di tabel di atas

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

            float qualityRaw = 0, estetikaRaw = 0, profesionalismeRaw = 0;
            float qualityMax = 0, estetikaMax = 0, profesionalismeMax = 0;
            int affinityPairs = 0;
            int totalPairs = 0;

            for (int i = 0; i < filled.Length; i++)
            {
                for (int j = i + 1; j < filled.Length; j++)
                {
                    var a = filled[i];
                    var b = filled[j];
                    totalPairs++;

                    bool hasAffinity = a.card.affinityCards.Contains(b.card) || b.card.affinityCards.Contains(a.card);
                    bool hasConflict = a.card.conflictCards.Contains(b.card) || b.card.conflictCards.Contains(a.card);
                    int pairScore = hasAffinity ? AffinityScore : hasConflict ? ConflictScore : 0;
                    if (hasAffinity) affinityPairs++;

                    float estW = EstetikaWeight.GetValueOrDefault(Key(a.slot, b.slot), DefaultWeight);
                    float proW = ProfesionalismeWeight.GetValueOrDefault(Key(a.slot, b.slot), DefaultWeight);

                    qualityRaw += pairScore;
                    estetikaRaw += pairScore * estW;
                    profesionalismeRaw += pairScore * proW;

                    qualityMax += AffinityScore;
                    estetikaMax += AffinityScore * estW;
                    profesionalismeMax += AffinityScore * proW;
                }
            }

            int quality = Normalize(qualityRaw, qualityMax);
            int estetika = Normalize(estetikaRaw, estetikaMax);
            int profesionalisme = Normalize(profesionalismeRaw, profesionalismeMax);
            int relevansi = totalPairs > 0 ? Mathf.RoundToInt((float)affinityPairs / totalPairs * 100f) : 0;

            // Nilai Jual = rata-rata berbobot dari 4 stat lain, bukan dihitung dari pasangan kartu langsung
            int nilaiJual = Mathf.RoundToInt(
                quality * 0.35f +
                relevansi * 0.20f +
                estetika * 0.20f +
                profesionalisme * 0.25f);

            return new StatsResult
            {
                Quality = quality,
                Relevansi = relevansi,
                Estetika = estetika,
                Profesionalisme = profesionalisme,
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
    }
}
