
namespace Monero.Common;

public static class ListExtensions
{
    private static readonly Random RNG = new ();
    
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = RNG.Next(n + 1); // random index
            (list[n], list[k]) = (list[k], list[n]); // swap
        }
    }
}