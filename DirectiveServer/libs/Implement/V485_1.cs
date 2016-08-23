using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectiveServer.libs.Directives;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Helper;

namespace DirectiveServer.libs.Implement
{
    [Version(ProtocolVersion.V485_1)]
    public class V485_1 : IProtocol
    {

        public byte[] GenerateDirectiveBuffer(BaseDirective directive)
        {
            switch (directive.DirectiveType)
            {
                case DirectiveTypeEnum.Idle:
                    return GenerateIdleBuffer(directive as IdleDirective);

                case DirectiveTypeEnum.TryStart:
                    return GenerateTryStartBuffer(directive as TryStartDirective);

                case DirectiveTypeEnum.TryPause:
                    return GenerateTryPauseBuffer(directive as TryPauseDirective);

                case DirectiveTypeEnum.Close:
                    return GenerateCloseBuffer(directive as CloseDirective);

                case DirectiveTypeEnum.Running:
                    return GenerateRunningBuffer(directive as RunningDirective);

                case DirectiveTypeEnum.Pausing:
                    return GeneratePausingBuffer(directive as PausingDirective);

                default:
                    return null;
            }
        }

        public DirectiveResult ResolveDirectiveResult(byte[] bytes)
        {
            if (bytes.Length <= 2) return null;
            DirectiveTypeEnum directiveType = (DirectiveTypeEnum) bytes[1];
            switch (directiveType)
            {
                case DirectiveTypeEnum.Idle:
                    return ParseIdleResultData(bytes);

                case DirectiveTypeEnum.TryStart:
                    return ParseTryStartResultData(bytes);

                case DirectiveTypeEnum.TryPause:
                    return ParseTryPauseResultData(bytes);

                case DirectiveTypeEnum.Close:
                    return ParseStopResultData(bytes);

                case DirectiveTypeEnum.Running:
                    return ParseRunningResultData(bytes);

                case DirectiveTypeEnum.Pausing:
                    return ParsePausingResultData(bytes);

                default:
                    return null;
            }
        }

        private byte[] GenerateCloseBuffer(CloseDirective directive)
        {
            return GetCommonBufferData(directive);
        }

        private byte[] GenerateTryStartBuffer(TryStartDirective directive)
        {
            if (null == directive)
                return new byte[0];

            IList<byte> list = new List<byte>();

            list.Add((byte)directive.TargetDeviceId);
            list.Add((byte)directive.DirectiveType);
            list = list.Concat(DirectiveHelper.ParseNumberTo2Bytes(directive.FlowRate)).ToList();
            list = list.Concat(DirectiveHelper.ParseNumberTo2Bytes(directive.Volume)).ToList();
            list.Add((byte)directive.Direction);
            list = list.Concat(DirectiveHelper.ParseNumberTo2Bytes(directive.DirectiveId)).ToList();
            list = list.Concat(DirectiveHelper.GenerateCheckCode(list.ToArray())).ToList();

            return list.ToArray();
        }

        private byte[] GenerateIdleBuffer(IdleDirective directive)
        {
            return GetCommonBufferData(directive);
        }

        private byte[] GenerateTryPauseBuffer(TryPauseDirective directive)
        {
            return GetCommonBufferData(directive);
        }

        private byte[] GenerateRunningBuffer(RunningDirective directive)
        {
            return GetCommonBufferData(directive);
        }

        private byte[] GeneratePausingBuffer(PausingDirective directive)
        {
            return GetCommonBufferData(directive);
        }

        private static byte[] GetCommonBufferData(BaseDirective directive)
        {
            if (null == directive)
                return new byte[0];

            IList<byte> list = new List<byte>();

            list.Add((byte)directive.TargetDeviceId);
            list.Add((byte)directive.DirectiveType);
            list = list.Concat(DirectiveHelper.ParseNumberTo2Bytes(directive.DirectiveId)).ToList();
            list = list.Concat(DirectiveHelper.GenerateCheckCode(list.ToArray())).ToList();

            return list.ToArray();
        }

        //1 1 2 2 2
        private DirectiveResult ParseIdleResultData(byte[] bytes)
        {
            var ret = new DirectiveResult();
            if (!IsValidationResult(bytes, 8))
            {
                ret.Status = false;
                return ret;
            }

            ret.Status = true;
            var data = new PumpDirectiveData();

            data.DeviceId = bytes[0];
            data.DirectiveType = (DirectiveTypeEnum)bytes[1];
            data.FlowRate = DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());
            data.DirectiveId = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(4).Take(2).ToArray());

