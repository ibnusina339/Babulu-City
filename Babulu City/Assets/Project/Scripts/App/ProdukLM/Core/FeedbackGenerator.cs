using System.Collections.Generic;
using System.Linq;

namespace ProdukLM
{
    public static class FeedbackGenerator
    {
        // Dipanggil setelah "Generate", isi panel AI Analysis
        public static List<string> Generate(ProjectState state)
        {
            var feedback = new List<string>();
            var filled = state.selectedCards.Where(c => c != null).ToArray();

            for (int i = 0; i < filled.Length; i++)
            {
                for (int j = i + 1; j < filled.Length; j++)
                {
                    var a = filled[i];
                    var b = filled[j];
                    if (a.conflictCards.Contains(b) || b.conflictCards.Contains(a))
                    {
                        feedback.Add($"{a.displayName} kurang cocok dengan {b.displayName}. Coba ganti salah satunya.");
                    }
                }
            }

            if (feedback.Count == 0)
                feedback.Add("Kombinasi kartu kamu sudah cukup konsisten. Bagus!");

            return feedback;
        }
    }
}
