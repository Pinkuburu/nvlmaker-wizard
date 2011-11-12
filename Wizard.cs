using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Threading;
using Tjs;
using System.Diagnostics;
using System.Text.RegularExpressions;

//
// app.ico == Any closet is a walk-in closet if you try hard enough..ico
// Based on icons by Paul Davey aka Mattahan. All rights reserved.
// 

namespace Wizard
{
    public partial class Wizard : Form
    {
        const string NAME_CUSTOM_RESOLUTION = "(�Զ���)";

        // ���ڲ���������
        WizardConfig _curConfig = new WizardConfig();

        // ��¼Ŀǰ�Ĳ���
        int _curStep = -1;

        // �����������
        GroupBox[] _stepGroups = null;

        // ���账����ָ��
        delegate void StepHandler();
        StepHandler[] _stepHandlers;

        // ��ȡ/���õ�ǰ����
        int Step
        {
            get { return _curStep; }
            set
            {
                // ����δ����
                if(_curStep == value)
                {
                    return;
                }

                // ���²���
                _curStep = value;
                if (_curStep < 0) 
                {
                    _curStep = 0; 
                }
                else if (_curStep >= _stepGroups.Length) 
                {
                    _curStep = _stepGroups.Length - 1; 
                }

                // ���յ�ǰ������ʽ���ض�Ӧ���
                _stepGroups[_curStep].BringToFront();

                // ���ư�ť��ʾ
                btnNext.Enabled = _curStep < _stepGroups.Length - 1;
                btnPrev.Enabled = _curStep > 0;
                if (!btnPrev.Enabled) btnNext.Focus();
                if (btnNext.Enabled) btnNext.BringToFront();

                // ���յ�ǰ������ö�Ӧ�Ĵ�����
                if (_curStep < _stepHandlers.Length)
                {
                    StepHandler handler = _stepHandlers[_curStep];
                    handler();
                }
            }
        }

        public Wizard()
        {
            InitializeComponent();

            this.SuspendLayout();

            // �趨����ʱ�Ĺ���·��Ϊ�����Ŀ¼
            _curConfig.BaseFolder = Directory.GetCurrentDirectory();

            // ��ʼ���ֱ�������
            cbResolution.Items.Clear();
            cbResolution.Items.Add(NAME_CUSTOM_RESOLUTION);
            foreach(Resolution res in Resolution.List)
            {
                cbResolution.Items.Add(res);
            }
            cbResolution.SelectedIndex = cbResolution.Items.Count - 1;

            // ��ʼ���򵼸���������λ�ã����浽�������Ա�����
            _stepGroups = new GroupBox[] { gbStep1, gbStep2, gbStep3, gbStep4 };
            for (int i = 1; i < _stepGroups.Length; i++)
            {
                // �Ѱ���λ�ö�ͬ������һ����λ��
                _stepGroups[i].Location = _stepGroups[0].Location;
            }

            // �󶨵�ǰ����
            _stepHandlers = new StepHandler[] { 
                new StepHandler(this.OnStep1),
                new StepHandler(this.OnStep2), 
                new StepHandler(this.OnStep3),
                new StepHandler(this.OnStep4),
            };

            this.Step = 0;

            this.ResumeLayout();
        }

