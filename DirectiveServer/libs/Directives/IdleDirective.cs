using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Implement;

namespace DirectiveServer.libs.Directives
{
    public class IdleDirective : BaseDirective
    {
        public override DirectiveTypeEnum DirectiveType => DirectiveTypeEnum.Idle;

        public IdleDirective(int targetDeviceId)
        {
            this.TargetDeviceId = targetDeviceId;
        }
    }
}
