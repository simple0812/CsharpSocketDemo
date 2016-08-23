using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Directives;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Implement;

namespace DirectiveServer.libs
{
    [VersionList(new Type[] { typeof(V485_1) })]
    public interface IProtocol
    {
        byte[] GenerateDirectiveBuffer(BaseDirective directive);
        DirectiveResult ResolveDirectiveResult(byte[] directive);
    }
}
