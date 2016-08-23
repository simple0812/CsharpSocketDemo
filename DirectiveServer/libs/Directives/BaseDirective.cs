using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Implement;

namespace DirectiveServer.libs.Directives
{
    public abstract class BaseDirective
    {
        public int DirectiveId { get; set; }
        public int TargetDeviceId { get; set; }
        private static int directiveId = 0;
        public abstract DirectiveTypeEnum DirectiveType { get; }

        protected BaseDirective()
        {
            Interlocked.Increment(ref directiveId);
            
            if (directiveId >= 0xffff)
            {
                directiveId = 0;
            }
            
            DirectiveId = directiveId;
        }

    }
}
