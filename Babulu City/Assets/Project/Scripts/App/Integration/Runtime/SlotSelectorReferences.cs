using System;
using ProdukLM;
using TMPro;
using UnityEngine.UI;

namespace IntegratedApps
{
    [Serializable]
    public class SlotSelectorReferences
    {
        public SlotType slotType;
        public TMP_Text valueText;
        public Button previousButton;
        public Button nextButton;
    }
}
