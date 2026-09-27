namespace IdlePond.Noyau
{
    /// <summary>
    /// mulberry32, bit pour bit celui de `tirer` dans noyau.ts. `Math.imul` y rend
    /// les 32 bits bas du produit : c'est exactement la multiplication `uint`
    /// non vérifiée de C#. Tirage pur : rend la valeur ET l'état suivant.
    ///
    /// Jamais tiré sur le chemin continu (§5.2) : un tirage par tick ferait
    /// diverger 480 pas de 60 s d'un pas de 8 h.
    /// </summary>
    public static class Prng
    {
        public static (double Valeur, EtatPrng Suivant) Tirer(EtatPrng prng)
        {
            unchecked
            {
                var graine = prng.Graine + 0x6d2b79f5u;
                var x = graine;
                x = (x ^ (x >> 15)) * (x | 1u);
                x ^= x + (x ^ (x >> 7)) * (x | 61u);
                return ((x ^ (x >> 14)) / 4294967296.0, new EtatPrng(graine));
            }
        }
    }
}
