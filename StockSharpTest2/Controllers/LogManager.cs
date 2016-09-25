using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Collect.Controllers
{
    class LogManager
    {
        string filePath;

        public LogManager()
        {
            string directory = Environment.CurrentDirectory + Properties.Resources.LogDirectory;
            Directory.CreateDirectory(directory);
            filePath = directory + DateTime.Now.ToShortDateString() + ".txt";
        }

        public void Log(string message)
        {
			using (StreamWriter sw = new StreamWriter(filePath, true))
			{
				sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " - " + message);
			}
        }

        async public void Log(string header, string message)
        {
			using (StreamWriter sw = new StreamWriter(filePath, true))
			{
				await sw.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + " - " + header + " - " + message);
			}
        }
    }
}
