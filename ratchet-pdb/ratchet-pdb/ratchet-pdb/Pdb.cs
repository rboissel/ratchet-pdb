using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.IO.Format
{
    public partial class PDB
    {
        uint _Version; public uint Version { get { return _Version; } }
        uint _Signature; public uint Signature { get { return _Signature; } }
        uint _Age; public uint Age { get { return _Age; } }
        Guid _Guid; public Guid Guid { get { return _Guid; } }
        Version _ToolchainVersion; public Version ToolchainVersion { get { return _ToolchainVersion; } }
        bool _WasIncrementallyLinked; public bool WasIncrementallyLinked { get { return _WasIncrementallyLinked; } }
        bool _ArePrivateSymbolsStripped; public bool ArePrivateSymbolsStripped { get { return _ArePrivateSymbolsStripped; } }

        Module[] _Modules = new Module[0]; public Module[] Modules { get { return _Modules; } }


        UInt32 _ModInfoSize = 0;
        UInt32 _SectionContributionSize = 0;
        UInt32 _SectionMapSize = 0;
        UInt32 _SourceInfoSize = 0;


        public enum MachineType
        {
            x86 = 0x014C,
            PPCLE = 0x01F0,
            IA64 = 0x0200,
            CEF = 0xCEF,
            AMD64 = 0x8664,
            ARM64 = 0xAA64,
            CEE = 0xC0EE
        }
        MachineType _Machine; public MachineType Machine { get { return _Machine; } }

        void ParsePdbStream(Ratchet.IO.Format.MSF.Stream pdbStream)
        {
            byte[] buffer = new byte[4 * 3];
            pdbStream.Read(buffer, 0, buffer.Length);
            _Version = BitConverter.ToUInt32(buffer, 0);
            _Signature = BitConverter.ToUInt32(buffer, 4);
            _Age = BitConverter.ToUInt32(buffer, 8);
            byte[] bufferGuid = new byte[128 / 8];
            pdbStream.Read(bufferGuid, 0, bufferGuid.Length);
            _Guid = new Guid(bufferGuid);
        }


        void ParseDbiStreamModuleInfo(Ratchet.IO.Format.MSF.Stream dbiStream, UInt32 offset, UInt32 size)
        {
            List<Module> modules = new List<Module>();
            dbiStream.Seek(offset, System.IO.SeekOrigin.Begin);
            byte[] moduleInfoBuffer = new byte[size];
            dbiStream.Read(moduleInfoBuffer, 0, moduleInfoBuffer.Length);
            UInt32 offsetInChunk = 0;
            while (offsetInChunk + 4 < size)
            {
                offsetInChunk += 4; // not used;
                UInt16 Section = BitConverter.ToUInt16(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 2;
                offsetInChunk += 2; // padding;
                UInt32 Offset = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;
                UInt32 Size = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;
                UInt32 Characteristics = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;
                UInt16 moduleIndex = BitConverter.ToUInt16(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 2;
                offsetInChunk += 2; // padding;
                UInt32 DataCrc = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;
                UInt32 RelocCrc = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;

                UInt16 Flags = BitConverter.ToUInt16(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 2;
                UInt16 ModuleSymStream = BitConverter.ToUInt16(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 2;


                UInt32 CodeViewSectionByteSize = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;
                UInt32 C11ByteSize = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;
                UInt32 C13ByteSize = BitConverter.ToUInt32(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 4;

                UInt16 SourceFileCount = BitConverter.ToUInt16(moduleInfoBuffer, (int)(offsetInChunk)); offsetInChunk += 2;
                offsetInChunk += 2; // padding;
                offsetInChunk += 4; // padding;
                offsetInChunk += 4; // not used;
                offsetInChunk += 4; // not used;

                string moduleName = "";

                try
                {
                    for (; moduleInfoBuffer[offsetInChunk] != 0; offsetInChunk++)
                    {
                        moduleName += (char)moduleInfoBuffer[offsetInChunk];
                    }
                    offsetInChunk++;
                }
                catch { }


                string objFileName = "";

                try
                {
                    for (; moduleInfoBuffer[offsetInChunk] != 0; offsetInChunk++)
                    {
                        objFileName += (char)moduleInfoBuffer[offsetInChunk];
                    }
                    offsetInChunk++;
                }
                catch { }

                Module module = new Module(moduleName, objFileName);
                modules.Add(module);
                offsetInChunk = ((offsetInChunk + 3) / 4) * 4;

                module.SetInternalModuleStreamInfo(ModuleSymStream, CodeViewSectionByteSize, C11ByteSize, C13ByteSize);
            }

            _Modules = modules.ToArray();
        }

        void ParseDbiStreamSectionContribution(Ratchet.IO.Format.MSF.Stream dbiStream, UInt32 offset)
        {

        }

        void ParseDbiStreamSectionMap(Ratchet.IO.Format.MSF.Stream dbiStream, UInt32 offset)
        {

        }

        void ParseDbiStreamFileInfo(Ratchet.IO.Format.MSF.Stream dbiStream, UInt32 offset, UInt32 size)
        {
            byte[] buffer = new byte[(2 * 2)];
            dbiStream.Seek(offset, System.IO.SeekOrigin.Begin);

            dbiStream.Read(buffer, 0, buffer.Length);
            UInt16 NumModules = BitConverter.ToUInt16(buffer, 0);
            UInt16 NumSourceFiles = 0;

            byte[] modIndiceBuffer = new byte[2 * NumModules];
            dbiStream.Read(modIndiceBuffer, 0, modIndiceBuffer.Length);

            byte[] ModFileCountsBuffer = new byte[2 * NumModules];
            dbiStream.Read(ModFileCountsBuffer, 0, ModFileCountsBuffer.Length);

            for (int n = 0; n < NumModules; n++)
            {
                UInt16 numFileInModule = BitConverter.ToUInt16(ModFileCountsBuffer, 2 * n);
                NumSourceFiles += numFileInModule;
            }

            byte[] FileNameOffsetsBuffer = new byte[4 * NumSourceFiles];
            dbiStream.Read(FileNameOffsetsBuffer, 0, FileNameOffsetsBuffer.Length);

            byte[] FileNameBuffer = new byte[(offset + size) - dbiStream.Position];
            dbiStream.Read(FileNameBuffer, 0, FileNameBuffer.Length);

            {
                int fcount = 0;
                for (int m = 0; m < NumModules; m++)
                {
                    UInt16 numFileInModule = BitConverter.ToUInt16(ModFileCountsBuffer, 2 * m);
                    File[] files = new File[numFileInModule];
                    for (int n = 0; n < numFileInModule; n++)
                    {
                        try
                        {
                            string name = "";
                            UInt32 fileNameOffset = BitConverter.ToUInt16(FileNameOffsetsBuffer, 4 * fcount);
                            for (UInt32 x = fileNameOffset; FileNameBuffer[x] != 0; x++)
                            {
                                name += (char)FileNameBuffer[x];
                            }
                            File file = new File(name);
                            files[n] = file;
                        }
                        catch { }
                        fcount++;
                    }

                    _Modules[m].SetFiles(files);

                }
            }
        }

        void ParseDbiStream(Ratchet.IO.Format.MSF.Stream dbiStream)
        {
            byte[] buffer = new byte[(4 * 11) + (2 * 8)];
            dbiStream.Read(buffer, 0, buffer.Length);
            UInt16 buildNumber = BitConverter.ToUInt16(buffer, 4 * 3 + 2);
            UInt16 toolchainMinor = (UInt16)(buildNumber & 0x00FF);
            UInt16 toolchainMajor = (UInt16)((buildNumber & 0x7F00) >> 8);
            _ToolchainVersion = new Version(toolchainMajor, toolchainMinor);
            bool newFormat = (buildNumber & 0x8000) != 0;
            if (!newFormat) { throw new Exception("PDB format not supported"); }
            UInt16 flags = BitConverter.ToUInt16(buffer, 4 * 11 + 6 * 2);
            _WasIncrementallyLinked = (flags & 0x1) != 0;
            _ArePrivateSymbolsStripped = (flags & 0x2) != 0;
            UInt16 machine = BitConverter.ToUInt16(buffer, 4 * 11 + 7 * 2);
            _Machine = (MachineType)machine;

            _ModInfoSize = BitConverter.ToUInt32(buffer, 4 * 3 + 6 * 2);
            _SectionContributionSize = BitConverter.ToUInt32(buffer, 4 * 4 + 6 * 2);
            _SectionMapSize = BitConverter.ToUInt32(buffer, 4 * 5 + 6 * 2);
            _SourceInfoSize = BitConverter.ToUInt32(buffer, 4 * 6 + 6 * 2);

            uint headerSize = 4 * 12 + 2 * 8;
            ParseDbiStreamModuleInfo(dbiStream, headerSize, _ModInfoSize);
            ParseDbiStreamSectionContribution(dbiStream, headerSize + _ModInfoSize);
            ParseDbiStreamSectionMap(dbiStream, headerSize + _ModInfoSize + _SectionContributionSize);
            ParseDbiStreamFileInfo(dbiStream, headerSize + _ModInfoSize + _SectionContributionSize + _SectionMapSize, _SourceInfoSize);
        }

        void ParseCodeViewInfo()
        {

        }


        public static PDB Open(System.IO.Stream Stream)
        {
            PDB pdb = new PDB();
            Ratchet.IO.Format.MSF.Stream[] _MSF =  Ratchet.IO.Format.MSF.Open(Stream);
            Ratchet.IO.Format.MSF.Stream pdbStream = _MSF[1];
            Ratchet.IO.Format.MSF.Stream dbiStream = _MSF[3];
            pdb.ParsePdbStream(pdbStream);
            pdb.ParseDbiStream(dbiStream);

            foreach (Module module in pdb._Modules)
            {
                if (module.ModuleStreamIndex >= _MSF.Length) { continue; }
                Ratchet.IO.Format.MSF.Stream moduleInfoStream = _MSF[module.ModuleStreamIndex];
                module.ParseModuleInfoStream(moduleInfoStream);
            }


            pdb.ParseCodeViewInfo();

            return pdb;
        }
    }
}
