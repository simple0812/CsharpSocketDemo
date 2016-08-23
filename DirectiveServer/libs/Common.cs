using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectiveServer.libs
{
    public static class Common
    {
        public static string BytesToString(byte[] bytes)
        {
            return bytes.Aggregate("", (current, t) => current + (Convert.ToString(t, 16).PadLeft(2, '0') + ","));
        }
    }
}
