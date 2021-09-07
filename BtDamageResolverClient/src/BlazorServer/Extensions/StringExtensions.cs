namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Extensions
{
    /// <summary>
    /// Stupid extensions for strings. Basically HTML manipulation.
    /// </summary>
    public static class StringExtensions
    {
        public static string WrapIntoDiv(this string input, string classToUse=null)
        {
            return classToUse == null ? $"<div>{input}</div>" : $"<div class=\"{classToUse}\">{input}</div>";
        }
    }
}