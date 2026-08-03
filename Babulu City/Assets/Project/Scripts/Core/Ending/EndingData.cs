using UnityEngine;

public enum StatLevel { Rendah, Sedang, Tinggi }

[System.Serializable]
public class EndingData
{
    public string title;
    public string subtitle;

    [TextArea(3, 6)]
    public string description;

    public StatLevel penjualan;
    public StatLevel prestasi;
}
