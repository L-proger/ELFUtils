using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElfReader.Elf;
using ElfUtils;

namespace ElfReader {

    public class MemoryUsage {
        public ulong Base;
        public ulong Length;
        public ulong BytesUsed;
        public IEnumerable<ElfSection> Sections;
    }

    public class MemoryProfile {
        private Dictionary<ElfSection, string> sectionMemoryTable;
        private Dictionary<ElfSection, List<string>> sectionMemoryTableEx;
        private Dictionary<string, MemoryUsage> memoryUsageTable;

        public string[] MemoryNames {
            get { return GetMemoryNames(); }
        }

        public DwarfData DwarfData {
            get;
        }

        public const string flashSectionName = "FLASH";
        public const string ramSectionName = "SRAM";
        public const string unknownMemoryName = "NOPROG";

        public MemoryProfile(DwarfData dwData, MemoryDescriptor memoryDescriptor = null) {
            DwarfData = dwData;
            DetectSectionsRamType(memoryDescriptor);

            //build memory usage table
            memoryUsageTable = new Dictionary<string, MemoryUsage>();
            var memories = GetMemoryNames();
            if(memories != null) {
                foreach (var memory in memories) {
                    var sections = GetSectionsByMemoryEx(memory);
                    var mem = new MemoryUsage {Sections = sections};
                    if(sections != null && sections.Length != 0) {
                        mem.BytesUsed = sections.Aggregate(0UL, (current, section) => current + section.Header.size);

                        if(memoryDescriptor != null && memoryDescriptor.IsEntryExists(memory)) {
                            var entry = memoryDescriptor.GetEntryByName(memory);
                            mem.Base = (ulong)entry.Origin;
                            mem.Length = (ulong)entry.Length;
                        } else {

                        }
                    }


                    memoryUsageTable[memory] = mem;
                }
            }
        }



        public ElfSection[] GetSectionsByMemoryEx(string memoryName) {
            return sectionMemoryTableEx.Where(v => v.Value.Contains(memoryName)).Select(v => v.Key).ToArray();
        }

        public string[] GetMemoryNames() {
            return sectionMemoryTable.Values.Distinct().ToArray();
        }

        private void DetectMemoryTypesEx() {
            if(sectionMemoryTable == null || sectionMemoryTable.Count == 0 || DwarfData == null) {
                return;
            }

            //clone memory table
            sectionMemoryTableEx = new Dictionary<ElfSection, List<string>>();
            foreach (var key in sectionMemoryTable.Keys) {
                sectionMemoryTableEx[key] = new List<string> { sectionMemoryTable[key] };
            }

            //First try to detect by name
            string flashName = sectionMemoryTable.Values.FirstOrDefault(v => v.ToLower().Contains("flash"));
            //If can't detect by name - try to detect by .text section
            if(string.IsNullOrEmpty(flashName)) {
                var dataSection = DwarfData.File.GetSection(".text");
                if(dataSection != null) {
                    sectionMemoryTable.TryGetValue(dataSection, out flashName);
                }
            }

            if(string.IsNullOrEmpty(flashName)) {
                return;
            }

            foreach (var pair in sectionMemoryTableEx) {
                if(pair.Key.Header.type != Elf32SectionType.SHT_NOBITS && !pair.Value.Contains(unknownMemoryName) && !pair.Value.Contains(flashName)) {
                    pair.Value.Add(flashName);
                }
            }
        }

        private void DetectSectionsRamType(MemoryDescriptor mcu = null) {
            sectionMemoryTable = new Dictionary<ElfSection, string>();
            var allocSections = new List<ElfSection>();

            //get alloc allocSections
            foreach(var section in DwarfData.File.Sections) {
                if((section.Header.flags & Elf32SectionFlags.SHF_ALLOC) == 0) {
                    sectionMemoryTable[section] = unknownMemoryName;
                } else {
                    allocSections.Add(section);
                }
            }

            if(mcu == null) {
                DetectSectionsRamTypeAuto(allocSections);
            } else {
                DetectSectionsRamTypeByMcu(allocSections, mcu);
            }
            DetectMemoryTypesEx();
        }

