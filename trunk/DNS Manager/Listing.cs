using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using clempaul;
using clempaul.Dreamhost.ResponseData;

namespace DNS_Manager
{
    public partial class Listing : Form
    {
        public Listing()
        {
            InitializeComponent();
        }

        BackgroundWorker GetRecords = new BackgroundWorker();
        BackgroundWorker DeleteRecord = new BackgroundWorker();

        DreamhostAPI API;
        IEnumerable<DNSRecord> DNSRecords;

        private void Listing_Load(object sender, EventArgs e)
        {
            // Check API Key

            if (Properties.Settings.Default.APIKey == string.Empty &&
                (new Settings().ShowDialog() == DialogResult.Cancel))
            {
                Application.Exit();
            }

            this.GetRecords.DoWork += new DoWorkEventHandler(GetRecords_DoWork);
            this.GetRecords.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GetRecords_RunWorkerCompleted);

            this.DeleteRecord.DoWork += new DoWorkEventHandler(DeleteRecord_DoWork);
            this.DeleteRecord.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DeleteRecord_RunWorkerCompleted);

            this.API = new DreamhostAPI(Properties.Settings.Default.APIKey);

            this.LoadRecords();
        }

        private void LoadRecords()
        {
            if (!this.GetRecords.IsBusy)
            {
                this.toolStripStatusLabel.Text = "Loading records...";
                this.GetRecords.RunWorkerAsync();
            }
        }

        void DeleteRecord_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                MessageBox.Show("Record Deleted!", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.LoadRecords();
            }
            else
            {
                if (e.Error.Message.Contains("no_such"))
                {
                    MessageBox.Show("Record not found.\nTry refreshing the record list to make sure it's not been deleted already.", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message.Contains("internal_error"))
                {
                    if (MessageBox.Show("An internal error has occurred", "DNS Manager", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        this.DeleteRecord.RunWorkerAsync((DNSRecord)this.dataGridView.SelectedRows[0].DataBoundItem);
                        return;
                    }
                }
                else
                {
                    if (MessageBox.Show(e.Error.Message, "DNS Manager", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        this.DeleteRecord.RunWorkerAsync((DNSRecord)this.dataGridView.SelectedRows[0].DataBoundItem);
                        return;
                    }
                }
            }
        }

        void DeleteRecord_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument.GetType() != typeof(DNSRecord))
            {
                throw new Exception("Invalid Argument");
            }

            this.API.DNS.RemoveRecord((DNSRecord)e.Argument);
            System.Threading.Thread.Sleep(3000);
        }

        void GetRecords_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                string zone = this.toolStripComboBoxZones.Text ?? string.Empty;

                this.toolStripComboBoxZones.Items.Clear();

                foreach (DNSRecord d in this.DNSRecords)
                {
                    if (!this.toolStripComboBoxZones.Items.Contains(d.zone))
                    {
                        this.toolStripComboBoxZones.Items.Add(d.zone);
                    }
                }

                this.toolStripStatusLabel.Text = string.Empty;

                this.toolStripComboBoxZones.Text = zone;
                this.SetZone();
            }
            else
            {
                this.toolStripStatusLabel.Text = "Unable to load records... Please try again. (" + e.Error.Message + ")";
            }
        }

        void GetRecords_DoWork(object sender, DoWorkEventArgs e)
        {
            this.DNSRecords = this.API.DNS.ListRecords();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://software.clempaul.me.uk/");
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message, "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            new Settings().ShowDialog();
        }

        private void toolStripButtonReload_Click(object sender, EventArgs e)
        {
            this.LoadRecords();
        }

        private void toolStripButtonSelectDomain_Click(object sender, EventArgs e)
        {
            this.SetZone();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (this.dataGridView.SelectedRows.Count == 1 && !this.DeleteRecord.IsBusy)
            {
                DNSRecord record = (DNSRecord)this.dataGridView.SelectedRows[0].DataBoundItem;

                if (!record.editable)
                {
                    MessageBox.Show("This record is not editable", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (MessageBox.Show("Are you sure you want to delete the record for " + record.record + "?", "DNS Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.toolStripStatusLabel.Text = "Deleting record...";
                    this.DeleteRecord.RunWorkerAsync(record);
                }
            }
        }

        private void toolStripButtonAdd_Click(object sender, EventArgs e)
        {
            if (new AddEdit(this.API).ShowDialog() == DialogResult.OK)
            {
                this.LoadRecords();
            }
        }

        private void toolStripButtonEdit_Click(object sender, EventArgs e)
        {
            if (this.dataGridView.SelectedRows.Count == 1)
            {
                DNSRecord record = (DNSRecord)this.dataGridView.SelectedRows[0].DataBoundItem;

                if (!record.editable)
                {
                    MessageBox.Show("This record is not editable", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    if (new AddEdit(this.API, record).ShowDialog() == DialogResult.OK)
                    {
                        this.LoadRecords();
                    }
                }
            }
        }

        private void SetZone()
        {
            string zoneValue = this.toolStripComboBoxZones.Text ?? string.Empty;

            this.dataGridView.DataSource = (from d in this.DNSRecords
                                            where d.zone == zoneValue
                                            select d).ToList();
        }
    }
}
