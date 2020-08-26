using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XInputChecker.ViewModels;

namespace XInputChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ControllerDisplayViewModel controlDisplayVm;
        private System.Timers.Timer refreshTimer;
        private bool timerActive;
        private Mutex refreshMutex = new Mutex();

        public MainWindow()
        {
            InitializeComponent();

            //this.Effect = null;

            refreshTimer = new System.Timers.Timer();
            refreshTimer.AutoReset = true;
            refreshTimer.Interval = 4;
            refreshTimer.Enabled = true;
            refreshTimer.Elapsed += RefreshStateDisplay;
            timerActive = true;

            controlDisplayVm = new ControllerDisplayViewModel();

            controllerSlotCombo.DataContext = controlDisplayVm;
            controllerPropertiesControl.DataContext = controlDisplayVm.StateWrapper;
            controlStatusLb.DataContext = controlDisplayVm;
            rumbleTestPanel.DataContext = controlDisplayVm;

            refreshTimer.Start();
            controlDisplayVm.SlotIndexChanged += ControlDisplayVm_SlotIndexChanged;
        }

        private void ControlDisplayVm_SlotIndexChanged(object sender, EventArgs e)
        {
            //refreshMutex.WaitOne();

            refreshTimer.Stop();
            controllerPropertiesControl.DataContext = null;
            Task.Run(() =>
            {
                controlDisplayVm.UpdateActiveState();
            }).Wait();

            controllerPropertiesControl.DataContext = controlDisplayVm.StateWrapper;
            if (timerActive)
            {
                refreshTimer.Start();
            }

            //refreshMutex.ReleaseMutex();
        }

        private void RefreshStateDisplay(object sender, ElapsedEventArgs e)
        {
            refreshTimer.Stop();

            //refreshMutex.WaitOne();

            controlDisplayVm.UpdateActiveState();

            Dispatcher.BeginInvoke((Action)(() =>
            {
                controllerPropertiesControl.DataContext = null;
                controllerPropertiesControl.DataContext = controlDisplayVm.StateWrapper;
            }));

            //refreshMutex.ReleaseMutex();

            if (timerActive)
            {
                refreshTimer.Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //refreshMutex.WaitOne();
            //refreshLocker.EnterWriteLock();

            timerActive = false;
            refreshTimer.Stop();

            //refreshMutex.ReleaseMutex();
            //refreshLocker.ExitWriteLock();
        }
    }
}
