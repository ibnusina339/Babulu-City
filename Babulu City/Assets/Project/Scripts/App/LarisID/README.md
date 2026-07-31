# Laris.ID Test System

Scene testing:

`Assets/Project/Scenes/LarisID_Test.unity`

## Struktur

- `Data/LarisModels.cs` — model produk, review, hasil harian, analytics, dan DTO integrasi.
- `Core/LarisPricing.cs` — rekomendasi harga dan dampak harga terhadap pembelian.
- `Core/LarisMarketSimulator.cs` — perhitungan pasar harian.
- `Core/LarisReviewGenerator.cs` — pemilihan ulasan berbasis kondisi produk.
- `Core/LarisMarketplaceService.cs` — state toko dan seluruh aksi marketplace.
- `Runtime/LarisIDManager.cs` — penghubung service dengan tampilan.
- `UI/LarisIDSceneUI.cs` — presenter UGUI; hanya mengisi data dan menerima aksi tombol.
- `UI/LarisProductRowUI.cs` — binding untuk template baris produk.

## Mengubah desain

Seluruh tampilan sudah tersimpan sebagai GameObject di dalam scene:

```text
LarisID_UIRoot
├── TopBar
├── Sidebar
└── ContentRoot
    ├── DashboardPage
    ├── ProductsPage
    ├── ProductDetailPage
    ├── AnalyticsPage
    └── DailySummaryPage
```

Desainer bebas mengubah warna `Image`, font dan ukuran TMP, anchor, posisi,
ukuran panel, serta bentuk tombol melalui Inspector. Jangan menghapus komponen
`LarisIDSceneUI` atau referensi di dalamnya. `ProductRowTemplate`,
`ReviewRowTemplate`, dan `DailyProductResultTemplate` sengaja dibuat tidak aktif;
objek tersebut merupakan template yang diduplikasi saat game berjalan.

## Integrasi ProdukLM nanti

ProdukLM membuat `LarisProductImportData`, lalu mengirimkannya melalui:

```csharp
larisIDManager.ImportProduct(importData);
```

Produk selalu masuk sebagai `Draft`. Laris.ID tidak membaca GameObject, panel,
atau TMP Text milik ProdukLM.

## Membuat ulang scene

Gunakan menu:

Hapus GameObject `LarisID_UIRoot`, lalu gunakan menu:

`Tools > Laris.ID > Create Test Scene GameObjects`

Jika `LarisID_UIRoot` masih ada, builder tidak menimpa layout. Ini menjaga
perubahan desainer tetap aman saat script dikompilasi ulang.

Data versi awal hanya hidup selama scene aktif dan akan kembali ke awal ketika
scene dimuat ulang atau tombol Reset digunakan.
