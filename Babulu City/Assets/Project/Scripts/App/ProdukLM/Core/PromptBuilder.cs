namespace ProdukLM
{
    public static class PromptBuilder
    {
        public static string Build(ProjectState state)
        {
            string F(SlotType s) => state.IsSlotFilled(s) ? state.GetCard(s).promptFragment : "...";

            return $"Buatkan {F(SlotType.ProductType)} untuk {F(SlotType.Purpose)}, " +
                   $"ditujukan untuk {F(SlotType.Audience)}, dengan fokus konten pada {F(SlotType.ContentFocus)}, " +
                   $"menggunakan gaya {F(SlotType.Style)}, dan dioptimasi untuk {F(SlotType.AIOptimization)}.";
        }
    }
}
