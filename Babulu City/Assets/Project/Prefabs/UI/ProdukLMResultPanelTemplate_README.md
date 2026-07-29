# ProdukLM Result Panel Template

Prefab utama:

`ProdukLMResultPanelTemplate.prefab`

Template hanya menampilkan tiga statistik:

- Quality
- Relevansi
- Nilai Jual

## Aman untuk diubah

Designer bebas mengubah:

- posisi dan ukuran seluruh elemen;
- warna, font, sprite, dan background;
- hierarchy layout;
- bentuk progress bar;
- nama GameObject setelah seluruh referensi selesai dihubungkan.

Logika perhitungan tidak bergantung pada tata letak.

## Referensi yang perlu dipertahankan

Komponen `StatsResultUI` berada di root prefab. Setelah mengganti atau membuat
ulang elemen UI, hubungkan kembali field berikut di Inspector:

- Product Name Text
- Final Prompt Text
- Quality Label Text
- Quality Slider dan Quality Text
- Relevansi Slider dan Relevansi Text
- Nilai Jual Slider dan Nilai Jual Text
- Feedback Container
- Feedback Line Prefab

Field Slider atau Text boleh dibiarkan kosong jika desain tidak ingin
menampilkan bagian tersebut. Script sudah melakukan null-check.

`DailyLimitText` memiliki komponen `DailyLimitUI`. Elemen ini bebas dipindah,
diubah font dan warnanya, atau diganti dengan TMP Text lain selama komponen
`DailyLimitUI` tetap dipasang pada TMP Text yang akan menampilkan sisa kuota.

Jumlah batas harian diatur melalui field `Daily Product Limit` pada
`ProjectFlowManager` di GameManager. Nilai default-nya adalah 5.

## Memakai di scene lain

1. Drag `ProdukLMResultPanelTemplate.prefab` ke panel tahap hasil.
2. Jadikan parent panel dalam keadaan nonaktif saat scene dimulai.
3. Hubungkan parent panel tersebut ke `resultPanel` pada `ProjectFlowManager`.
4. Jangan menambahkan kalkulator ke prefab; prefab hanya bertanggung jawab
   menampilkan data dari `ProjectFlowManager`.
