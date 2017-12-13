using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ElfUtils {

    public class MemoryEntry {
        public string Name;
        public long Origin;
        public long Length;

        public override string ToString() {
            return string.Format("{0} : origin = 0x{1:X}, length = 0x{2:X}", Name, Origin, Length);
        }
    }

    public class MemoryDescriptor {
        public List<MemoryEntry> Entries = new List<MemoryEntry>();

        public static MemoryDescriptor FromLinkerScript(string filePath) {
            if(string.IsNullOrEmpty(filePath)) {
                return null;
            }

            try {
                var result = new MemoryDescriptor();
                var text = System.IO.File.ReadAllText(filePath);
                result.Entries = ParseMemorySection(ExtractMemorySection(text));
                return result;
            }
            catch {
                return null;
            }
        }

        public static long ParseInteger(string text) {
            if (string.IsNullOrEmpty(text)) {
                return 0;
            }
            text = text.ToLower();

            long multiplier = 1;
            if (text.EndsWith("k")) {
                multiplier = 1024;
                text = text.Substring(0, text.Length - 1);
            } else if (text.EndsWith("m")) {
                multiplier = 1024 * 1024;
                text = text.Substring(0, text.Length - 1);
            }

            if (text.StartsWith("0x")) {//hexadecimal
                return Convert.ToInt64(text, 16) * multiplier;
            }
            if (text.StartsWith("0")) {//octal
                return Convert.ToInt64(text, 8) * multiplier;
            }
            return Convert.ToInt64(text, 10) * multiplier;
        }

        public static List<MemoryEntry> ParseMemorySection(string linkerMemory) {
            List<MemoryEntry> result = new List<MemoryEntry>();
            var memLines = Regex.Matches(linkerMemory, @"[\s]*(.*)[\s]*:[\s]*(.*)[\s]*");

            foreach (Match memLine in memLines) {
               
                var nameSrc = Regex.Match(memLine.Groups[1].Value, @"\s*(\w+)\s*(\([^\)]*\))?");

                var entry = new MemoryEntry();
                entry.Name = nameSrc.Groups[1].Value;

                var par = Regex.Matches(memLine.Groups[2].Value, @"\s*(\w*)\s*=\s*(\w*)\s*");
                //parse values
                foreach (Match match in par) {
                    var paramName = match.Groups[1].Value.ToLower();
                    var paramVal = match.Groups[2].Value;

                    if ((paramName == "origin") || (paramName == "org") || (paramName == "o")) {
                        entry.Origin = ParseInteger(paramVal);
                    }else if ((paramName == "length") || (paramName == "len") || (paramName == "l")) {
                        entry.Length = ParseInteger(paramVal);
                    }
                }

                result.Add(entry);
            }
            return result;
        }

        public static string ExtractMemorySection(string linkerScriptContent) {
            var match = Regex.Match(linkerScriptContent, @"\bMEMORY\b[^{]*{[^\w]*([^}]*)}");
            if (!match.Success) {
                return null;
            }
            return match.Groups[1].Value;
        }

        public MemoryEntry GetEntryByName(string name, bool ignoreCase = true) {
            return Entries?.FirstOrDefault(v => string.Compare(v.Name, name, ignoreCase) == 0);
        }

        public bool IsEntryExists(string name, bool ignoreCase = true) {
            return GetEntryByName(name, ignoreCase) != null;
        }
    }
}
