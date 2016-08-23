using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Enums;

namespace DirectiveServer.libs.Directives
{

    public class DirectiveData
    {
        public int DeviceId { get; set; }
        public int DirectiveId { get; set; }
        public DirectiveTypeEnum DirectiveType { get; set; }
        public int TimeInterval { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int DeviceStatus { get; set; }
        public DateTime? DeviceTime { get; set; }
    }

    public class PumpDirectiveData : DirectiveData
    {
        public double FlowRate { get; set; }
        public DirectionEnum Direction { get; set; }
    }

    public class RockerDirectiveData : DirectiveData
    {
        public double Angle { get; set; }
        public double Speed { get; set; }
    }

    public class TemperatureDirectiveData : DirectiveData
    {
        public  double Temperature { get; set; }
    }

    public class DirectiveResult
    {
        public bool Status { get; set; }
        public DirectiveTypeEnum SourceDirectiveType { get; set; }
        public DirectiveData Data { get; set; }
    }

    public enum DirectionEnum
    {
        In = 0,
        Out
    }
}
