using System;
using System.Collections.Generic;
using System.IO;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — un encodeur PNG pur (RGBA 8 bits), sans Unity. Le générateur de provisoires
    /// l'utilise pour que ses octets ne dépendent ni de l'éditeur ni de la plateforme : la même
    /// toile rend les mêmes octets dans Unity, sous .NET, partout. La compression est un
    /// deflate à codes de Huffman fixes et à recherche gloutonne, écrit ici : celui du système
    /// change d'une version à l'autre, et l'empreinte d'un provisoire (son `.meta`) avec.
    /// </summary>
    public static class Png
    {
        /// `rgba` : quatre octets par pixel, la première rangée en HAUT de l'image.
        public static byte[] Encoder(int largeur, int hauteur, byte[] rgba)
        {
            if (rgba.Length != largeur * hauteur * 4) throw new ArgumentException("taille des pixels");
            var brut = new byte[hauteur * (1 + largeur * 4)];
            for (var y = 0; y < hauteur; y++)
            {
                var o = y * (1 + largeur * 4);
                brut[o] = 0; // pas de filtre
                Buffer.BlockCopy(rgba, y * largeur * 4, brut, o + 1, largeur * 4);
            }
            using var sortie = new MemoryStream();
            sortie.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var entete = new byte[13];
            EcrireU32(entete, 0, (uint)largeur);
            EcrireU32(entete, 4, (uint)hauteur);
            entete[8] = 8; entete[9] = 6; entete[10] = 0; entete[11] = 0; entete[12] = 0;
            Morceau(sortie, "IHDR", entete);
            Morceau(sortie, "IDAT", Zlib(brut));
            Morceau(sortie, "IEND", new byte[0]);
            return sortie.ToArray();
        }

        static void EcrireU32(byte[] b, int o, uint v)
        {
            b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v;
        }

        static void Morceau(Stream s, string type, byte[] donnees)
        {
            var t = new byte[4];
            EcrireU32(t, 0, (uint)donnees.Length);
            s.Write(t, 0, 4);
            var nom = new[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
            s.Write(nom, 0, 4);
            s.Write(donnees, 0, donnees.Length);
            var crc = Crc(Crc(0xFFFFFFFFu, nom), donnees) ^ 0xFFFFFFFFu;
            EcrireU32(t, 0, crc);
            s.Write(t, 0, 4);
        }

        static readonly uint[] TABLE_CRC = TableCrc();

        static uint[] TableCrc()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        static uint Crc(uint crc, byte[] b)
        {
            foreach (var x in b) crc = TABLE_CRC[(crc ^ x) & 0xFF] ^ (crc >> 8);
            return crc;
        }

        /* ─── zlib : en-tête, un bloc deflate à codes fixes, Adler-32 ─────────────── */

        static byte[] Zlib(byte[] donnees)
        {
            var bits = new Bits();
            bits.Octet(0x78);
            bits.Octet(0x01);
            bits.Ecrire(1, 1); // dernier bloc
            bits.Ecrire(1, 2); // codes de Huffman fixes
            Comprimer(donnees, bits);
            bits.Litteral(256);
            bits.Aligner();
            uint a = 1, b = 0;
            foreach (var x in donnees) { a = (a + x) % 65521; b = (b + a) % 65521; }
            var adler = (b << 16) | a;
            bits.Octet((byte)(adler >> 24)); bits.Octet((byte)(adler >> 16)); bits.Octet((byte)(adler >> 8)); bits.Octet((byte)adler);
            return bits.Octets();
        }

        const int FENETRE = 32768;
        const int MIN = 3;
        const int MAX = 258;
        const int CHAINE = 64;

        static void Comprimer(byte[] d, Bits bits)
        {
            var tete = new Dictionary<int, int>();
            var precedent = new int[d.Length];
            int Cle(int i) => (d[i] << 16) | (d[i + 1] << 8) | d[i + 2];
            void Inserer(int i)
            {
                if (i + MIN > d.Length) return;
                var cle = Cle(i);
                precedent[i] = tete.TryGetValue(cle, out var p) ? p : -1;
                tete[cle] = i;
            }
            var pos = 0;
            while (pos < d.Length)
            {
                int meilleure = 0, distance = 0;
                if (pos + MIN <= d.Length && tete.TryGetValue(Cle(pos), out var candidat))
                {
                    var essais = 0;
                    while (candidat >= 0 && pos - candidat <= FENETRE && essais++ < CHAINE)
                    {
                        var l = 0;
                        var limite = Math.Min(MAX, d.Length - pos);
                        while (l < limite && d[candidat + l] == d[pos + l]) l++;
                        if (l > meilleure) { meilleure = l; distance = pos - candidat; if (l == limite) break; }
                        candidat = precedent[candidat];
                    }
                }
                if (meilleure >= MIN)
                {
                    bits.Longueur(meilleure);
                    bits.Distance(distance);
                    for (var k = 0; k < meilleure; k++) Inserer(pos + k);
                    pos += meilleure;
                }
                else
                {
                    bits.Litteral(d[pos]);
                    Inserer(pos);
                    pos++;
                }
            }
        }

        static readonly int[] BASES_DE_LONGUEUR = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
        static readonly int[] EXTRA_DE_LONGUEUR = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
        static readonly int[] BASES_DE_DISTANCE = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
        static readonly int[] EXTRA_DE_DISTANCE = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

        sealed class Bits
        {
            readonly List<byte> octets = new List<byte>();
            uint tampon;
            int nombre;

            public void Ecrire(int valeur, int n)
            {
                for (var i = 0; i < n; i++)
                {
                    tampon |= (uint)((valeur >> i) & 1) << nombre;
                    if (++nombre == 8) { octets.Add((byte)tampon); tampon = 0; nombre = 0; }
                }
            }

            /// Un code de Huffman : ses bits du plus fort au plus faible.
            void Code(int code, int n)
            {
                for (var i = n - 1; i >= 0; i--) Ecrire((code >> i) & 1, 1);
            }

            public void Octet(byte b) => Ecrire(b, 8);

            public void Aligner()
            {
                if (nombre > 0) { octets.Add((byte)tampon); tampon = 0; nombre = 0; }
            }

            public void Litteral(int s)
            {
                if (s < 144) Code(0x30 + s, 8);
                else if (s < 256) Code(0x190 + s - 144, 9);
                else if (s < 280) Code(s - 256, 7);
                else Code(0xC0 + s - 280, 8);
            }

            public void Longueur(int l)
            {
                var i = BASES_DE_LONGUEUR.Length - 1;
                while (BASES_DE_LONGUEUR[i] > l) i--;
                if (l == 258) i = 28;
                Litteral(257 + i);
                Ecrire(l - BASES_DE_LONGUEUR[i], EXTRA_DE_LONGUEUR[i]);
            }

            public void Distance(int d)
            {
                var i = BASES_DE_DISTANCE.Length - 1;
                while (BASES_DE_DISTANCE[i] > d) i--;
                Code(i, 5);
                Ecrire(d - BASES_DE_DISTANCE[i], EXTRA_DE_DISTANCE[i]);
            }

            public byte[] Octets() => octets.ToArray();
        }
    }
}
