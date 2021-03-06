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
using WeifenLuo.WinFormsUI.Docking;

using OpenRetail.Helper;
using ConceptCave.WaitCursor;
using OpenRetail.App.Cashier.Transaksi;
using OpenRetail.Model;
using OpenRetail.Bll.Api;
using OpenRetail.Bll.Service;
using log4net;
using AutoUpdaterDotNET;
using System.Threading;
using OpenRetail.App.Cashier.Pengaturan;
using OpenRetail.App.Cashier.Laporan;

namespace OpenRetail.App.Cashier.Main
{
    public partial class FrmMain : Form, IListener
    {
        //Disable close button
        private const int CP_DISABLE_CLOSE_BUTTON = 0x200;

        /// <summary>
        /// Variabel lokal untuk menampung menu id. 
        /// Menu id digunakan untuk mengeset hak akses masing-masing form yang diakses
        /// </summary>
        private Dictionary<string, string> _getMenuID;
        private ILog _log;

        private ThreadHelper _lightSleeper = new ThreadHelper();

        public bool IsLogout { get; private set; }

        public FrmMain()
        {
            InitializeComponent();
            mainDock.BackColor = Color.FromArgb(255, 255, 255);

            _log = MainProgram.log;

            AddEventToolbar();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            InitializeStatusBar();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                if (Utils.IsRunningUnderIDE())
                {
                    return base.CreateParams;
                }
                else
                {
                    var cp = base.CreateParams;
                    cp.ClassStyle = cp.ClassStyle | CP_DISABLE_CLOSE_BUTTON;

                    // bug fixed: flicker
                    // http://stackoverflow.com/questions/2612487/how-to-fix-the-flickering-in-user-controls
                    //cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED

                    return cp;
                }
            }
        }        

