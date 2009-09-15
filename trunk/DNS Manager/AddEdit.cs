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
            this.EditRecord.RunWorkerCompleted += new RunWorkerCompletedEventHandler(EditRecord_RunWorkerCompleted);
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
            if (e.Error == null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                if (e.Error.Message == "CNAME_already_on_record")
                {
                    MessageBox.Show("There can only be one CNAME for this record.", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message == "CNAME_must_be_only_record")
                {
                    MessageBox.Show("A CNAME already exists for this record.", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message.Contains("invalid_record"))
                {
                    MessageBox.Show("This record is invalid:\n" + e.Error.Message.Replace("invalid_record\t", "").CapitaliseFirstLetter(), "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message.Contains("invalid_value"))
                {
                    MessageBox.Show("This value is invalid:\n" + e.Error.Message.Replace("invalid_value\t", "").CapitaliseFirstLetter(), "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message == "no_such_zone")
                {
                    MessageBox.Show("Unable to add record to this zone", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message == "record_already_exists_not_editable")
                {
                    MessageBox.Show("This record already exists and is not editable", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message == "record_already_exists_remove_first")
                {
                    MessageBox.Show("This record already exists.\nPlease try editing it or removing it first.", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (e.Error.Message.Contains("internal_error"))
                {
                    if (MessageBox.Show("An internal error has occurred", "DNS Manager", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        this.AddRecord.RunWorkerAsync(this.BuildRecord());
                        return;
                    }
                }
                else
                {
                    if (MessageBox.Show(e.Error.Message, "DNS Manager", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        this.AddRecord.RunWorkerAsync(this.BuildRecord());
                        return;
                    }
                }

                this.buttonSave.Enabled = true;
                this.buttonCancel.Enabled = true;
                this.textBoxComment.Enabled = true;
                this.textBoxValue.Enabled = true;
                this.textBoxRecord.Enabled = true;
                this.comboBoxType.Enabled = true;
            }
        }

        void EditRecord_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                if (e.Error.Message == "internal_error_could_not_destroy_record"
                    || e.Error.Message == "internal_error_could_not_update_zone")
                {
                    if (MessageBox.Show("An internal error has occurred.", "DNS Manager", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        this.EditRecord.RunWorkerAsync(this.BuildRecord());
                        return;
                    }
                }
                else if (e.Error.Message.Contains("invalid_value"))
                {
                    MessageBox.Show("This value is invalid:\n" + 
                        e.Error.Message.Replace("invalid_value\t", "").CapitaliseFirstLetter()
                        + "\nThe previous record has been deleted.", "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    this.IsEdit = false;
                    this.textBoxRecord.Enabled = true;
                    this.comboBoxType.Enabled = true;
                }

                this.buttonSave.Enabled = true;
                this.buttonCancel.Enabled = true;
                this.textBoxComment.Enabled = true;
                this.textBoxValue.Enabled = true;
            }
        }

        void AddRecord_DoWork(object sender, DoWorkEventArgs e)
        {
            this.API.DNS.AddRecord((DNSRecord)e.Argument);
            System.Threading.Thread.Sleep(3000);
        }

        void EditRecord_DoWork(object sender, DoWorkEventArgs e)
        {
            this.API.DNS.RemoveRecord(this.Record);

            string[] retryErrors = {
                                    "CNAME_must_be_only_record",
                                    "CNAME_already_on_record",
                                    "record_already_exists_remove_first",
                                    "internal_error_updating_zone",
                                    "internal_error_could_not_load_zone",
                                    "internal_error_could_not_add_record"
                                   };

            bool finish = false;

            while (!finish)
            {
                try
                {
                    this.API.DNS.AddRecord((DNSRecord)e.Argument);
                    finish = true;
                }
                catch (Exception x)
                {
                    if (!retryErrors.Contains(x.Message))
                    {
                        throw x;
                    }
                }
            }

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
                    this.EditRecord.RunWorkerAsync(this.BuildRecord());
                }
                else
                {
                    this.AddRecord.RunWorkerAsync(this.BuildRecord());
                }
            }

        }

        private DNSRecord BuildRecord()
        {
            return new DNSRecord
                        {
                            record = this.textBoxRecord.Text,
                            value = this.textBoxValue.Text,
                            type = this.comboBoxType.Text,
                            comment = this.textBoxComment.Text
                        };
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
