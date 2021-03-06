﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace PluginInstallerUI
{
    public enum Operation { Install, Uninstall }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public BootstrapperApplication Bootstrapper { get; set; }
        private volatile bool running = false;
        private Operation operation = Operation.Install;

        public MainWindow(BootstrapperApplication bootstrapper)
        {
            InitializeComponent();
            MouseDown += Window_MouseDown;
            this.Bootstrapper = bootstrapper;
            this.Bootstrapper.ApplyComplete += this.OnApplyComplete;
            this.Bootstrapper.DetectPackageComplete += this.OnDetectPackageComplete;
            this.Bootstrapper.PlanComplete += this.OnPlanComplete;
            this.Bootstrapper.Error += Bootstrapper_Error;
            this.Bootstrapper.ExecuteMsiMessage += Bootstrapper_ExecuteMsiMessage;
            this.Bootstrapper.ExecuteFilesInUse += Bootstrapper_ExecuteFilesInUse;
            this.installBtn.IsEnabled = false;

            this.insLbl.Text = checkCandidates();
            this.insLbl.TextChanged += InsLbl_TextChanged;

            CenterWindowOnScreen();
            Bootstrapper.Engine.StringVariables["BVAR_IDA_HOME_PATH"] = this.insLbl.Text.Trim();
            Bootstrapper.Engine.Detect();
            

        }

        private void InsLbl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!running)
            {
                Bootstrapper.Engine.StringVariables["BVAR_IDA_HOME_PATH"] = this.insLbl.Text.Trim();
                Bootstrapper.Engine.Detect();
            }
        }

        private void Bootstrapper_ExecuteFilesInUse(object sender, ExecuteFilesInUseEventArgs e)
        {
            foreach (string f in e.Files)
            {
                updateMsg(f);
            }

        }

        private void Bootstrapper_ExecuteMsiMessage(object sender, ExecuteMsiMessageEventArgs e)
        {
            updateMsg(e.Message);
        }

        private void Bootstrapper_Error(object sender, ErrorEventArgs e)
        {
            updateMsg("ERROR: [" + e.ErrorCode + "] " + e.ErrorMessage);
        }

        private void updateMsg(string msg)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                this.logger.AppendText(Environment.NewLine);
                this.logger.AppendText("> " + msg);
                this.logger.ScrollToEnd();
            }));
        }

        private void updateBtn(string msg)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.installBtn.Content = msg;
            }));
        }

        private void OnDetectPackageComplete(object sender, DetectPackageCompleteEventArgs e)
        {
            updateMsg(e.PackageId + " is " + e.State.ToString().ToLower() + " in your system.");

            if (e.PackageId.Contains("Kam1n0_Plugin_IDA_Pro"))
            {
                if (e.State == PackageState.Absent)
                {
                    operation = Operation.Install;
                    updateBtn("Agree and Install All");
                }
                else if (e.State == PackageState.Present)
                {
                    operation = Operation.Uninstall;
                    updateBtn("Remove All");
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        this.insLbl.IsEnabled = false;
                        this.insLbl.Visibility = Visibility.Hidden;
                        this.insLblTlt.Content = "We found existing Kam1n0 packages in your computer.";
                        this.insLblBtn.Visibility = Visibility.Hidden;
                    }));

                }
            }

            this.Dispatcher.BeginInvoke(new Action(() => { this.installBtn.IsEnabled = true; }));
        }

        private void OnApplyComplete(object sender, ApplyCompleteEventArgs e)
        {
            if (operation.Equals(Operation.Install))
            {
                updateBtn("Installation Completed.");
                updateMsg("Installation Completed.");
            }
            else
            {
                updateBtn("Application Uninstalled.");
                updateMsg("Application Uninstalled.");
            }
            this.running = false;
        }


        private void OnPlanComplete(object sender, PlanCompleteEventArgs e)
        {
            if (operation.Equals(Operation.Install))
            {
                updateMsg("Installing...");
            }
            else
            {
                updateMsg("Removing...");
            }

            Bootstrapper.Engine.Apply(System.IntPtr.Zero);


        }



        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                MessageBoxResult ans = MessageBox.Show("The Kam1n0 IDA Plugin is being installed. Are you sure to terminate intallation and reverse changes?", "Terminating the Installation Procedure.", MessageBoxButton.YesNo);
                if (ans.Equals(MessageBoxResult.No))
                    return;
            }
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.installBtn.IsEnabled = false;
            if (operation == Operation.Install)
            {
                this.installBtn.Content = "Installing...";
                if (!CheckIfIsIdaPluginsFolder(this.insLbl.Text)) {
                    MessageBox.Show("The selected directory does not contains a IDA distribution. Please choose another directory.", "Warning", MessageBoxButton.OK);
                    this.Dispatcher.BeginInvoke(new Action(() => { this.installBtn.IsEnabled = true; }));
                    updateBtn("Agree and Install All");
                    return;
                }

                Bootstrapper.Engine.StringVariables["BVAR_IDA_HOME_PATH"] = this.insLbl.Text.Trim();
                Bootstrapper.Engine.Plan(LaunchAction.Install);
            }
            else
            {
                this.installBtn.Content = "Uninstalling...";
                Bootstrapper.Engine.Plan(LaunchAction.Uninstall);
            }
            this.running = true;

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    this.insLbl.Text = dialog.SelectedPath;
                }
            }
        }


        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }


        public static bool CheckIfIsIdaPluginsFolder(String selectedPath)
        {
                if (System.IO.File.Exists(selectedPath + "\\idaq.exe") || System.IO.File.Exists(selectedPath + "\\ida.exe"))
                    return true;
                else
                return false;
        }


        public static string checkCandidates()
        {
            string[] candidates = new string[] {
            @"C:\Program Files (x86)\IDA 6.8",
            @"C:\Program Files (x86)\IDA 6.9",
            @"C:\Program Files\IDA 7.0",
            @"C:\Program Files\IDA 7.1",
            @"C:\Program Files\IDA 7.2",
            @"C:\Program Files\IDA 7.3" };
            foreach (string candidate in candidates) {
                if (CheckIfIsIdaPluginsFolder(candidate))
                    return candidate;
            }
            return candidates[1];
        }

    }
}
