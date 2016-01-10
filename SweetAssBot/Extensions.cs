using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SweetAssBot
{
  public static class Extensions
  {
    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
      => !source.Any(predicate);

    public static bool IsEmpty<T>(this IEnumerable<T> source)
      => !source.Any();

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
      foreach (var item in source)
        action?.Invoke(item);
    }
  }
}