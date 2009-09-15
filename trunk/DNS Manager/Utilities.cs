namespace DNS_Manager
{
    internal static class Utilities
    {
        internal static string CapitaliseFirstLetter(this string m)
        {
            return m[0].ToString().ToUpper() + m.Remove(0, 1);
        }
    }
}
