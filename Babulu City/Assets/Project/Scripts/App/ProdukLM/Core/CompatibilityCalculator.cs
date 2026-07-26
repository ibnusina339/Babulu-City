using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProdukLM
{
    public static class CompatibilityCalculator
    {
        const int AffinityScore = 10;
        const int ConflictScore = -10;

        // Return 0-100. Panggil ini tiap kali ada slot berubah.
        public static int Calculate(ProjectState state)
        {
            var filled = state.selectedCards.Where(c => c != null).ToArray();
            if (filled.Length < 2) return 0;

            int rawScore = 0;
            int pairCount = 0;

            for (int i = 0; i < filled.Length; i++)
            {
                for (int j = i + 1; j < filled.Length; j++)
                {
                    pairCount++;
                    if (filled[i].affinityCards.Contains(filled[j]) ||
                        filled[j].affinityCards.Contains(filled[i]))
                    {
                        rawScore += AffinityScore;
                    }
                    else if (filled[i].conflictCards.Contains(filled[j]) ||
                             filled[j].conflictCards.Contains(filled[i]))
                    {
                        rawScore += ConflictScore;
                    }
                }
            }

            int maxPossible = pairCount * AffinityScore;
            float normalized = maxPossible > 0 ? (float)rawScore / maxPossible : 0f;
            return Mathf.Clamp(Mathf.RoundToInt((normalized + 1f) / 2f * 100f), 0, 100);
        }
    }
}
