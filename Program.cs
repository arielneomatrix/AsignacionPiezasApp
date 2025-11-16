using System;
using System.Windows.Forms;
using AsignacionPiezasApp.Services;

namespace AsignacionPiezasApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Database.Initialize();
            ApplicationConfiguration.Initialize();
            Application.Run(new Forms.MainForm());
        }
    }
}