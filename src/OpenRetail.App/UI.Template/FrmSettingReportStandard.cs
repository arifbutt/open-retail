﻿/**
 * Copyright (C) 2017 Kamarudin (http://coding4ever.net/)
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * The latest version of this file can be found at https://github.com/rudi-krsoftware/open-retail
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using OpenRetail.App.Helper;

namespace OpenRetail.App.UI.Template
{
    public partial class FrmSettingReportStandard : Form
    {
        public FrmSettingReportStandard()
        {
            InitializeComponent();
            ColorManagerHelper.SetTheme(this, this);

            dtpTanggalMulai.Value = DateTime.Today;
            dtpTanggalSelesai.Value = DateTime.Today;
        }

        public FrmSettingReportStandard(string header)
            : this()
        {
            this.Text = header;
            this.lblHeader.Text = header;
        }

        protected void SetCheckBoxTitle(string title)
        {
            this.chkBoxTitle.Text = title;
        }

        /// <summary>
        /// Method override untuk menghandle proses preview
        /// </summary>
        protected virtual void Preview()
        {
        }

        protected virtual void PilihCheckBoxTampilkanNota()
        {
        }

        protected virtual void PilihSemua()
        {
            for (int i = 0; i < chkListBox.Items.Count; i++)
            {
                chkListBox.SetItemChecked(i, chkPilihSemua.Checked);
            }
        }

        protected virtual void PilihCheckBoxTitle()
        {
            chkListBox.Enabled = chkBoxTitle.Checked;
            chkPilihSemua.Enabled = chkBoxTitle.Checked;
        }

        /// <summary>
        /// Method override untuk menghandle proses selesai
        /// </summary>
        protected virtual void Selesai()
        {
            this.Close();
        }                

        private void chkBoxTitle_CheckedChanged(object sender, EventArgs e)
        {
            PilihCheckBoxTitle();
        }

        private void chkTampilkanNota_CheckedChanged(object sender, EventArgs e)
        {
            PilihCheckBoxTampilkanNota();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            Preview();
        }

        private void btnSelesai_Click(object sender, EventArgs e)
        {
            Selesai();
        }

        private void FrmSettingReportStandard_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (KeyPressHelper.IsEsc(e))
                Selesai();
        }
    }
}
