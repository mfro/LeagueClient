using MFroehlich.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient {
  public static class Extensions {
    public static T WaitForResult<T>(this Task<T> t) {
      t.Wait();
      return t.Result;
    }

    public static void WriteString(this Stream stm, string str) {
      stm.Write(Encoding.UTF8.GetBytes(str));
    }

    public static bool CanAssignTo<T>(this Type type) {
      return typeof(T).IsAssignableFrom(type);
    }

    public static string RemoveAllWhitespace(this string str) {
      return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    public static string Substring(this string str, string prefix, string suffix) {
      int start = str.IndexOf(prefix) + prefix.Length;
      if (start < prefix.Length) throw new ArgumentException($"Prefix {prefix} not found in string {str}");
      int end = str.IndexOf(suffix, start);
      if (end < 0) throw new ArgumentException($"Suffix {suffix} not found in string {str} after {prefix}");
      return str.Substring(start, end - start);
    }
  }
}
