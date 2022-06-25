using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketApp
{
    internal class Key
    {
        public bool refreshRequired { get; set; } = true;
        public string TokenString { get; set; } = string.Empty;

        public int RefreshTimeout { get; set; }
    }
}
