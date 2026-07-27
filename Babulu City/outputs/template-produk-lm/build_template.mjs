import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "D:/Avicenna Project/Unity/Game/Babulu-City/Babulu City/outputs/template-produk-lm";
const workbook = Workbook.create();
const form = workbook.worksheets.add("Ide Produk");
const guide = workbook.worksheets.add("Petunjuk & Opsi");

form.showGridLines = false;
guide.showGridLines = false;

form.getRange("A1:I1").merge();
form.getRange("A1").values = [["FORM IDE PRODUK DIGITAL – NOTEBOOKLM & LYNK.ID"]];
form.getRange("A2:I2").merge();
form.getRange("A2").values = [["Isi satu ide produk per baris. Baris berwarna krem adalah contoh; baris putih siap diisi."]];

const headers = [[
  "Jenis Produk", "Tujuan", "Target Pengguna", "Konten",
  "Gaya Penyajian / Style", "Fokus AI", "Selaras",
  "Tidak Selaras", "Netral"
]];
form.getRange("A4:I4").values = headers;

const rows = [
  ["Template Canva (contoh)", "Presentasi", "Founder startup", "Pitch deck startup", "Profesional", "Persuasif", "Cocok untuk kebutuhan bisnis", "Kurang cocok untuk pelajar", "Bisa dipakai berbagai industri"],
  ["E-book (contoh)", "Edukasi", "Siswa SMA", "Fisika dasar", "Visual", "Mudah dipahami", "Materi sesuai kurikulum", "Hindari bahasa terlalu teknis", "Bisa belajar mandiri"],
  ...Array.from({ length: 18 }, () => ["", "", "", "", "", "", "", "", ""])
];
form.getRange("A5:I24").values = rows;

const titleFmt = {
  fill: "#1F4E3D",
  font: { bold: true, color: "#FFFFFF", size: 16 },
  horizontalAlignment: "center",
  verticalAlignment: "center"
};
form.getRange("A1:I1").format = titleFmt;
form.getRange("A1:I1").format.rowHeight = 34;
form.getRange("A2:I2").format = {
  fill: "#DCEFE7",
  font: { italic: true, color: "#355B4C", size: 10 },
  horizontalAlignment: "center",
  verticalAlignment: "center"
};
form.getRange("A2:I2").format.rowHeight = 25;
form.getRange("A4:I4").format = {
  fill: "#356B58",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  wrapText: true,
  borders: { preset: "all", style: "thin", color: "#2B5747" }
};
form.getRange("A4:I4").format.rowHeight = 32;
form.getRange("A5:I6").format = {
  fill: "#FFF4DF",
  font: { color: "#5D4730" },
  verticalAlignment: "top",
  wrapText: true
};
form.getRange("A7:I24").format = {
  fill: "#FFFFFF",
  font: { color: "#263238" },
  verticalAlignment: "top",
  wrapText: true
};
form.getRange("A5:I24").format.borders = {
  insideHorizontal: { style: "thin", color: "#DDE5E1" },
  bottom: { style: "thin", color: "#B7C8C0" },
  left: { style: "thin", color: "#B7C8C0" },
  right: { style: "thin", color: "#B7C8C0" }
};
form.getRange("A5:I24").format.rowHeight = 38;

const widths = [22, 18, 22, 28, 22, 22, 30, 30, 28];
for (let c = 0; c < widths.length; c++) {
  form.getRangeByIndexes(0, c, 24, 1).format.columnWidth = widths[c];
}
form.freezePanes.freezeRows(4);

form.getRange("A7:A24").dataValidation = {
  rule: { type: "list", values: ["E-book", "Workbook", "Template Canva", "Paket Prompt AI", "Cheat Sheet", "Mini-course", "Flashcard", "Toolkit Bisnis", "Audio Pembelajaran", "Lainnya"] }
};
form.getRange("B7:B24").dataValidation = {
  rule: { type: "list", values: ["Edukasi", "Bisnis", "Produktivitas", "Hiburan", "Pemasaran", "Pengembangan Diri", "Lainnya"] }
};
form.getRange("E7:E24").dataValidation = {
  rule: { type: "list", values: ["Profesional", "Visual", "Santai", "Minimalis", "Interaktif", "Storytelling", "Akademis"] }
};
form.getRange("F7:F24").dataValidation = {
  rule: { type: "list", values: ["Mudah Dipahami", "Persuasif", "Ringkas", "Mendalam", "Praktis", "Kreatif", "Terstruktur"] }
};

