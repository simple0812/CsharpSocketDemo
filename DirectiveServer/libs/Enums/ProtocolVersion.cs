using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectiveServer.libs.Enums
{
    public enum ProtocolVersion
    {
        V485_1 = 0  // V2， V3 ...
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class VersionAttribute : Attribute
    {
        public ProtocolVersion Version { get;private set; }

        public VersionAttribute(ProtocolVersion version)
        {
            Version = version;
        }
    }

    internal class VersionListAttribute : Attribute
    {
        public Type[] VersionTypeList { get; private set; }

        public VersionListAttribute(Type[] versions)
        {
            VersionTypeList = versions;
        }
    }
}
