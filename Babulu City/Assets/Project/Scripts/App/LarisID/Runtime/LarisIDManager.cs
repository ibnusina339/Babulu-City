using System;
using UnityEngine;

namespace LarisID
{
    public class LarisIDManager : MonoBehaviour
    {
        [Header("Progress level harga toko")]
        [Min(1)] public int growingTierRequirement = 120;
        [Min(2)] public int famousTierRequirement = 500;

        [Header("Frekuensi pembeli harian")]
        [Tooltip("Atur ramai-sepinya pasar tanpa mengubah rumus simulasi.")]
        public LarisMarketTuning marketTuning = new LarisMarketTuning();

        public LarisMarketplaceService Marketplace { get; private set; }
        public LarisProduct SelectedProduct { get; private set; }
        public string LastMessage { get; private set; } = "Laris.ID siap diuji.";

        public event Action StateChanged;
        public event Action<DailyMarketResult> DaySimulated;

        void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (Marketplace != null)
            {
                // Nilai Inspector tetap dipakai walaupun service sudah dibuat.
                Marketplace.Tuning = marketTuning ??= new LarisMarketTuning();
                return;
            }

            Marketplace = new LarisMarketplaceService(
                growingTierRequirement,
                famousTierRequirement)
            {
                Tuning = marketTuning ??= new LarisMarketTuning()
            };
        }

        public LarisProduct AddDummyProduct()
        {
            SelectedProduct = Marketplace.AddDummyProduct();
            LastMessage = $"Draft dummy '{SelectedProduct.productName}' ditambahkan.";
            NotifyChanged();
            return SelectedProduct;
        }

        public LarisProduct ImportProduct(LarisProductImportData data)
        {
            SelectedProduct = Marketplace.AddProductFromImport(data);
            LastMessage = $"Produk '{SelectedProduct.productName}' masuk sebagai draft.";
            NotifyChanged();
            return SelectedProduct;
        }

        public void SelectProduct(LarisProduct product)
        {
            SelectedProduct = product;
            LastMessage = product != null ? $"Membuka {product.productName}." : "Produk ditutup.";
            NotifyChanged();
        }

        public void PublishSelected()
        {
            if (Marketplace.Publish(SelectedProduct, out string message))
                LastMessage = message;
            else
                LastMessage = message;
            NotifyChanged();
        }

        public void PromoteProduct(LarisProduct product, PromoterOffer offer)
        {
            Marketplace.Promote(product, offer, out string message);
            LastMessage = message;
            NotifyChanged();
        }

        public void SetSelectedProductPrice(int requestedPrice)
        {
            Marketplace.SetPrice(SelectedProduct, requestedPrice, out string message);
            LastMessage = message;
            NotifyChanged();
        }

        public void SetProductName(LarisProduct product, string requestedName)
        {
            if (product == null)
                return;

            string value = requestedName?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                LastMessage = "Nama produk tidak boleh kosong.";
                NotifyChanged();
                return;
            }

            if (value.Length > 18)
                value = value.Substring(0, 18).TrimEnd();

            product.productName = value;
            LastMessage = $"Nama produk diubah menjadi '{value}'.";
            NotifyChanged();
        }

        public void SetProductPrice(LarisProduct product, int requestedPrice)
        {
            Marketplace.SetPrice(product, requestedPrice, out string message);
            LastMessage = message;
            NotifyChanged();
        }

        public void ToggleProductActive(LarisProduct product)
        {
            if (product == null)
                return;

            if (product.status == ProductStatus.Active)
            {
                Marketplace.Archive(product);
                LastMessage = $"'{product.productName}' dinonaktifkan dari toko.";
                NotifyChanged();
                return;
            }

            // Harga yang tersimpan bisa berada di luar rentang level toko saat
            // ini, misalnya setelah level toko naik atau setelah save lama
            // dimuat. Publish lalu selalu gagal, dan karena Laris.ID tidak punya
            // area pesan, tombol Aktif/Mati terlihat seperti tidak bisa ditekan.
            // Harga dirapikan dulu ke rentang yang berlaku supaya alasan gagal
            // hanya tersisa untuk data yang memang belum lengkap.
            PriceRange band = LarisPricing.GetMarketBand(product, Marketplace.StoreTier);
            if (product.price < band.minimum || product.price > band.maximum)
                Marketplace.SetPrice(product, Mathf.Clamp(product.price, band.minimum, band.maximum), out _);

            if (!Marketplace.Publish(product, out string publishMessage))
            {
                Debug.LogWarning(
                    $"Produk '{product.productName}' belum dapat dijual: {publishMessage}",
                    this);
            }

            LastMessage = publishMessage;
            NotifyChanged();
        }

        public void DeleteProduct(LarisProduct product)
        {
            if (!Marketplace.DeleteProduct(product))
                return;

            if (SelectedProduct == product)
                SelectedProduct = null;
            LastMessage = $"Produk '{product.productName}' dihapus dari library toko.";
            NotifyChanged();
        }

        public void SetShopIdentity(string shopName, string shopDescription)
        {
            if (!string.IsNullOrWhiteSpace(shopName))
                Marketplace.ShopName = shopName.Trim();
            if (!string.IsNullOrWhiteSpace(shopDescription))
                Marketplace.ShopDescription = shopDescription.Trim();
            LastMessage = "Profil toko diperbarui.";
            NotifyChanged();
        }

        public void ArchiveSelected()
        {
            Marketplace.Archive(SelectedProduct);
            LastMessage = "Produk diarsipkan. Riwayat performa tetap tersimpan.";
            NotifyChanged();
        }

        public void ReactivateSelected()
        {
            Marketplace.Reactivate(SelectedProduct);
            LastMessage = "Produk diaktifkan kembali.";
            NotifyChanged();
        }

        public DailyMarketResult SimulateOneDay()
        {
            StorePriceTier previousTier = Marketplace.StoreTier;
            DailyMarketResult result = Marketplace.SimulateOneDay();
            LastMessage =
                $"Hari {result.day}: +{result.newSales} penjualan, " +
                $"+Rp {result.newRevenue:N0}.";
            if (Marketplace.StoreTier > previousTier)
                LastMessage +=
                    $" Level toko naik menjadi {Marketplace.StoreTier}; rentang harga baru terbuka.";
            DaySimulated?.Invoke(result);
            NotifyChanged();
            return result;
        }

        public void CycleTrend()
        {
            Marketplace.CycleTrend();
            LastMessage = $"Tren pasar diubah menjadi {Marketplace.ActiveTrend}.";
            NotifyChanged();
        }

        public bool TrySpendBalance(long amount, string purchaseName)
        {
            bool success = Marketplace.TrySpend(amount, out string message);
            LastMessage = success
                ? $"{purchaseName} dibeli. {message}"
                : message;
            NotifyChanged();
            return success;
        }

        public void ResetAll()
        {
            Marketplace.Reset();
            SelectedProduct = null;
            LastMessage = "Seluruh data Laris.ID sudah di-reset.";
            NotifyChanged();
        }

        public void NotifyProductEdited()
        {
            LastMessage = "Perubahan produk diterapkan untuk sesi testing.";
            NotifyChanged();
        }

        void NotifyChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
