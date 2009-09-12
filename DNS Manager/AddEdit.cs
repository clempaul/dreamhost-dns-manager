using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using clempaul.Dreamhost.ResponseData;
using clempaul;

namespace DNS_Manager
{
    public partial class AddEdit : Form
    {
        private BackgroundWorker AddRecord = new BackgroundWorker();
        private BackgroundWorker EditRecord = new BackgroundWorker();

        private DreamhostAPI API;
        private DNSRecord Record;

        private bool IsEdit = false;

        public AddEdit(DreamhostAPI API, DNSRecord Record)
            : this(API)
        {
            this.IsEdit = true;
            this.Record = Record;

            this.Text = "Edit Record";
            this.textBoxRecord.Text = this.Record.record;
            this.textBoxComment.Text = this.Record.comment;
            this.textBoxValue.Text = this.Record.value;
            this.comboBoxType.Text = this.Record.type;
            this.textBoxRecord.Enabled = false;
            this.comboBoxType.Enabled = false;

            this.EditRecord.DoWork += new DoWorkEventHandler(EditRecord_DoWork);
            this.EditRecord.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AddRecord_RunWorkerCompleted);
        }

        

        public AddEdit(DreamhostAPI API)
        {
            InitializeComponent();

            this.API = API;

            this.AddRecord.DoWork += new DoWorkEventHandler(AddRecord_DoWork);
            this.AddRecord.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AddRecord_RunWorkerCompleted);
        }

        void AddRecord_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        void AddRecord_DoWork(object sender, DoWorkEventArgs e)
        {
            this.API.DNS.AddRecord((DNSRecord)e.Argument);
            System.Threading.Thread.Sleep(3000);
        }

        void EditRecord_DoWork(object sender, DoWorkEventArgs e)
        {
            this.API.DNS.RemoveRecord(this.Record);
            System.Threading.Thread.Sleep(3000);
            this.API.DNS.AddRecord((DNSRecord)e.Argument);
            System.Threading.Thread.Sleep(3000);
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (this.textBoxRecord.Text == string.Empty)
            {
                MessageBox.Show("You must enter a record", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (this.comboBoxType.Text == string.Empty)
            {
                MessageBox.Show("You must select a type", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (this.textBoxValue.Text == string.Empty)
            {
                MessageBox.Show("You must enter a value", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                this.buttonSave.Enabled = false;
                this.buttonCancel.Enabled = false;
                this.textBoxComment.Enabled = false;
                this.textBoxValue.Enabled = false;
                this.textBoxRecord.Enabled = false;
                this.comboBoxType.Enabled = false;

                if (this.IsEdit)
                {
                    this.EditRecord.RunWorkerAsync(
                        new DNSRecord
                        {
                            record = this.textBoxRecord.Text,
                            value = this.textBoxValue.Text,
                            type = this.comboBoxType.Text,
                            comment = this.textBoxComment.Text
                        }
                        );
                }
                else
                {
                    this.AddRecord.RunWorkerAsync(
                        new DNSRecord
                        {
                            record = this.textBoxRecord.Text,
                            value = this.textBoxValue.Text,
                            type = this.comboBoxType.Text,
                            comment = this.textBoxComment.Text
                        }
                        );
                }
            }

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