            ret.Data = data;
            ret.SourceDirectiveType = DirectiveTypeEnum.Idle;

            return ret;
        }

        //1 1 2 2 1 2 2
        private DirectiveResult ParseTryStartResultData(byte[] bytes)
        {
            var ret = new DirectiveResult();
            if (!IsValidationResult(bytes, 11))
            {
                ret.Status = false;
                return ret;
            }

            ret.Status = true;
            var data = new PumpDirectiveData();

            data.DeviceId = bytes[0];
            data.DirectiveType = (DirectiveTypeEnum)bytes[1];
            data.FlowRate = DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());
            data.Direction = (DirectionEnum)bytes.Skip(6).Take(1).FirstOrDefault();
            data.DirectiveId = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(7).Take(2).ToArray());

            ret.SourceDirectiveType = DirectiveTypeEnum.TryStart;
            ret.Data = data;

            return ret;
        }

        //1 1 2 2
        private DirectiveResult ParseTryPauseResultData(byte[] bytes)
        {
            var ret = new DirectiveResult();

            if (!IsValidationResult(bytes, 6))
                {
                ret.Status = false;
                return ret;
            }

            ret.Status = true;
            var data = new PumpDirectiveData();

            data.DeviceId = bytes[0];
            data.DirectiveType = (DirectiveTypeEnum)bytes[1];
            data.DirectiveId = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());

            ret.SourceDirectiveType = DirectiveTypeEnum.TryPause;
            ret.Data = data;

            return ret;
        }

        //1 1 2 2
        private DirectiveResult ParseStopResultData(byte[] bytes)
        {
            var ret = new DirectiveResult();
            if (!IsValidationResult(bytes, 6))
            {
                ret.Status = false;
                return ret;
            }

            ret.Status = true;
            var data = new PumpDirectiveData();

            data.DeviceId = bytes[0];
            data.DirectiveType = (DirectiveTypeEnum)bytes[1];
            data.DirectiveId = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());

            ret.SourceDirectiveType = DirectiveTypeEnum.Close;
            ret.Data = data;

            return ret;
        }
        //1 1 2 2 1 1 2 2
        private DirectiveResult ParseRunningResultData(byte[] bytes)
        {
            var ret = new DirectiveResult();
            if (!IsValidationResult(bytes, 12))
                {
                ret.Status = false;
                return ret;
            }

            ret.Status = true;
            var data = new PumpDirectiveData();

            data.DeviceId = bytes[0];
            data.DirectiveType = (DirectiveTypeEnum)bytes[1];
            data.TimeInterval = (int) DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());
            data.FlowRate = DirectiveHelper.Parse2BytesToNumber(bytes.Skip(4).Take(2).ToArray());
            data.DeviceStatus = bytes.Skip(6).Take(1).FirstOrDefault();
            data.Direction = (DirectionEnum) bytes.Skip(7).Take(1).FirstOrDefault();
            data.DirectiveId = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(8).Take(2).ToArray());

            ret.SourceDirectiveType = DirectiveTypeEnum.Running;
            ret.Data = data;

            return ret;
        }

        //1 1 2 2 1 2 2
        private DirectiveResult ParsePausingResultData(byte[] bytes)
        {
            var ret = new DirectiveResult();
            if (!IsValidationResult(bytes, 11))
            {
                ret.Status = false;
                return ret;
            }

            ret.Status = true;
            var data = new PumpDirectiveData();

            data.DeviceId = bytes[0];
            data.DirectiveType = (DirectiveTypeEnum)bytes[1];
            data.TimeInterval = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());
            data.FlowRate = DirectiveHelper.Parse2BytesToNumber(bytes.Skip(4).Take(2).ToArray());
            data.DeviceStatus = bytes.Skip(6).Take(1).FirstOrDefault();
            data.DirectiveId = (int)DirectiveHelper.Parse2BytesToNumber(bytes.Skip(7).Take(2).ToArray());

            ret.SourceDirectiveType = DirectiveTypeEnum.Pausing;
            ret.Data = data;

            return ret;
        }

        private bool IsValidationResult(byte[] bytes, int len)
        {
            if (bytes.Length != len || len <= 2)
            {
                return false;
            }

            var codes = DirectiveHelper.GenerateCheckCode(bytes.Take(len - 2).ToArray());

            if (codes == null || codes.Length != 2 || codes[0] != bytes[len-2] || codes[1] != bytes[len-1])
            {
                return false;
            }

            return true;
        }

    }
}
