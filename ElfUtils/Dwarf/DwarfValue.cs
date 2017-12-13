using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElfUtils;

namespace ElfReader.Dwarf {
    public abstract class DwarfEntryValueBase {
        public readonly DwarfConstants.DwarfAttribute attribute;

        protected DwarfEntryValueBase(DwarfConstants.DwarfAttribute _attrib) {
            attribute = _attrib;
        }

        public virtual ulong AsUlong() {
            return 0;
        }

        public virtual long Aslong() {
            return 0;
        }
    }

    public class DwarfScalarValue<T> : DwarfEntryValueBase {
        public readonly T value;

        public DwarfScalarValue(DwarfConstants.DwarfAttribute attrib, T val)
            : base(attrib) {
            value = val;
        }

        public override ulong AsUlong() {
            return Convert.ToUInt64(value);
        }

        public override long Aslong() {
            return Convert.ToInt64(value);
        }

        public override string ToString() {
            return string.Format("{0} = {1}", attribute, value);
        }
    }

    public class DwarfStringValue : DwarfEntryValueBase {
        public readonly string value;

        public DwarfStringValue(DwarfConstants.DwarfAttribute attrib, string val)
            : base(attrib) {
            value = val;
        }

        public override string ToString() {
            return string.Format("{0} = {1}", attribute, value);
        }
    }

    public class DwarfAttribBoolValue : DwarfEntryValueBase {
        public readonly bool value;

        public DwarfAttribBoolValue(DwarfConstants.DwarfAttribute attrib, bool val)
            : base(attrib) {
            value = val;
        }

        public override string ToString() {
            return string.Format("{0} = {1}", attribute, value);
        }
    }

    public class DwarfAttribBufferValue : DwarfEntryValueBase {
        public readonly byte[] value;

        public DwarfAttribBufferValue(DwarfConstants.DwarfAttribute attrib, byte[] val)
            : base(attrib) {
            value = val;
        }

        public DwarfAttribBufferValue(DwarfConstants.DwarfAttribute attrib, Stream stm, int len)
            : base(attrib) {
            value = new byte[len];
            stm.Read(value, 0, len);
        }

        public override string ToString() {
            return string.Format("{0} = (block of {1} bytes)", attribute, value.Length);
        }
    }
}
