using SmartCOM3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collect
{
    public enum DataStorage
    {
        Local,
        Cloud
    }

    public enum ServerType
    {
        Main,
        Main2,
		Reserve,
		Reserve2,
        Demo
    }

    public static class Enums
    {

        public enum Direction
        {
            Buy,
            Sell
        }

        public static Direction ToDirection(StOrder_Action action)
        {
            Direction dir = Direction.Buy;
            switch (action)
            {
                case StOrder_Action.StOrder_Action_Buy:
                    dir = Direction.Buy;
                    break;
                case StOrder_Action.StOrder_Action_Sell:
                    dir = Direction.Sell;
                    break;
            }
            return dir;
        }

		public static ServerType ToServerType(int serverNumber)
		{
			switch(serverNumber)
			{
				case 0:
					return ServerType.Main;
				case 1:
					return ServerType.Main2;
				case 2:
					return ServerType.Reserve;
				case 3:
					return ServerType.Reserve2;
				default:
					return ServerType.Main;
			}
		}
	}
}