        private IEnumerable<ToolStripMenuItem> GetItems(ToolStripMenuItem menuItem)
        {
            foreach (var item in menuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem)
                {
                    var dropDownItem = (ToolStripMenuItem)item;

                    if (dropDownItem.HasDropDownItems)
                    {
                        foreach (ToolStripMenuItem subItem in GetItems(dropDownItem))
                            yield return subItem;
                    }

                    yield return (ToolStripMenuItem)item;
                }
            }
        }

        public void InisialisasiData()
        {
            SetMenuId();
            SetDisabledMenuAndToolbar(menuStrip1, toolStrip1);
            
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;            
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            _lightSleeper.Cancel();

            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {                    
                    var msg = "Update terbaru versi {0} sudah tersedia. Saat ini Anda sedang menggunakan Versi {1}\n\nApakah Anda ingin memperbarui aplikasi ini sekarang ?";

                    var installedVersion = string.Format("{0}.{1}.{2}.{3} (v{0}.{1}.{2}{4})", args.InstalledVersion.Major, args.InstalledVersion.Minor, args.InstalledVersion.Build, args.InstalledVersion.Revision, MainProgram.stageOfDevelopment);
                    var currentVersion = string.Format("{0}.{1}.{2}.{3}", args.CurrentVersion.Major, args.CurrentVersion.Minor, args.CurrentVersion.Build, args.CurrentVersion.Revision);

                    var dialogResult = MessageBox.Show(string.Format(msg, currentVersion, installedVersion), "Update Tersedia",
                                                       MessageBoxButtons.YesNo,
                                                       MessageBoxIcon.Information);

                    if (dialogResult.Equals(DialogResult.Yes))
                    {
                        try
                        {
                            AutoUpdater.DownloadUpdate();
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Tidak ada update yang tersedia, silahkan dicoba lagi nanti.", "Update belum tersedia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Gagal melakukan koneksi ke server, silahkan dicoba lagi nanti.", "Cek update terbaru gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetMenuId()
        {
            IMenuBll menuBll = new MenuBll(_log);
            var listOfMenu = menuBll.GetAll().Where(f => f.parent_id != null && f.nama_form.Length > 0)
                                             .ToList();
            _getMenuID = new Dictionary<string, string>();

            foreach (var item in listOfMenu)
            {
                _getMenuID.Add(item.nama_form, item.menu_id);
            }
        }

        /// <summary>
        /// Method untuk menonaktifkan menu dan toolbar yang belum aktif (membaca setting tabel m_menu)
        /// </summary>
        /// <param name="menuStrip"></param>
        /// <param name="toolStrip"></param>
        private void SetDisabledMenuAndToolbar(MenuStrip menuStrip, ToolStrip toolStrip)
        {
            IMenuBll menuBll = new MenuBll(_log);
            var listOfMenu = menuBll.GetAll()
                                    .Where(f => f.parent_id != null && f.nama_form.Length > 0)
                                    .ToList();
            
            // perulangan untuk mengecek menu dan sub menu
            foreach (ToolStripMenuItem parentMenu in menuStrip.Items)
            {
                var listOfChildMenu = GetItems(parentMenu);

                foreach (var childMenu in listOfChildMenu)
                {
                    var menu = listOfMenu.Where(f => f.nama_menu == childMenu.Name)
                                         .SingleOrDefault();
                    if (menu != null)
                    {
                        childMenu.Enabled = menu.is_enabled;
                    }
                }
            }

            // perulangan untuk mengecek item toolbar
            foreach (ToolStripItem item in toolStrip.Items)
            {
                var menu = listOfMenu.Where(f => f.nama_menu.Substring(3) == item.Name.Substring(2))
                                     .SingleOrDefault();
                if (menu != null)
                {
                    item.Enabled = menu.is_enabled;
                }
            }
        }

        public void InitializeStatusBar()
        {
            var dt = DateTime.Now;

            sbJam.Text = string.Format("{0:HH:mm:ss}", dt);
            sbTanggal.Text = string.Format("{0}, {1}", DayMonthHelper.GetHariIndonesia(dt), dt.Day + " " + DayMonthHelper.GetBulanIndonesia(dt.Month) + " " + dt.Year);

            if (MainProgram.pengguna != null)
                sbOperator.Text = string.Format("Operator : {0}", MainProgram.pengguna.nama_pengguna);

            var firstReleaseYear = 2017;
            var currentYear = DateTime.Today.Year;
            var copyright = currentYear > firstReleaseYear ? string.Format("{0} - {1}", firstReleaseYear, currentYear) : firstReleaseYear.ToString();

            var appName = string.Format(MainProgram.appName, MainProgram.currentVersion, MainProgram.stageOfDevelopment, copyright);

            this.Text = appName;
            sbNamaAplikasi.Text = appName.Replace("&", "&&");
        }

        private void AddEventToolbar()
        {
            tbPenjualanProduk.Click += mnuPenjualanProduk_Click;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            sbJam.Text = string.Format("{0:HH:mm:ss}", DateTime.Now);
        }

        private bool IsChildFormExists(Form frm)
        {
            return !(frm == null || frm.IsDisposed);
        }        

        private void CloseAllDocuments()
        {
            foreach (var form in MdiChildren)
            {
                form.Close();
            }                
        }

        private void ShowForm<T>(object sender, ref T form)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            if (!IsChildFormExists((DockContent)(object)form))
                form = (T)Activator.CreateInstance(typeof(T), header, MainProgram.pengguna, menuId);

            ((DockContent)(object)form).Show(this.mainDock);
        }

        private void ShowFormDialog<T>(object sender)
        {
            var header = GetMenuTitle(sender);
            var menuName = GetMenuName(sender);

            if (menuName.Substring(0, 6) == "mnuLap")
            {
                header = string.Format("Laporan {0}", GetMenuTitle(sender));
            }

            if (RolePrivilegeHelper.IsHaveHakAkses(menuName, MainProgram.pengguna, GrantState.SELECT))
            {
                var form = (T)Activator.CreateInstance(typeof(T), header);
                ((Form)(object)form).ShowDialog();
            }
            else
                MsgHelper.MsgWarning("Maaf Anda tidak mempunyai otoritas untuk mengakses menu ini");
        }

        private string GetMenuTitle(object sender)
        {
            var title = string.Empty;

            if (sender is ToolStripMenuItem)
            {
                title = ((ToolStripMenuItem)sender).Text;
            }
            else
            {
                title = ((ToolStripButton)sender).Text;
            }

            return title;
        }

        private string GetMenuName(object sender)
        {
            var menuName = string.Empty;

            if (sender is ToolStripMenuItem)
            {
                menuName = ((ToolStripMenuItem)sender).Name;
            }
            else
            {
                menuName = ((ToolStripButton)sender).Name;
                menuName = string.Format("mnu{0}", menuName.Substring(2));
            }

            return menuName;
        }

        private string GetFormName(object sender)
        {
            var formName = string.Empty;

            if (sender is ToolStripMenuItem)
            {
                formName = ((ToolStripMenuItem)sender).Tag.ToString();
            }
            else
            {
                formName = ((ToolStripButton)sender).Tag.ToString();
            }

            return formName;
        }

        private void mnuPenjualanProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);
            var menuId = _getMenuID[GetFormName(sender)];

            var frmPenjualan = new FrmPenjualan(header, MainProgram.pengguna, menuId);
            frmPenjualan.Show(this.mainDock);
        }

        private void mnuSettingPrinter_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);

            var frmPengaturan = new FrmPengaturanUmum(header, MainProgram.pengaturanUmum);
            frmPengaturan.ShowDialog();
        }

        private void mnuGantiUser_Click(object sender, EventArgs e)
        {            
            if (MsgHelper.MsgKonfirmasi("Apakah proses ingin dilanjutkan ?"))
            {
                using (new StCursor(Cursors.WaitCursor, new TimeSpan(0, 0, 0, 0)))
                {
                    AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
                    CloseAllDocuments();

                    this.IsLogout = true;
                    this.Close();
                }
            }
        }

        private void mnuKeluarDariProgram_Click(object sender, EventArgs e)
        {
            if (MsgHelper.MsgKonfirmasi("Apakah proses ingin dilanjutkan ?"))
            {
                using (new StCursor(Cursors.WaitCursor, new TimeSpan(0, 0, 0, 0)))
                {
                    CloseAllDocuments();
                    this.Close();
                }
            }
        }

        private void OpenUrl(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        private void mnuFanPageOpenRetail_Click(object sender, EventArgs e)
        {
            var url = "https://www.facebook.com/openretail/";
            OpenUrl(url);
        }

        private void mnuGroupOpenRetail_Click(object sender, EventArgs e)
        {
            var url = "https://web.facebook.com/groups/openretail/";
            OpenUrl(url);
        }

        private void mnuPetunjukPenggunaanOpenRetail_Click(object sender, EventArgs e)
        {
            var url = "https://github.com/rudi-krsoftware/open-retail/wiki/";
            OpenUrl(url);
        }

        private void mnuRegistrasi_Click(object sender, EventArgs e)
        {
            var url = "https://openretailblog.wordpress.com/registrasi/";
            OpenUrl(url);
        }        

        private void mnuDukungPengembanganOpenRetail_Click(object sender, EventArgs e)
        {
            var url = "https://github.com/rudi-krsoftware/open-retail/wiki/Cara-Berkontribusi/";
            OpenUrl(url);
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            var frmAbout = new FrmAbout();
            frmAbout.ShowDialog();
        }

        public void Ok(object sender, object data)
        {
            if (data is Profil)
            {
                MainProgram.profil = (Profil)data;
                InitializeStatusBar();
            }
        }

        public void Ok(object sender, bool isNewData, object data)
        {
            throw new NotImplementedException();
        }


        private void mnuLapPenjualanProduk_Click(object sender, EventArgs e)
        {
            var header = GetMenuTitle(sender);

            var frmLaporan = new FrmLapPenjualan(string.Format("Laporan {0}", header), MainProgram.pengguna, MainProgram.pengaturanUmum);
            frmLaporan.ShowDialog();
        }

        private void mnuCekUpdateTerbaru_Click(object sender, EventArgs e)
        {
            if (MainProgram.onlineUpdateUrlInfo.Length > 0)
            {
                using (new StCursor(Cursors.WaitCursor, new TimeSpan(0, 0, 0, 0)))
                {
                    AutoUpdater.Start(MainProgram.onlineUpdateUrlInfo);

                    while (!_lightSleeper.HasBeenCanceled)
                    {
                        _lightSleeper.Sleep(10000);
                    } 
                }
            }
            else
                MsgHelper.MsgWarning("Maaf link/url Online Update belum diset !!!\nProses cek update terbaru batal.");
        }        
    }
}
