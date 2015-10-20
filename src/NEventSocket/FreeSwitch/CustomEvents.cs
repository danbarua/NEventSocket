using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEventSocket.FreeSwitch
{
    public static class CustomEvents
    {
        public static class Conference
        {
            public const string Maintainence = "conference::maintenance";
        }

        public static class Sofia
        {
            public const string Register = "sofia::register";

            public const string PreRegister = "sofia::pre_register";

            public const string RegisterAttempt = "sofia::register_attempt";

            public const string RegisterFailure = "sofia::register_failure";

            public const string Unregister = "sofia::unregister";

            public const string Expire = "sofia::expire";

            public const string GatewayAdd = "sofia::gateway_add";

            public const string GatewayDelete = "sofia::gateway_delete";

            public const string GatewayState = "sofia::gateway_state";

            public const string RecoverySend = "sofia::recovery_send";

            public const string RecoveryReceive = "sofia::recovery_recv";

            public const string Recoveryrecovered = "sofia::recovery_recovered";

            public const string NotifyRefer = "sofia::notify_refer";

            public const string Reinvite = "sofia::reinvite";

            public const string Error = "sofia::error";
        }
    }
}
