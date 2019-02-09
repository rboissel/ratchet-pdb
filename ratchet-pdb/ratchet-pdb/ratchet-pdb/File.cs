using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.IO.Format
{
    public partial class PDB
    {
        public class File
        {
            string _Path = "";
            public string Path { get { return _Path; } }
            internal File(string Path) { _Path = Path; }

            Line[] _Lines = null;
            public Line[] Lines { get { return _Lines; } }
            List<Line> _PendingLines = new List<Line>();
            internal void AddLines(Line[] Lines) { _PendingLines.AddRange(Lines); }
            internal void FinalizeLines() { _Lines = _PendingLines.ToArray(); _PendingLines.Clear(); }

            public class Line
            {
                uint _Number = 0;
                uint _Offset = 0;
                uint _Info = 0;
                public uint LineNumber { get { return _Number; } }
                public uint Offset { get { return _Offset; } }
                public uint Info { get { return _Info; } }
                uint _ColOffsetStart = 0;
                uint _ColOffsetEnd = 0;
                public uint ColumnOffsetStart { get { return _ColOffsetStart; ; } }
                public uint ColumnOffsetEnd { get { return _ColOffsetEnd; } }

                internal Line(uint number, uint offset, uint info)
                {
                    _Number = number;
                    _Offset = offset;
                    _Info = info;
                }

                internal void AddColumnInfo(uint start, uint end) { _ColOffsetStart = start; _ColOffsetEnd = end; }
            }
        }
    }
}