        private void DetectSectionsRamTypeAuto(List<ElfSection> allocSections) {
            const string codeSectionName = ".text";
            const string initializedDataSectionName = ".data";
            const string zeroDataSectionName = ".bss";

            //sort by section ram address
            allocSections.Sort(((d0, sd) => d0.Header.addr.CompareTo(sd.Header.addr)));

            //search for BSS section
            ElfSection bssSection = allocSections.FirstOrDefault(
                section => !section.Header.UsingFlashMemory || (section.Name == zeroDataSectionName));

            //if BSS not found - use initialized data section
            if(bssSection == null) {
                foreach(var section in allocSections) {
                    if(section.Name == initializedDataSectionName) {
                        bssSection = section;
                        break;
                    }
                }
            }

            //search code section
            ElfSection codeSection = allocSections.FirstOrDefault(sd => !string.IsNullOrEmpty(codeSectionName) && sd.Name == codeSectionName);

            //if code section not found = use any not bss section
            if(codeSection == null) {
                foreach(var sd in allocSections) {
                    if(sd != bssSection) {
                        codeSection = sd;
                        break;
                    }
                }
            }

            //find biggest gap between sections
            long maxSectionsDistance = 0;
            for(int i = 1; i < allocSections.Count; ++i)
                maxSectionsDistance = Math.Max(maxSectionsDistance, (allocSections[i].Header.addr - (allocSections[i - 1].Header.addr + allocSections[i - 1].Header.addr)));

            const long flashRamMinGap = 1024 * 1024;

            foreach(var section in allocSections) {
                if(codeSection == null || bssSection == null) {
                    sectionMemoryTable[section] = flashSectionName;
                } else {
                    //if really different address sections found
                    if(maxSectionsDistance >= flashRamMinGap) {
                        sectionMemoryTable[section] = Math.Abs((int)section.Header.addr - (int)bssSection.Header.addr) >=
                                  Math.Abs((int)section.Header.addr - (int)codeSection.Header.addr)
                            ? flashSectionName
                            : ramSectionName;
                    } else {
                        sectionMemoryTable[section] = ramSectionName;
                    }
                }
            }

        }

        private void DetectSectionsRamTypeByMcu(IEnumerable<ElfSection> allocSections, MemoryDescriptor mcu) {
            foreach(var section in allocSections) {
                sectionMemoryTable[section] = unknownMemoryName;
                foreach(var memoryEntry in mcu.Entries) {
                    if((section.Header.addr >= memoryEntry.Origin) &&
                        (section.Header.addr <= (memoryEntry.Origin + memoryEntry.Length))) {
                        sectionMemoryTable[section] = memoryEntry.Name;
                        break;
                    }
                }
            }
        }

        public ElfSection[] GetFlashSections() {
            return DwarfData.File.Sections.Where(IsFlashSection).ToArray();
        }

        public bool IsFlashSection(ElfSection section) {
            if(section == null) {
                return false;
            }
            return section.Header.type != Elf32SectionType.SHT_NOBITS && section.Header.addr != 0 &&
                   ((section.Header.flags & Elf32SectionFlags.SHF_ALLOC) != 0);
        }

        public ElfSection[] GetProgramSections() {
            if(DwarfData == null) {
                return null;
            }
            return DwarfData.File.Sections.Where(v => v.Header.addr != 0 && ((v.Header.flags & Elf32SectionFlags.SHF_ALLOC) != 0)).ToArray();
        }

        public string GetSectionMemoryName(ElfSection section) {
            if(sectionMemoryTable.ContainsKey(section)) {
                return sectionMemoryTable[section];
            }
            return unknownMemoryName;
        }
    }
}
