/*
 * Les records et les accesseurs `init` du C# 9 demandent ce type, que le profil
 * .NET Standard 2.1 d'Unity ne fournit pas. Le déclarer est la solution que la
 * documentation d'Unity recommande. Il est public pour servir aux autres
 * assemblages du jeu, et absent sous .NET 5+, qui le porte déjà.
 */
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
#endif
