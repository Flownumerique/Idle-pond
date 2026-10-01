// C# 9 : les records et `init` exigent ce type, que la bibliothèque de base
// d'Unity ne fournit pas. Il n'a aucun comportement.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
