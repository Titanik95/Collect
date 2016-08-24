using Collect.Models;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Data;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Text;
using Collect.Controllers;

namespace Collect.Views
{
    /// <summary>
    /// Окно с параметрами приложения
    /// </summary>
    public partial class ParametersWindow : Window
    {
        Parameters parameters;
        MainController mainWindow;

        public ParametersWindow(Parameters p, MainController mw)
        {
            InitializeComponent();

            parameters = p;
            mainWindow = mw;

            mainStackPanel.DataContext = parameters;
            passwordTextBox.Password = Common.DecryptPassword(parameters.Password);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            parameters.Password = Common.EncryptPassword(passwordTextBox.Password);
            mainWindow.UpdateParameters();
            using (FileStream fs = new FileStream(Properties.Resources.ParamatersFileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                try
                {
                    bf.Serialize(fs, parameters);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Owner, ex.Message, "Ошибка при сериализации параметров");
                }
            }
        }
    }

    [ValueConversion(typeof(byte[]), typeof(string))]
    public class EncryptedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return Common.DecryptPassword((byte[])value);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return Common.EncryptPassword((string)value);
        }
    }

    public class EnumMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();
            return checkValue.Equals(targetValue,
                     StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            bool useValue = (bool)value;
            string targetValue = parameter.ToString();
            if (useValue)
                return Enum.Parse(targetType, targetValue);

            return null;
        }
    }
}