        private void test()
        {
            return;

            string strTitle = ";System.title =\"ģ�幤��\";";
            string strW = ";scWidth =1024;";
            string strH = ";scHeight =768;";

            Regex regTitle = new Regex(@"\s*;\s*System.title\s*=");
            Regex regW = new Regex(@"\s*;\s*scWidth\s*=");
            Regex regH = new Regex(@"\s*;\s*scHeight\s*=");

            bool ret = false;
            ret = regTitle.IsMatch(strTitle);
            ret = regW.IsMatch(strW);
            ret = regH.IsMatch(strH);

            string[] layouts = Directory.GetFiles(_curConfig.ThemeDataFolder, WizardConfig.UI_LAYOUT);

            // ����tjsֵ��ȡ
            foreach (string layout in layouts)
            {
                using (StreamReader r = new StreamReader(layout))
                {
                    TjsParser parser = new TjsParser();
                    TjsValue val = null;
                    do 
                    {
                        val = parser.Parse(r);
                    } while (val != null);
                }
            }

            // ����tjs���Ŷ�ȡ
            using (StreamReader r = new StreamReader(layouts[0]))
            {
                TjsParser parser = new TjsParser();
                TjsParser.Token token = null;
                do
                {
                    token = parser.GetNext(r);
                } while (token != null && token.t != TjsParser.TokenType.Unknow);
            }

            // ��Դת��������Ĳ�������
            ResConfig config = new ResConfig();
            config.files.Add(new ResFile(@"a.png"));
            config.files.Add(new ResFile(@"b.png"));
            config.name = "TestTest";
            config.path = @"c:\";

            config.Save(@"c:\test.xml");
            ResConfig newConfig = ResConfig.Load(@"c:\test.xml");

            ResConverter cov = new ResConverter();
            cov.Start(config, @"d:\", 1024, 768, 1920, 1080);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            Step = Step + 1;
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            Step = Step - 1;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // ��ʼ������Ŀ
            if(MessageBox.Show("��ʼ������Ŀ��", this.Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                OnBuild();
            }
        }
        
        void OnStep1()
        {
            // ˢ������Ŀ¼�б�
            int selected = 0;
            lstTemplate.BeginUpdate();
            lstTemplate.Items.Clear();
            lstTemplate.Items.Add(WizardConfig.NAME_DEFAULT_THEME);

            try
            {
                string lastSelect = _curConfig.ThemeName.ToLower();
                string root = _curConfig.BaseFolder;
                string[] themes = Directory.GetDirectories(root + WizardConfig.THEME_FOLDER);
                foreach (string theme in themes)
                {
                    // ֻ��Ŀ¼��
                    string name = Path.GetFileName(theme);
                    lstTemplate.Items.Add(name);

                    // ƥ���һ��Ŀ¼����ͬ��������Ϊѡ������ص�ʱ�򱣳�ѡ����ȷ
                    if (selected == 0 && lastSelect == name)
                    {
                        selected = lstTemplate.Items.Count - 1;
                    }
                }
            }
            catch (System.Exception e)
            {
            	// �����˾Ͳ�����
            }

            lstTemplate.SelectedIndex = selected;
            lstTemplate.EndUpdate();
        }

        void OnStep2()
        {
            ProjectProperty info = _curConfig.ReadThemeInfo();

            txtResolution.Text = "ͼƬԭʼ�ֱ��ʣ�";

            // �ڶ�����˵�����ڣ�ĿǰҲֻ����ôһ�����Կ�����ʾ
            string name = _curConfig.IsDefaultTheme ? WizardConfig.NAME_DEFAULT_THEME:_curConfig.ThemeName;
            txtResolution.Text += string.Format("{0}{0}��{3}��: {1}x{2}",
                                               Environment.NewLine, info.width, info.height, name);

            // �Ƿ�ѡ����Ĭ�����⣬ûѡ�򸽼�Ĭ����������
            if (!_curConfig.IsDefaultTheme)
            {
                ProjectProperty baseInfo = _curConfig.ReadBaseTemplateInfo();
                txtResolution.Text += string.Format("{0}{0}��{3}��: {1}x{2}",
                    Environment.NewLine, baseInfo.width, baseInfo.height, WizardConfig.NAME_DEFAULT_THEME);
            }

            // ѡ���ֱ���
            int w = info.width, h = info.height;
            for(int i=0;i<cbResolution.Items.Count;i++)
            {
                Resolution r = cbResolution.Items[i] as Resolution;
                if (r != null && r._w == w && r._h == h )
                {
                    cbResolution.SelectedIndex = i;
                    break;
                }
            }            

            // ���ﱾ��Ӧ�ø������Ų�����������ʾÿ���ļ��������
            // �ȼ���һ���ļ���Ŀ¼�ɡ���
            LoadThemeFiles();

            // �����²����õĺ���
            test();
        }

        void OnStep3()
        {
            // ������һ���Ľ��
            _curConfig._width = (int)numWidth.Value;
            _curConfig._height = (int)numHeight.Value;
            
            txtProjectName.SelectAll();
            txtProjectName.Focus();
        }

        void OnStep4()
        {
            // ������һ���Ľ��
            _curConfig.ProjectName = txtProjectName.Text;
            if (checkFolder.Checked)
                _curConfig.ProjectFolder = txtFolderName.Text;

            // ���ݵ�ǰ�������ɱ���
            StringWriter otuput = new StringWriter();
            
            btnOK.Enabled = _curConfig.IsReady(otuput);
            ReportRefresh(otuput.ToString());

            btnOK.BringToFront();
            btnOK.Show();
            btnOK.Focus();
            btnExit.Hide();
        }

        void OnBuild()
        {
            // ����Logging
            LoggingBegin();

            // ��ʼ������Ŀ
            try
            {
                // ��ֹ��ť
                btnPrev.Enabled = false;
                btnCancel.Enabled = false;
                btnOK.Enabled = false;
                btnExit.Enabled = false;

                // ����һ���߳��������ļ�����ֹUI����
                Thread t = new Thread(new ThreadStart(BuildProject));
                t.Start();
                while(!t.Join(100))
                {
                    Application.DoEvents();
                }
                
                // ������ɣ���ʾ�˳���ť
                btnOK.Hide();
                btnExit.BringToFront();
                btnExit.Show();
                btnExit.Enabled = true;

                ReportAppend("��Ŀ������ϣ�");
            }
            catch (System.Exception e)
            {
                // ��ʾ����ԭ��
                ReportAppend(e.Message);

                // �ָ���ť
                btnCancel.Enabled = true;
                btnPrev.Enabled = true;
            }

            // ����Logging
            LoggingEnd();
        }

        // �������ô���Ŀ����Ŀ
        private void BuildProject()
        {
            // �������ж�ȡ��Ҫ��Դ��С��Ŀ���С
            int dw = _curConfig._width, dh = _curConfig._height;

            // �ȴӻ���ģ��Ŀ¼�����ļ�����ĿĿ¼
            string template = _curConfig.BaseTemplateFolder;
            string project = _curConfig.ProjectFolder;

            // ��ȡ����ģ�������
            ProjectProperty baseInfo = _curConfig.ReadBaseTemplateInfo();

            int sw = baseInfo.width;
            if (sw <= 0) sw = WizardConfig.DEFAULT_WIDTH;
            int sh = baseInfo.height;
            if (sh <= 0) sh = WizardConfig.DEFAULT_HEIGHT;

            ConvertFiles(template, sw, sh, project, dw, dh);

            // �����������꣬д����Ŀ����
            AdjustSettings(sw, sh);

            // ���ѡ���˷�Ĭ�����⣬�ٴ�����Ŀ¼�����ļ�����Ŀ�����ļ���
            if (_curConfig.ThemeFolder != template)
            {
                // ��ȡ��ѡ��������
                ProjectProperty themeInfo = _curConfig.ReadThemeInfo();

                sw = themeInfo.width;
                if (sw <= 0) sw = WizardConfig.DEFAULT_WIDTH;
                sh = themeInfo.height;
                if (sh <= 0) sh = WizardConfig.DEFAULT_HEIGHT;

                // ������ļ�ֱ�ӿ�������Ŀ¼
                ConvertFiles(_curConfig.ThemeFolder, sw, sh, _curConfig.ProjectDataFolder, dw, dh);

                // �����������꣬д����Ŀ����
                AdjustSettings(sw, sh);
            }
        }

        // ���ߺ����������ļ��У�����¼���е��ļ�
        void CreateDir(string source, string dest, List<string> files)
        {
            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            if (files != null)
            {
                string[] curFiles = Directory.GetFiles(source);
                files.AddRange(curFiles);
            }

            string[] subDirs = Directory.GetDirectories(source);
            if (subDirs.Length == 0)
            {
                // ľ���ҵ��κ���Ŀ¼
                return;
            }

            foreach (string dir in subDirs)
            {
                string name = Path.GetFileName(dir);
                CreateDir(dir, Path.Combine(dest, name), files);
            }
        }

        // ���ߺ����������������ļ�
        void ConvertFiles(string srcPath, int sw, int sh, string destPath, int dw, int dh)
        {
            // Դ�ļ��б�
            List<string> srcFiles = new List<string>();
            try
            {
                // ����Ŀ¼����ȡ�ļ��б�
                CreateDir(srcPath, destPath, srcFiles);

                // ����ͼƬת�����ã����ڼ�¼��Ҫת����ͼƬ�ļ��������ļ���ֱ�ӿ���
                ResConfig resource = new ResConfig();
                resource.path = srcPath;
                resource.name = WizardConfig.NAME_DEFAULT_THEME;

                // ���������ļ�
                int cutLen = srcPath.Length;
                foreach (string srcfile in srcFiles)
                {
                    // �ص�ģ��Ŀ¼�Ծ���ȡ���·��
                    string relFile = srcfile.Substring(cutLen + 1);

                    // ȡ����չ��
                    string ext = Path.GetExtension(relFile).ToLower();

                    if ( // ��������Դ�ļ���ͬ�ǾͲ���ת����
                         (sw != dw || sh != dh) &&
                         // ����ĳЩͼƬ
                         !WizardConfig.IgnorePicture(relFile) &&
                         // ֻת����Щ��չ����Ӧ���ļ�
                         (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp") )
                    {
                        // ��ͼƬ����ӵ�ת������
                        resource.files.Add(new ResFile(relFile));
                    }
                    else
                    {
                        // ֱ�ӿ���
                        Logging(string.Format("����{0}", relFile));
                        File.Copy(srcfile, Path.Combine(destPath, relFile), true);
                    }
                }

                Logging("ͼƬת���С���");

                if (resource.files.Count > 0)
                {
                    // ����һ��ͼƬת��������ʼת��
                    ResConverter conv = new ResConverter();
                    conv.NotifyProcessEvent += new ResConverter.NotifyProcessHandler(conv_NotifyProcessEvent);
                    conv.Start(resource, destPath, sw, sh, dw, dh);
                }

            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // ���ߺ���������Ŀ����Ŀ�ļ����е�����
        void AdjustSettings(int sw, int sh)
        {
            string dataPath = _curConfig.ProjectDataFolder;
            int dh = _curConfig._height;
            int dw = _curConfig._width;
            string title = _curConfig.ProjectName;

            try
            {
                WizardConfig.ModifySetting(dataPath, title, dh, dw);
            }
            catch (System.Exception e)
            {
                ReportAppend("�޸�setting.tjsʧ��:" + e.Message);
            }

            try
            {
                WizardConfig.ModifyConfig(dataPath, title, dh, dw);
            }
            catch (System.Exception e)
            {
                ReportAppend("�޸�Config.tjsʧ��:" + e.Message);
            }
            
            // ����Ƿ���Ҫת��
            if (sw != dw || sh != dh)
            {
                try
                {
                    WizardConfig.ModifyLayout(dataPath, sw, sh, dh, dw);
                }
                catch (System.Exception e)
                {
                    ReportAppend("�޸Ľ��沼���ļ�ʧ��:" + e.Message);
                }
            }
        }

        // ��������Ŀ¼�����е�Ŀ¼�͸�Ŀ¼�µ��ʾ�
        private void LoadThemeFiles()
        {
            // �����DataĿ¼
            string theme = _curConfig.ThemeFolder;

            try
            {
                lstScale.BeginUpdate();
                lstScale.Items.Clear();

                // ��ȡ����Ŀ¼�µ��ļ��б�
                string[] subDirs = Directory.GetDirectories(theme);
                foreach (string dir in subDirs)
                {
                    lstScale.Items.Add(string.Format("<dir> {0}", Path.GetFileName(dir)));
                }
                string[] files = Directory.GetFiles(theme);
                foreach (string file in files)
                {
                    lstScale.Items.Add(Path.GetFileName(file));
                }
                lstScale.EndUpdate();
            }
            catch (System.Exception e) { }
        }

        #region Logging����
        string _titleSaved = null;
        void LoggingBegin()
        {
            // ���洰�ڱ���
            if (_titleSaved == null) { _titleSaved = this.Text; }
        }
        void LoggingEnd()
        {
            // �ָ����ڱ���
            this.Invoke(new ThreadStart(delegate()
            {
                if (_titleSaved != null) { this.Text = _titleSaved; _titleSaved = null; }
            }));
        }
        void Logging(string msg)
        {
            if (_titleSaved == null)
            {
                Debug.Assert(false, "call LoggingBegin() first");
                return;
            }

            this.BeginInvoke(new ThreadStart(delegate()
            {
                this.Text = string.Format("{0}: {1}", _titleSaved, msg);
            }));
        }
        #endregion

        void ReportRefresh(string report)
        {
            this.Invoke(new ThreadStart(delegate()
            {
                txtReport.Text = report;
            }));
        }

        void ReportAppend(string report)
        {
            this.BeginInvoke(new ThreadStart(delegate()
            {
                txtReport.Text += report + Environment.NewLine;
            }));
        }

        // ����Ƿ��ڲ��������б���ֹ������ѡ��ؼ��໥����
        bool _isSelectingRes = false;
        private void cbResolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            Resolution res = cbResolution.SelectedItem as Resolution;
            if(res != null)
            {
                _isSelectingRes = true;
                numWidth.Value = res._w;
                numHeight.Value = res._h;
                _isSelectingRes = false;
            }
        }

        private void numResolution_ValueChanged(object sender, EventArgs e)
        {
            if(!_isSelectingRes && cbResolution.Items.Count > 0)
            {
                cbResolution.SelectedIndex = 0;
            }
        }

        private void checkFolder_CheckedChanged(object sender, EventArgs e)
        {
            txtFolderName.ReadOnly = !checkFolder.Checked;
            if(!checkFolder.Checked)
            {
                txtFolderName.Text = txtProjectName.Text;
            }
        }

        private void txtProjectName_TextChanged(object sender, EventArgs e)
        {
            if (!checkFolder.Checked)
            {
                txtFolderName.Text = txtProjectName.Text;
            }
        }

        private void lstTemplate_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ��¼ѡȡ������Ŀ¼
            if (lstTemplate.SelectedIndex > 0)
            {
                string lastSelect = lstTemplate.SelectedItem as string;
                _curConfig.ThemeName = lastSelect.Trim();
            }
            else
            {
                _curConfig.ThemeName = string.Empty;
            }

            ProjectProperty info = _curConfig.ReadThemeInfo();
            txtTemplate.Text = info.readme;
            txtProjectName.Text = info.title;
        }

        private void Wizard_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(!btnExit.Enabled)
            {
                MessageBox.Show("���ڴ�����Ŀ�����Ժ򡭡�");
                e.Cancel = true;
            }
        }

        void conv_NotifyProcessEvent(ResConverter sender, ResConverter.NotifyProcessEventArgs e)
        {
            Logging(string.Format("({0}/{1}){2} ת���С���", e.index, e.count, e.file));
        }
    }
}