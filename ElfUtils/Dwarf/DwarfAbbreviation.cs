using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElfReader.Elf;
using ElfUtils;

namespace ElfReader.Dwarf {
    public class DwarfAbbreviation {
        public List<DwarfAbbreviationEntry> entries = new List<DwarfAbbreviationEntry>();

        public DwarfAbbreviationEntry GetEntryByAbbreviationCode(ulong abbrevCode) {
            return entries.FirstOrDefault(v => v.abbrevCode == abbrevCode);
        }

        public static DwarfAbbreviation Read(ElfFile file, long offset) {
            var stm = file.DataStream;
            stm.Seek(offset, SeekOrigin.Begin);
            DwarfAbbreviation result = new DwarfAbbreviation();

            //read all entries (abbreviation = array on entries)
            while (true) {
                var abbrevCode = stm.ReadLeb128Unsigned();
                if (abbrevCode != 0) {
                    var entry = new DwarfAbbreviationEntry {
                        abbrevCode = abbrevCode,
                        tag = (DwarfConstants.DwarfTag)stm.ReadLeb128Unsigned(),
                        hasChild = (byte)stm.ReadByte()
                    };

                    //read entry attributes
                    while (true) {
                        var dwarfAttribute = stm.ReadLeb128Unsigned();
                        var dwarfForm = stm.ReadLeb128Unsigned();
                        if (dwarfAttribute != 0 || dwarfForm != 0)
                            entry.attributes.Add(new DwarfAbbrevEntryAttribute() {
                                attribute = (DwarfConstants.DwarfAttribute)dwarfAttribute,
                                form = (DwarfConstants.DwarfForm)dwarfForm
                            });
                        else
                            break;
                    }
                    result.entries.Add(entry);

                } else
                    break;
            }
            return result;
        }
    }

    public struct DwarfAbbrevEntryAttribute {
        public DwarfConstants.DwarfAttribute attribute;
        public DwarfConstants.DwarfForm form;
        public override string ToString() {
            return string.Format("{0}: {1}", attribute, form);
        }
    }

    public class DwarfAbbreviationEntry {
        public DwarfConstants.DwarfTag tag;
        public byte hasChild;
        public List<DwarfAbbrevEntryAttribute> attributes = new List<DwarfAbbrevEntryAttribute>();
        public ulong abbrevCode;
    }
}
