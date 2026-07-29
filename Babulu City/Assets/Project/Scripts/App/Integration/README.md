# ProdukLM + Laris.ID Integration Test

Scene:

`Assets/Project/Scenes/ProdukLM_LarisID_Test.unity`

## Hierarchy utama

```text
BRIDA_Desktop_UI
├── Taskbar
├── ProdukLM_DesktopIcon
├── LarisID_DesktopIcon
├── ProdukLM_Window
└── LarisID_Window
```

Seluruh tampilan merupakan GameObject UGUI yang tersimpan di scene. Builder hanya
membuat hierarchy satu kali dan tidak menimpa `BRIDA_Desktop_UI` yang sudah ada.

## Pengaturan Inspector

Pilih `BRIDA_Desktop_UI`, lalu buka komponen `Integrated Desktop UI`.

- `Free Daily Limit`, `Plus Daily Limit`, dan `Pro Daily Limit` mengatur limit
  Generate per tier.
- `Free/Plus/Pro Quality Boost` mengatur peningkatan stats menuju nilai 100.
- `Produk LM Icon` dan `Laris ID Icon` menerima sprite desain final untuk icon
  desktop.
- `PDF Icon`, `DOCX Icon`, `PPTX Icon`, `Image Icon`, dan `Generic File Icon`
  menerima sprite icon produk final.

Placeholder huruf dan label PDF/DOCX/PPTX/PNG tetap ditampilkan jika sprite belum
diisi.

## Alur data

```text
Generate ProdukLM (langsung memakai limit)
→ pemain mengisi nama dan Save
→ Laris.ID menerima LarisProductImportData
→ produk masuk Library sebagai Draft
→ pemain mengatur kategori, target, dan harga
→ pemain Publish manual
→ simulasi pasar menghitung tayangan, pembelian, rating, dan rating toko
```

Harga bisa dinaikkan atau diturunkan Rp1.000 menggunakan tombol. Harga di atas
rentang rekomendasi tetap diperbolehkan, tetapi multiplier pembeli turun.
