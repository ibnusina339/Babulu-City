using System;
using UnityEngine;

namespace LarisID
{
    public class LarisIDManager : MonoBehaviour
    {
        [Header("Progress level harga toko")]
        [Min(1)] public int growingTierRequirement = 120;
        [Min(2)] public int famousTierRequirement = 500;

        public LarisMarketplaceService Marketplace { get; private set; }
        public LarisProduct SelectedProduct { get; private set; }
        public string LastMessage { get; private set; } = "Laris.ID siap diuji.";

        public event Action StateChanged;
        public event Action<DailyMarketResult> DaySimulated;

        void Awake()
        {
            Marketplace = new LarisMarketplaceService(
                growingTierRequirement,
                famousTierRequirement);
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
