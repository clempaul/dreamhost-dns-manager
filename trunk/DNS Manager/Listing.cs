using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
            MessageBox.Show("Record Deleted", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.LoadRecords();
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
            string zone = this.toolStripComboBoxDomains.Text ?? string.Empty;

            this.toolStripComboBoxDomains.Items.Clear();

            foreach (DNSRecord d in this.DNSRecords)
            {
                if (!this.toolStripComboBoxDomains.Items.Contains(d.zone))
                {
                    this.toolStripComboBoxDomains.Items.Add(d.zone);
                }
            }

            this.toolStripStatusLabel.Text = string.Empty;

            this.toolStripComboBoxDomains.Text = zone;
            this.SetZone();
        }

        void GetRecords_DoWork(object sender, DoWorkEventArgs e)
        {
            this.DNSRecords = this.API.DNS.ListRecords();

            if (e.Argument != null)
            {
                e.Result = true;
            }
            else
            {
                e.Result = false;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://software.clempaul.me.uk/");
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

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (this.dataGridView.SelectedRows.Count == 1)
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

        private void toolStripButton2_Click(object sender, EventArgs e)
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
            string zoneValue = this.toolStripComboBoxDomains.Text ?? string.Empty;

            this.dataGridView.DataSource = (from d in this.DNSRecords
                                            where d.zone == zoneValue
                                            select d).ToList();
        }
    }
}
