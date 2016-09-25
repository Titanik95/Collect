using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collect.Others
{
    static class SmartComAddresses
    {
        static readonly string demoAddress = "mxdemo.ittrade.ru";
        static readonly string mainAddress = "mx.ittrade.ru";
        static readonly string mainAddress2 = "mx2.ittrade.ru";
		static readonly string reserveAddress = "mxr.ittrade.ru";
		static readonly string reserveAddress2 = "mxr2.ittrade.ru";

		static readonly ushort port = 8443;

        static public string GetAddress(ServerType st)
        {
            switch (st)
            {
                case ServerType.Main:
                    return mainAddress;
                case ServerType.Main2:
                    return mainAddress2;
				case ServerType.Reserve:
					return reserveAddress;
				case ServerType.Reserve2:
					return reserveAddress2;
				case ServerType.Demo:
                    return demoAddress;
                default:
                    return mainAddress;
            }
        }

        static public ushort Port
        {
            get
            {
                return port;
            }
        }
    }
}
