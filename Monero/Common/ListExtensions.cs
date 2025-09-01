using System;
using System.Collections.Generic;

public static class ListExtensions
{
    private static Random rng = new Random();

    // Estensione per List<T>
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1); // indice casuale
            (list[n], list[k]) = (list[k], list[n]); // swap
        }
    }
}
