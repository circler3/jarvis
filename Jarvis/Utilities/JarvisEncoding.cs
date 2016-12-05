using System;
using System.Text;
using System.IO;

namespace Jarvis
{
  public static class JarvisEncoding
  {
    public static string GetDiff(string actual, string expected)
    {
      string diff = string.Empty;

      if (actual.Equals(expected, StringComparison.Ordinal))
      {
        diff = "No difference";
      }
      else
      {        
        diff = HtmlDiff.HtmlDiff.Execute(actual, expected);
      }

      return diff;
    }

    public static string ToHtmlEncoding(string text)
    {
      StringBuilder builder = new StringBuilder(text);

      builder.Replace("<", "&lt;");
      builder.Replace(">", "&gt;");
      builder.Replace(" ", "&nbsp;");

      builder.Replace("\\", "&#92;");
      builder.Replace("/", "&#47;");
      builder.Replace("\n", "<br />");

      builder.Replace("\x1B[0;31m", "<span style='color: #FF0000;'>");
      builder.Replace("\x1B[0;35m", "<span style='color: #57FF42;'>");
      builder.Replace("\x1B[0m", "</span>");

      return builder.ToString();
    }

    public static string ToHtmlEncodingWithNewLines(string text)
    {
      StringBuilder builder = new StringBuilder(text);

      builder.Replace("<", "&lt;");
      builder.Replace(">", "&gt;");
      builder.Replace(" ", "&nbsp;");

      //Handle ASCII non-printables
      builder.Replace("\0", "<span style='color: #888888; font-size: 10px;'>\\0</span>");
      builder.Replace("\a", "<span style='color: #888888; font-size: 10px;'>\\a</span>");
      builder.Replace("\b", "<span style='color: #888888; font-size: 10px;'>\\b</span>");
      builder.Replace("\t", "<span style='color: #888888;'>&#8677;</span>"); //replace ascii tab with Unicode "RIGHT ARROW TO BAR"
      builder.Replace("\n", "<span style='color: #888888; font-size: 10px;'>\\n</span><br />");
      builder.Replace("\v", "<span style='color: #888888; font-size: 10px;'>\\v</span>");
      builder.Replace("\f", "<span style='color: #888888; font-size: 10px;'>\\f</span>");
      builder.Replace("\r", "<span style='color: #888888; font-size: 10px;'>\\r</span>");

      return builder.ToString();
    }

    public static string ToTextEncoding(string text)
    {
      StringBuilder builder = new StringBuilder(text);

      builder.Replace("<", "&lt;");
      builder.Replace(">", "&gt;");
      builder.Replace("&nbsp;", " ");
      builder.Replace("<br />", "\n");

      return builder.ToString();
    }

    public static string ConvertToBase64(string pngFile, bool deleteAfter = true)
    {
      string result = string.Empty;

      if (File.Exists(pngFile))
      {
        byte[] pngBytes = File.ReadAllBytes(pngFile);

        if (deleteAfter)
        {
          File.Delete(pngFile);
        }

        result = Convert.ToBase64String(pngBytes);
      }

      return result;
    }
  }
}

