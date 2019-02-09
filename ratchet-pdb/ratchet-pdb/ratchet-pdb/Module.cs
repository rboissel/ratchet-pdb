using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.IO.Format
{
    public partial class PDB
    {
        public class Module
        {
            uint _ModuleStreamIndex = 0;
            internal uint ModuleStreamIndex { get { return _ModuleStreamIndex; } }
            uint _CodeviewChunkByteSize = 0;
            internal uint CodeviewChunkSize { get { return _CodeviewChunkByteSize; } }
            uint _C11InfoChunkByteSize = 0;
            internal uint C11InfoChunkSize { get { return _C11InfoChunkByteSize; } }
            uint _C13InfoChunkByteSize = 0;
            internal uint C13InfoChunkSize { get { return _C13InfoChunkByteSize; } }

            internal void SetInternalModuleStreamInfo(uint index, uint codeviewChunkSize, uint C11infoChunkSize, uint C13infoChunkSize)
            {
                _ModuleStreamIndex = index;
                _CodeviewChunkByteSize = codeviewChunkSize;
                _C11InfoChunkByteSize = C11infoChunkSize;
                _C13InfoChunkByteSize = C13infoChunkSize;
            }

            string _Name = "";
            public string Name { get { return _Name; } }

            string _ObjectFileName = "";
            public string ObjectFileName { get { return _ObjectFileName; } }

            File[] _Files = new File[0]; public File[] Files { get { return _Files; } }

            byte[] _CodeViewLineInformation;
            public byte[] CodeViewLineInformation { get { return _CodeViewLineInformation; } }

            internal Module(string Name, string ObjectFileName)
            {
                _Name = Name;
                _ObjectFileName = ObjectFileName;
            }

            internal void SetFiles(File[] Files)
            {
                _Files = Files;
            }

            /*
             * enum DEBUG_S_SUBSECTION_TYPE {
    DEBUG_S_IGNORE = 0x80000000,    // if this bit is set in a subsection type then ignore the subsection contents

    DEBUG_S_SYMBOLS = 0xf1,
    DEBUG_S_LINES,
    DEBUG_S_STRINGTABLE,
    DEBUG_S_FILECHKSMS,
    DEBUG_S_FRAMEDATA,
    DEBUG_S_INLINEELINES,
    DEBUG_S_CROSSSCOPEIMPORTS,
    DEBUG_S_CROSSSCOPEEXPORTS,

    DEBUG_S_IL_LINES,
    DEBUG_S_FUNC_MDTOKEN_MAP,
    DEBUG_S_TYPE_MDTOKEN_MAP,
    DEBUG_S_MERGED_ASSEMBLYINPUT,

    DEBUG_S_COFF_SYMBOL_RVA,
};*/

        enum DebugSubsectionType : uint
        {
            DEBUG_S_IGNORE = 0x80000000,    // if this bit is set in a subsection type then ignore the subsection contents

            DEBUG_S_SYMBOLS = 0xf1,
            DEBUG_S_LINES,
            DEBUG_S_STRINGTABLE,
            DEBUG_S_FILECHKSMS,
            DEBUG_S_FRAMEDATA,
            DEBUG_S_INLINEELINES,
            DEBUG_S_CROSSSCOPEIMPORTS,
            DEBUG_S_CROSSSCOPEEXPORTS,

            DEBUG_S_IL_LINES,
            DEBUG_S_FUNC_MDTOKEN_MAP,
            DEBUG_S_TYPE_MDTOKEN_MAP,
            DEBUG_S_MERGED_ASSEMBLYINPUT,

            DEBUG_S_COFF_SYMBOL_RVA,
        }

            void parseDebugSubsections(byte[] debugSubsection, ref uint offset, uint length)
            {
                uint bound = offset + length;
                while (offset + 8 < bound)
                {
                    parseDebugSubsection(debugSubsection, ref offset);
                }
            }

            void parseDebugSubsection(byte[] debugSubsection, ref uint offset)
            {
                uint debugSubsectionType = BitConverter.ToUInt32(debugSubsection, (int)offset); offset += 4;
                uint length = BitConverter.ToUInt32(debugSubsection, (int)offset); offset += 4;

                if ((debugSubsectionType & (uint)DebugSubsectionType.DEBUG_S_IGNORE) == 0)
                {
                    switch ((DebugSubsectionType)debugSubsectionType)
                    {
                        case DebugSubsectionType.DEBUG_S_LINES:
                            ParseC13CadeViewLines(debugSubsection, offset, length);
                            break;
                    }
                }
                offset += length;
            }

            internal void ParseC13CadeViewLines(byte[] C13LinesInfo, uint offset, uint length)
            {
                uint limit = offset + length;
                uint offCon = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;
                uint segCon = BitConverter.ToUInt16(C13LinesInfo, (int)offset); offset += 2;
                uint flags = BitConverter.ToUInt16(C13LinesInfo, (int)offset); offset += 2;
                uint cbCon = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;

                const uint CV_LINES_HAVE_COLUMNS = 0x0001;

                bool fHasColumn = (flags & CV_LINES_HAVE_COLUMNS) != 0;

                while (offset + 12 < limit)
                {
                    uint fileid = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;
                    uint nLines = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;
                    uint cbFileBlock = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;
                    uint cbLineInfo = (uint)(nLines * ((4 * 2) + (fHasColumn ? 4 : 0)));
                    List<File.Line> lines = new List<File.Line>();
                    File.Line[] line = new File.Line[nLines];
                    for (int n = 0; n < nLines; n++)
                    {
                        uint lineOffset = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;
                        uint lineInfo = BitConverter.ToUInt32(C13LinesInfo, (int)offset); offset += 4;
                        uint linenumStart = lineInfo & ((1 << 24) - 1);
                        uint deltaLineEnd = (lineInfo >> 24) & ((1 << 7) - 1);
                        bool staements = (lineInfo & 0x80000000) != 0;

                        if (linenumStart > 0xf00000)
                        {
                            // Special line info. Not supported yet
                            continue;
                        }

                        lines.Add(new File.Line(linenumStart, lineOffset, lineInfo));
                    }
                    if (fHasColumn)
                    {
                        for (int n = 0; n < nLines; n++)
                        {
                            uint colOffsetStart = BitConverter.ToUInt16(C13LinesInfo, (int)offset); offset += 2;
                            uint colOffsetEnd = BitConverter.ToUInt16(C13LinesInfo, (int)offset); offset += 2;
                        }
                    }
                    _Files[fileid >> 3].AddLines(lines.ToArray()); 
                }

            }

            internal void ParseModuleInfoStream(Ratchet.IO.Format.MSF.Stream moduleInfoStream)
            {
                byte[] tempBuffer = new byte[4];
                byte[] codeViewInfo = new byte[_CodeviewChunkByteSize - 4];
                byte[] C11LinesInfo = new byte[_C11InfoChunkByteSize];
                byte[] C13LinesInfo = new byte[_C13InfoChunkByteSize];
                moduleInfoStream.Read(tempBuffer, 0, tempBuffer.Length); // Signature ignored
                moduleInfoStream.Read(codeViewInfo, 0, codeViewInfo.Length); // Signature ignored
                moduleInfoStream.Read(C11LinesInfo, 0, C11LinesInfo.Length); // C11 style lines info
                moduleInfoStream.Read(C13LinesInfo, 0, C13LinesInfo.Length); // C13 style lines info
                if (C13LinesInfo.Length != 0) { _CodeViewLineInformation = C13LinesInfo; }
                else { _CodeViewLineInformation = C11LinesInfo; }

                uint ignored = 0;
                parseDebugSubsections(C13LinesInfo, ref ignored, (uint)C13LinesInfo.Length);

                foreach (var file in _Files) { file.FinalizeLines(); }
            }
        }
    }
}
