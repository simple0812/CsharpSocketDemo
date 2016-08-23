using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Implement;

namespace DirectiveServer.libs.Directives
{
    public class TryStartDirective : BaseDirective
    {
        public override DirectiveTypeEnum DirectiveType => DirectiveTypeEnum.TryStart;

        public double FlowRate { get; set; }
        public double Volume { get; set; }
        public DirectionEnum Direction { get; set; }

        public TryStartDirective(int targetDeviceId, double flowRate, double volume, DirectionEnum direction)
        {
            this.TargetDeviceId = targetDeviceId;
            this.FlowRate = flowRate;
            this.Volume = volume;
            this.Direction = direction;
        }
    }
}
