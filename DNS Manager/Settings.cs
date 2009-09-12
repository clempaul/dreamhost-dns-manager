using System;
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

        private void buttonSave_Click(object sender, EventArgs e)
        {
            // If key has been entered check it in worker thread, else display error.

            if (this.textBoxKey.Text == string.Empty)
            {
                MessageBox.Show("You must enter an API Key", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                this.buttonSave.Enabled = false;
                this.labelKeyCheck.Text = "Checking key...";
                this.Checker.RunWorkerAsync(this.textBoxKey.Text);
            }
        }

        void Checker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.buttonSave.Enabled = true;
            this.labelKeyCheck.Text = string.Empty;

            // If error occurred

            if (e.Result.GetType() == typeof(bool) && (bool)e.Result == false)
            {
                MessageBox.Show("An unknown error has occurred", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // If missing functions returned

            else if (e.Result.GetType() == typeof(List<string>) && ((List<string>)e.Result).Count > 0)
            {
                string ErrorMessage = "The key does not have access to the following functions:";

                foreach (string m in (List<string>)e.Result)
                {
                    ErrorMessage += "\n - " + m;
                }

                MessageBox.Show(ErrorMessage, "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // If key valid

            else
            {
                Properties.Settings.Default.APIKey = this.textBoxKey.Text;
                Properties.Settings.Default.Save();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void Checker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get API Methods
            string Key = (string)e.Argument;

            DreamhostAPI API = new DreamhostAPI(Key);

            IEnumerable<string> Methods;

            try
            {
                Methods = API.ListAccessibleMethods();
            }
            catch
            {

                e.Result = false;
                return;
            }

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

        private void Settings_Load(object sender, EventArgs e)
        {
            // Enter existing key

            this.textBoxKey.Text = Properties.Settings.Default.APIKey;

            // Set up Background Worker thread

            this.Checker.DoWork += new DoWorkEventHandler(Checker_DoWork);
            this.Checker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Checker_RunWorkerCompleted);
            this.Checker.WorkerSupportsCancellation = true;
        }
    }
}
