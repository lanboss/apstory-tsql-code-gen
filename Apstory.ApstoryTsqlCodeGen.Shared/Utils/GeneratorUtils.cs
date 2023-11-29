using System;
using System.IO;
using System.Linq;

namespace Apstory.ApstoryTsqlCodeGen.Shared.Utils
{
    public static class GeneratorUtils
    {
        public static void WriteToFile(string filePath, string contents)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, contents);
        }

        public static string CamelCase(string stringToConvert)
        {
            if (stringToConvert == "Event")
            {
                stringToConvert = "Events";
            }
            return Char.ToLowerInvariant(stringToConvert[0]) + stringToConvert.Substring(1);
        }

        public static string ToCamelCase(this string toConvert)
        {
            return char.ToLowerInvariant(toConvert[0]) + toConvert.Substring(1);
        }

        public static string ToDashSeperatedName(this string toConvert)
        {
            if (toConvert.All(s => char.IsUpper(s)))
                return toConvert.ToLower();

            var dsn = CamelCase(toConvert);
            int i = 0;
            while (i < dsn.Length)
            {
                if (char.IsUpper(dsn[i]))
                    dsn = dsn.Substring(0, i) + '-' + char.ToLower(dsn[i]) + dsn.Substring(i + 1);

                i++;
            }

            return dsn;
        }

        public static string GenPathString(string[] genPathList, string pathType)
        {
            if (genPathList.Any(s => s.Equals(pathType, StringComparison.OrdinalIgnoreCase)))
                return ".Gen";

            return string.Empty;
        }

        public static string GenPathModelString(string[] genPathList, string[] genPathNamespaceList, string pathType, string schema)
        {
            var schemaAdd = schema != "dbo";
            if (genPathList.Any(s => s.Equals(pathType, StringComparison.OrdinalIgnoreCase)))
            {
                if (genPathNamespaceList.Any(s => s.Equals(pathType, StringComparison.OrdinalIgnoreCase)))
                {
                    return (schemaAdd ? schema.ToUpper() + ".Gen." : "Gen.");
                }
            }

            return (schemaAdd ? schema.ToUpper() + "." : string.Empty);
        }

        public static string GenPathNamespaceString(string[] genPathNamespaceList, string pathType)
        {
            if (genPathNamespaceList.Any(s => s.Equals(pathType, StringComparison.OrdinalIgnoreCase)))
                return ".Gen";

            return string.Empty;
        }
    }
}
