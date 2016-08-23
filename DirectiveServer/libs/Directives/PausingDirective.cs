using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Implement;

namespace DirectiveServer.libs.Directives
{
    public class PausingDirective : BaseDirective
    {

        public override DirectiveTypeEnum DirectiveType => DirectiveTypeEnum.Pausing;

        public PausingDirective(int targetDeviceId)
        {
            this.TargetDeviceId = targetDeviceId;
        }
    }
}
