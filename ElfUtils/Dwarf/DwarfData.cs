using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ElfReader.Dwarf;
using ElfReader.Elf;

namespace ElfUtils {

    public class DwarfData {
        public readonly ElfFile _file;
        public DwarfCompilationUnit[] compilationUnits;
        public List<DwarfCompilationUnitItem> globalItems = new List<DwarfCompilationUnitItem>();

        public DwarfData(ElfFile file) {
            _file = file;
            //extract compilation units
            compilationUnits = DwarfCompilationUnit.ReadCompilationUnits(file);

            //extract abbreviations
            var abbrevSection = file.GetSection(DwarfSections.Abbrev);
            int id = 0;
            foreach(var cu in compilationUnits) {
                cu.Abbrev = DwarfAbbreviation.Read(file, (long)cu.AbbrevOffset + abbrevSection.Header.offset);
                cu.ReadValues(file);
                id++;
            }

            //build file items tables
            var nameTable = new Dictionary<string, DwarfCompilationUnitItem>();
            var pcTable = new Dictionary<ulong, DwarfCompilationUnitItem>();
            foreach(var unit in compilationUnits) {

                foreach(var dwarfAbbreviationEntry in unit.Abbrev.entries) {
                    foreach(var attr in dwarfAbbreviationEntry.attributes) {
                        if(attr.attribute == DwarfConstants.DwarfAttribute.DW_AT_MIPS_linkage_name) {
                            Console.WriteLine("LN");
                        }
                    }
                }

                foreach(var item in unit.Items) {
                    if(item.Pc != 0) {
                        pcTable[item.Pc] = item;
                    }
                    if(!string.IsNullOrEmpty(item.Name)) {
                        nameTable[item.Name] = item;
                    }
                }
            }


            var symbols = file.Sections.SelectMany(v => v.Symbols).ToArray();
            var tmpItems = new List<DwarfCompilationUnitItem>();
            foreach(var symbol in symbols) {
                DwarfCompilationUnitItem item = null;
                //find item corresponding to symbol
                if(!string.IsNullOrEmpty(symbol.Name) && nameTable.TryGetValue(symbol.Name, out item) ||
                    symbol.Header.ValueAddr != 0 && pcTable.TryGetValue(symbol.Header.ValueAddr, out item)) {

                    if((int)item.Size == 0) {
                        item.Size = symbol.Header.Size;
                    }

                    if((long)item.Pc == 0L) {
                        item.Pc = symbol.Header.ValueAddr;
                    }

                    //dictionary3[mem.pc] = true;
                    if(string.IsNullOrEmpty(item.Name))
                        item.Name = symbol.Name;
                } else {
                    if(!string.IsNullOrEmpty(symbol.Name)) {
                        item = new DwarfCompilationUnitItem(symbol);
                    }
                }

                if(item != null && item.Size != 0 && !string.IsNullOrEmpty(item.Name) && item.HasLine()) {
                    tmpItems.Add(item);
                }
            }

            //sort members by pc
            tmpItems.Sort(((mem, member) => mem.Pc.CompareTo(member.Pc)));
            foreach(var unitItem in tmpItems) {
                if(unitItem.Size != 0) {
                    DwarfCompilationUnitItem item = null;
                    if(globalItems.Count > 0) {
                        item = globalItems[globalItems.Count - 1];
                    }

                    if(item != null && (item.Pc == unitItem.Pc)) {
                        if(!item.HasLine() && unitItem.HasLine())
                            globalItems[globalItems.Count - 1] = unitItem;
                        else
                            continue;
                    } else if(item != null && item.Pc <= unitItem.Pc && item.Pc + item.Size > unitItem.Pc) {
                        item.Size = (uint)(unitItem.Pc - item.Pc);
                    }


                    globalItems.Add(unitItem);
                }
            }

            //fill sections with items
            foreach(var item in globalItems) {
                if(item.Size != 0) {
                    var index = file.GetSectionId(item.Pc);
                    if(index >= 0) {
                        var section = file.Sections[index];
                        section.Items.Add(item);
                    }
                }
            }

            foreach(var section in file.Sections) {
                ulong itemsSize = section.Items.Aggregate<DwarfCompilationUnitItem, ulong>(0, (current, item) => current + item.Size);
                var diff = section.Header.size - itemsSize;
                if(diff != 0) {
                    section.Items.Add(new DwarfCompilationUnitItem() { Name = "other", Size = diff });
                }
            }
        }

        public ElfFile File
        {
            get { return _file; }
        }
    }

    public static class DwarfSections {
        public const string DebugInfo = ".debug_info";
        public const string DebugLine = ".debug_line";
        public const string Abbrev = ".debug_abbrev";
        public const string DebugStr = ".debug_str";
        public const string StringTable = ".strtab";
    }

    public class DwarfSymbolDescpription {
        public DwarfAbbreviationEntry Abbreviation;
        public List<DwarfEntryValueBase> Values = new List<DwarfEntryValueBase>(); 
    }

    
}
