/*
 * IdlePond — les fonctions de `Math` que .NET n'a pas.
 *
 * Une seule aujourd'hui, et elle porte l'équivalence de pas : `expm1`. Sur les
 * pas de 100 ms, k·dt vaut ~3e-4 et la maturation ~5e-6 ; `1 - exp(-x)` y perd
 * les chiffres qui font tenir « 480 pas de 60 s valent un pas de 8 h ».
 */
using System;

namespace IdlePond.Nombres
{
    public static class MathJs
    {
        /// <summary>
        /// e^x − 1 sans annulation. Méthode de Kahan : l'erreur de `exp` et celle
        /// de `log` se compensent, à quelques ulps près sur tout le domaine.
        /// </summary>
        public static double Expm1(double x)
        {
            if (double.IsNaN(x)) return double.NaN;
            if (double.IsPositiveInfinity(x)) return double.PositiveInfinity;
            if (double.IsNegativeInfinity(x)) return -1;
            if (x == 0) return x;

            var u = Math.Exp(x);
            if (u == 1.0) return x;
            var um1 = u - 1.0;
            if (um1 == -1.0) return -1.0;
            if (double.IsInfinity(u)) return u;
            return um1 * x / Math.Log(u);
        }
    }
}
