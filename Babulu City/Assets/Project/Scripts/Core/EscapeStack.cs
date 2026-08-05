using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BabuluCity.Core
{
    /// <summary>Urutan prioritas lapisan UI saat tombol ESC ditekan.</summary>
    public enum EscapeLayer
    {
        /// <summary>Layar penuh: kalender, desktop laptop.</summary>
        Screen = 10,

        /// <summary>Jendela aplikasi di dalam desktop laptop.</summary>
        App = 20,

        /// <summary>Popup konfirmasi/informasi yang tampil paling depan.</summary>
        Popup = 30
    }

    /// <summary>
    /// Router tunggal untuk tombol ESC.
    ///
    /// Sebelumnya setiap UI membaca <c>escapeKey.wasPressedThisFrame</c> sendiri.
    /// Karena urutan Update antar komponen tidak dijamin Unity, satu penekanan
    /// ESC bisa terbaca dua kali dalam frame yang sama: kalender menutup diri,
    /// lalu GameSaveManager melihat tidak ada UI terbuka dan langsung memunculkan
    /// popup keluar game.
    ///
    /// Sekarang UI cukup mendaftarkan diri saat terbuka dan melepas pendaftaran
    /// saat tertutup. Hanya GameSaveManager yang membaca tombol ESC, lalu
    /// memanggil <see cref="TryCloseTopmost"/>. Popup keluar game baru muncul
    /// bila tumpukan benar-benar kosong.
    /// </summary>
    public static class EscapeStack
    {
        sealed class Entry
        {
            public UnityEngine.Object owner;
            public EscapeLayer layer;
            public Action close;
            public int sequence;
        }

        static readonly List<Entry> entries = new();
        static int nextSequence;

        /// <summary>True bila masih ada UI yang harus ditutup sebelum popup keluar game.</summary>
        public static bool HasOpenLayer => Prune() > 0;

        /// <summary>
        /// Mendaftarkan satu lapisan UI yang sedang terbuka. <paramref name="owner"/>
        /// boleh berupa komponen atau GameObject popup; satu owner hanya punya satu
        /// entri sehingga pemanggilan berulang tidak menumpuk.
        /// </summary>
        public static void Register(UnityEngine.Object owner, EscapeLayer layer, Action close)
        {
            if (owner == null || close == null)
                return;

            Unregister(owner);
            entries.Add(new Entry
            {
                owner = owner,
                layer = layer,
                close = close,
                sequence = nextSequence++
            });
        }

        /// <summary>Melepas pendaftaran saat UI ditutup lewat tombol atau kode lain.</summary>
        public static void Unregister(UnityEngine.Object owner)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                // owner == null juga membersihkan objek yang sudah dihancurkan.
                if (entries[i].owner == null || entries[i].owner == owner)
                    entries.RemoveAt(i);
            }
        }

        /// <summary>
        /// Menutup satu lapisan terdepan: prioritas layer tertinggi, lalu yang
        /// paling terakhir dibuka. Mengembalikan false bila tidak ada apa-apa.
        /// </summary>
        public static bool TryCloseTopmost()
        {
            if (Prune() == 0)
                return false;

            int top = 0;
            for (int i = 1; i < entries.Count; i++)
            {
                Entry candidate = entries[i];
                Entry best = entries[top];
                if (candidate.layer > best.layer ||
                    (candidate.layer == best.layer && candidate.sequence > best.sequence))
                    top = i;
            }

            Entry closing = entries[top];
            entries.RemoveAt(top);
            closing.close.Invoke();
            return true;
        }

        public static void Clear()
        {
            entries.Clear();
        }

        static int Prune()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].owner == null)
                    entries.RemoveAt(i);
            return entries.Count;
        }
    }

    /// <summary>
    /// Pembaca ESC cadangan untuk scene yang tidak memiliki GameSaveManager,
    /// misalnya scene testing. Di scene Main, GameSaveManager yang memegang
    /// tombol ESC supaya popup keluar game tetap ikut alur prioritas.
    /// </summary>
    public sealed class EscapeStackFallbackReader : MonoBehaviour
    {
        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                EscapeStack.TryCloseTopmost();
        }
    }

    static class EscapeStackBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            // Objek dari scene lama sudah hancur, tetapi list statis tetap hidup
            // antar scene. Bersihkan supaya ESC di scene baru tidak tertahan entri mati.
            EscapeStack.Clear();

            if (SceneManager.GetActiveScene().name == "Main" ||
                UnityEngine.Object.FindAnyObjectByType<EscapeStackFallbackReader>() != null)
                return;

            new GameObject("Escape Stack Reader").AddComponent<EscapeStackFallbackReader>();
        }
    }
}
