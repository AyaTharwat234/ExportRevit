using Elibre.Net.Core;
using System;

namespace EFRvt
{
    public class RoofLine
    {
        private Line3d _line;
        private bool _taken;

        public Line3d Line
        {
            get
            {
                return _line;
            }

            set
            {
                _line = value;
            }
        }

        public bool Taken
        {
            get
            {
                return _taken;
            }

            set
            {
                _taken = value;
            }
        }

        public RoofLine(Line3d line, bool taken)
        {
            _line = line;
            _taken = taken;
        }
    }
}
