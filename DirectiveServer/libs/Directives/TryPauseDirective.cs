using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Implement;

namespace DirectiveServer.libs.Directives
{
    public class TryPauseDirective : BaseDirective
    {
        public override DirectiveTypeEnum DirectiveType => DirectiveTypeEnum.TryPause;

        public TryPauseDirective(int targetDeviceId)
        {
            this.TargetDeviceId = targetDeviceId;
        }

    }
}
