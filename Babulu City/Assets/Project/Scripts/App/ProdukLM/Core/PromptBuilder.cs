namespace ProdukLM
{
    public static class PromptBuilder
    {
        public static string Build(ProjectState state)
        {
            if (!state.IsSlotFilled(SlotType.ProductType))
                return string.Empty;

            string prompt = $"Buatkan {Fragment(SlotType.ProductType)}";

            if (!state.IsSlotFilled(SlotType.Purpose))
                return prompt + ".";
            prompt += $" untuk {Fragment(SlotType.Purpose)}";

            if (!state.IsSlotFilled(SlotType.Audience))
                return prompt + ".";
            prompt += $", ditujukan untuk {Fragment(SlotType.Audience)}";

            if (!state.IsSlotFilled(SlotType.ContentFocus))
                return prompt + ".";
            prompt += $", dengan fokus konten pada {Fragment(SlotType.ContentFocus)}";

            if (!state.IsSlotFilled(SlotType.Style))
                return prompt + ".";
            prompt += $", menggunakan gaya {Fragment(SlotType.Style)}";

            if (!state.IsSlotFilled(SlotType.AIOptimization))
                return prompt + ".";
            prompt += $", dan dioptimasi untuk {Fragment(SlotType.AIOptimization)}";

            return prompt + ".";

            string Fragment(SlotType slot) => state.GetCard(slot).promptFragment;
        }
    }
}
