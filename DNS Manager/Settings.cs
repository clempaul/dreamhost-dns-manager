﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using clempaul;

namespace DNS_Manager
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        // BackgroundWorker for verifying key functionality.

        private BackgroundWorker Checker = new BackgroundWorker();

        private void Settings_Load(object sender, EventArgs e)
        {
            // Enter existing key

            this.textBoxKey.Text = Properties.Settings.Default.APIKey;

            // Set up Background Worker thread

            this.Checker.DoWork += new DoWorkEventHandler(Checker_DoWork);
            this.Checker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Checker_RunWorkerCompleted);
            this.Checker.WorkerSupportsCancellation = true;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            // Cancel background worker thread

            if (this.Checker.IsBusy)
            {
                this.Checker.CancelAsync();
            }

            // Close dialog

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }


        private void buttonSave_Click(object sender, EventArgs e)
        {
            // If key has been entered check it in worker thread, else display error.

            if (this.textBoxKey.Text == string.Empty)
            {
                MessageBox.Show("You must enter an API Key", "Dreamhost DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                this.buttonSave.Enabled = false;
                this.labelKeyCheck.Text = "Checking key...";
                this.Checker.RunWorkerAsync(this.textBoxKey.Text);
            }
        }

        private void Checker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get API Methods
            string Key = (string)e.Argument;

            DreamhostAPI API = new DreamhostAPI(Key);

            IEnumerable<string> Methods = API.ListAccessibleMethods();

            // Add unavailable methods to list

            string[] RequiredMethods = {
                                        "dns-list_records",
                                        "dns-add_record",
                                        "dns-remove_record"
                                       };

            List<string> MissingMethods = new List<string>();

            foreach (string m in RequiredMethods)
            {
                if (!Methods.Contains(m))
                {
                    MissingMethods.Add(m);
                }
            }

            e.Result = MissingMethods;
        }

        void Checker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            

            // If error occurred

            if (e.Error != null)
            {
                // Ask whether to retry.

                if (MessageBox.Show(e.Error.Message, "Dreamhost DNS Manager", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                {
                    this.Checker.RunWorkerAsync(this.textBoxKey.Text);
                    return;
                } 
            }

            // If missing functions returned

            else if (e.Result.GetType() == typeof(List<string>) && ((List<string>)e.Result).Count > 0)
            {
                string ErrorMessage = "The key does not have access to the following functions:";

                foreach (string m in (List<string>)e.Result)
                {
                    ErrorMessage += "\n - " + m;
                }

                MessageBox.Show(ErrorMessage, "Dreamhost DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // If key valid

            else
            {
                // Save key

                Properties.Settings.Default.APIKey = this.textBoxKey.Text;
                Properties.Settings.Default.Save();

                // Set dialog result and return.

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            
            this.buttonSave.Enabled = true;
            this.labelKeyCheck.Text = string.Empty;
        }




    }
}