const table = form.tables.add("A4:I24", true, "IdeProdukTable");
table.style = "TableStyleMedium4";
table.showFilterButton = true;
table.showBandedRows = true;

guide.getRange("A1:D1").merge();
guide.getRange("A1").values = [["PETUNJUK PENGISIAN"]];
guide.getRange("A1:D1").format = titleFmt;
guide.getRange("A1:D1").format.rowHeight = 34;
guide.getRange("A3:D3").values = [["Kolom", "Apa yang Diisi", "Contoh", "Tips"]];
guide.getRange("A3:D3").format = {
  fill: "#356B58",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  borders: { preset: "all", style: "thin", color: "#2B5747" }
};
guide.getRange("A4:D12").values = [
  ["Jenis Produk", "Bentuk produk digital yang akan dijual", "E-book / Workbook / Template", "Pilih dari dropdown atau tulis Lainnya"],
  ["Tujuan", "Manfaat utama produk", "Edukasi", "Gunakan satu tujuan paling utama"],
  ["Target Pengguna", "Siapa calon pembelinya", "Siswa SMA kelas 10", "Buat spesifik: usia, profesi, atau kebutuhan"],
  ["Konten", "Topik dan isi produk", "Panduan fisika dasar + latihan", "Jelaskan hasil yang akan diperoleh pembeli"],
  ["Gaya Penyajian / Style", "Nuansa visual atau bahasa", "Visual dan santai", "Sesuaikan dengan target pengguna"],
  ["Fokus AI", "Peran AI dalam penyusunan produk", "Mudah dipahami", "Tentukan kualitas utama yang ingin dicapai"],
  ["Selaras", "Hal yang cocok dengan tujuan/target", "Materi sesuai kurikulum", "Tuliskan alasan produk layak dibuat"],
  ["Tidak Selaras", "Hal yang harus dihindari", "Bahasa terlalu teknis", "Catat batasan, risiko, atau hal yang tidak cocok"],
  ["Netral", "Hal yang tidak terlalu mendukung atau menghambat", "Bisa dipakai belajar mandiri", "Isi jika ada; boleh dikosongkan"]
];
guide.getRange("A4:D12").format = {
  verticalAlignment: "top",
  wrapText: true,
  borders: { preset: "all", style: "thin", color: "#D5DFDA" }
};
guide.getRange("A4:A12").format = { fill: "#EAF3EF", font: { bold: true, color: "#284C3E" }, verticalAlignment: "top", wrapText: true };
guide.getRange("A3:D12").format.rowHeight = 42;
guide.getRange("A3:D3").format.rowHeight = 28;
guide.getRange("A:A").format.columnWidth = 24;
guide.getRange("B:B").format.columnWidth = 34;
guide.getRange("C:C").format.columnWidth = 32;
guide.getRange("D:D").format.columnWidth = 38;
guide.freezePanes.freezeRows(3);

const inspect = await workbook.inspect({
  kind: "table",
  range: "Ide Produk!A1:I10",
  include: "values,formulas",
  tableMaxRows: 10,
  tableMaxCols: 9
});
console.log(inspect.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan"
});
console.log(errors.ndjson);

await fs.mkdir(outputDir, { recursive: true });
for (const [sheetName, fileName, range] of [
  ["Ide Produk", "preview-ide-produk.png", "A1:I12"],
  ["Petunjuk & Opsi", "preview-petunjuk.png", "A1:D12"]
]) {
  const preview = await workbook.render({ sheetName, range, scale: 1.2, format: "png" });
  await fs.writeFile(`${outputDir}/${fileName}`, new Uint8Array(await preview.arrayBuffer()));
}

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outputDir}/Template_Ide_Produk_LM.xlsx`);
