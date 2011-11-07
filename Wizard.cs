using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ResConverter
{
    public partial class Wizard : Form
    {
        const string SKIN_FOLDER = "\\skin";
        const string TEMPLATE_FOLDER = "\\project\\template";
        const string PROJECT_FOLDER = "\\project";
        const string NAME_DEFAULT_SKIN = "Ĭ��Ƥ��";
        const string NAME_CUSTOM_RESOLUTION = "(�Զ���)";

        // �ֱ������ö���
        class Resolution
        {
            public int _w;
            public int _h;

            public Resolution(int w, int h) { _w = w; _h = h; }
            public override string ToString()
            {
                const float delta = 0.001f;

                string ratioStr = string.Empty;

                float ratio = (float)_w / _h;
                if (Math.Abs(ratio - 4.0 / 3.0) < delta)
                {
                    ratioStr = "(4:3)";
                }
                else if (Math.Abs(ratio - 16.0 / 10.0) < delta)
                {
                    ratioStr = "(16:10)";
                }
                else if (Math.Abs(ratio - 16.0 / 9.0) < delta)
                {
                    ratioStr = "(16:9)";
                }
                else if (Math.Abs(ratio - 5.0 / 4.0) < delta)
                {
                    ratioStr = "(5:4)";
                }

                return string.Format("{0}x{1} {2}", _w, _h, ratioStr);
            }

            public static Resolution[] List
            {
                get
                {
                    return new Resolution[] {
                    new Resolution(640, 480),
                    new Resolution(800, 600),
                    new Resolution(1024, 768),
                    new Resolution(1152, 864),
                    new Resolution(1280, 720),
                    new Resolution(1280, 800),
                    new Resolution(1280, 960),
                    new Resolution(1280, 1024),
                    new Resolution(1366, 768),
                    new Resolution(1400, 1050),
                    new Resolution(1440, 900),
                    new Resolution(1680, 1050),
                    new Resolution(1920, 1080),
                    };
                }
            }
        }

        class ProjectConfig
        {
            #region ���ݳ�Ա
            public string _baseFolder = string.Empty; // nvlmaker��Ŀ¼
            public string _themeFolder = string.Empty; // Ƥ��Ŀ¼��

            public int _height; // �ֱ���-�߶�
            public int _width;  // �ֱ���-���

            public string _projectName = string.Empty;     // ��Ŀ����
            public string _projectFolder = string.Empty;   // ��ĿĿ¼������ȡ������ΪĿ¼

            // Ŀǰ���žͰ�Ĭ����
            public string _scaler = ResFile.SCALER_DEFAULT; // ���Ų��ԣ�Ŀǰֻ������:(
            public string _quality = ResFile.QUALITY_DEFAULT;   // ����������Ĭ���Ǹ�
            #endregion

            // nvlmaker��Ŀ¼
            public string BaseFolder
            {
                get
                {
                    // �����£���֤��Ϊ��ָ���հ��ִ�
                    _baseFolder = (_baseFolder == null ? string.Empty : _baseFolder.Trim());
                    // �����Ŀ¼����·������������β�� ��\��
                    return Path.GetFullPath(_baseFolder);
                }
            }

            // Ƥ��Ŀ¼
            public string ThemeFolder
            {
                get
                {
                    // �����£���֤��Ϊ��ָ���հ��ִ�
                    _themeFolder = (_themeFolder == null ? string.Empty : _themeFolder.Trim());

                    // 0�����ִ���ʾû��ʹ��Ƥ��
                    if(_themeFolder.Length == 0)
                    {
                        return _themeFolder;
                    }
                    else
                    {
                        // ����Ƥ��Ŀ¼�͸�Ŀ¼
                        return Path.Combine(this.BaseFolder, _themeFolder);
                    }
                }
            }

            public string ThemeConfig
            {
                get
                {
                    return Path.Combine(this.ThemeFolder, "Config.tjs");
                }
            }

            // Ŀ����ĿĿ¼
            public string ProjectFolder
            {
                get
                {
                    // �����£���֤��Ϊ��ָ���հ��ִ�
                    _projectName = (_projectName == null ? string.Empty : _projectName.Trim());

                    // 0�����ִ���ʾû�е���������ĿĿ¼
                    _projectFolder = (_projectFolder == null ? string.Empty : _projectFolder.Trim());

                    if (_projectFolder.Length == 0)
                    {
                        return Path.Combine(this.BaseFolder, "project\\" + _projectName);
                    }
                    else
                    {
                        return Path.Combine(this.BaseFolder, "project\\" + _projectFolder);
                    }
                }
            }

            // �����������Ƿ��Ѿ��걸���ѳ�����Ϣд��output
            public bool IsReady(TextWriter output)
            {
                try
                {
                    string path = this.BaseFolder;
                    if (string.IsNullOrEmpty(_baseFolder) || !Directory.Exists(path))
                    {
                        if (output != null) output.WriteLine("�����Ŀ¼�����ڡ�");
                        return false;
                    }

                    if (_height <= 0 || _width <= 0)
                    {
                        if (output != null) output.WriteLine("������Ч�ķֱ������á�");
                        return false;
                    }

                    path = this.ProjectFolder;
                    if (string.IsNullOrEmpty(_projectName))
                    {
                        if (output != null) output.WriteLine("������Ч����Ŀ���ơ�");
                        return false;
                    }
                    else if (Directory.Exists(path))
                    {
                        if (output != null) output.WriteLine("������Ŀ�ļ����Ѵ��ڣ��������Ŀ������������·����");
                        return false;
                    }

                    path = this.ThemeFolder;
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!Directory.Exists(path))
                        {
                            if (output != null) output.WriteLine("����Ƥ��Ŀ¼�����ڡ�");
                            return false;
                        }

                        path = this.ThemeConfig;
                        if (string.IsNullOrEmpty(path) || !File.Exists(path))
                        {
                            if (output != null) output.WriteLine("���棺Ƥ��ȱ�������ļ�");
                        }
                    }

                    // �������ñ���
                    if(output != null)
                    {
                        output.WriteLine(this.ToString());
                    }
                }
                catch (System.Exception e)
                {
                    if (output != null) output.WriteLine("��Ч����Ŀ���ã�" + e.Message);
                    return false;
                }

                return true;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                if(string.IsNullOrEmpty(this._themeFolder))
                {
                    sb.AppendFormat("Ƥ����{0}", NAME_DEFAULT_SKIN);
                }
                else
                {
                    sb.AppendFormat("Ƥ����{0}", this._themeFolder);
                }
                sb.Append(Environment.NewLine);

                sb.AppendFormat("��Ŀ���ƣ�{0}", this._projectName);sb.Append(Environment.NewLine);
                sb.AppendFormat("��Ŀ�ļ��У�{0}", this.ProjectFolder);sb.Append(Environment.NewLine);
                sb.AppendFormat("�ֱ��ʣ�{0}x{1}", this._width, this._height);sb.Append(Environment.NewLine);
                sb.AppendFormat("===��ϸ��Ϣ===");sb.Append(Environment.NewLine);
                sb.AppendFormat("��Ŀ¼��{0}", this.BaseFolder);sb.Append(Environment.NewLine);
                sb.AppendFormat("���Ų��ԣ�{0}", this._scaler); sb.Append(Environment.NewLine);
                sb.AppendFormat("����������{0}", this._quality); sb.Append(Environment.NewLine);
                return sb.ToString();
            }
        }

        // ���ڲ���������
        ProjectConfig _curConfig = new ProjectConfig();

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
            _curConfig._baseFolder = Directory.GetCurrentDirectory();

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
            // ��Դת��������Ĳ�������
            ResConverter cov = new ResConverter();

            ResConfig config = new ResConfig();
            ResFile f1 = new ResFile();
            f1.path = @"c:\a.png";
            ResFile f2 = new ResFile();
            f2.path = @"c:\b.png";
            config.files.Add(f1);
            config.files.Add(f2);
            config.name = "TestTest";
            config.path = @"c:\test.xml";

            config.Save(config.path);

            ResConfig newConfig = ResConfig.Load(config.path);
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

        }

        void OnStep1()
        {
            //
            int selected = 0;
            lstTemplate.BeginUpdate();
            lstTemplate.Items.Clear();
            lstTemplate.Items.Add(NAME_DEFAULT_SKIN);

            try
            {
                string lastSelect = _curConfig._themeFolder.ToLower();
                string root = _curConfig.BaseFolder;
                string[] skins = Directory.GetDirectories(root + SKIN_FOLDER);
                foreach(string skin in skins)
                {
                    // ֻ��Ŀ¼��
                    lstTemplate.Items.Add(Path.GetFileName(skin));

                    // ƥ���һ��Ŀ¼����ͬ��Ƥ����Ϊѡ������ص�ʱ�򱣳�ѡ����ȷ
                    if (selected == 0 && lastSelect == skin)
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
            // ��¼��һ��ѡ��Ƥ��Ŀ¼
            if(lstTemplate.SelectedIndex > 0)
            {
                string lastSelect = lstTemplate.SelectedItem as string;
                _curConfig._themeFolder = lastSelect.Trim();
            }
            else
            {
                _curConfig._themeFolder = string.Empty;
            }
        }

        void OnStep3()
        {
            // ������һ���Ľ��
            _curConfig._width = (int)numWidth.Value;
            _curConfig._height = (int)numHeight.Value;
        }

        void OnStep4()
        {
            // ������һ���Ľ��
            _curConfig._projectName = txtProjectName.Text;
            if (checkFolder.Checked)
                _curConfig._projectFolder = txtFolderName.Text;

            // ���ݵ�ǰ�������ɱ���
            StringWriter otuput = new StringWriter();
            
            btnOK.Enabled = _curConfig.IsReady(otuput);
            btnOK.BringToFront();
            btnOK.Focus();

            txtReport.Text = otuput.ToString();
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
            if(!checkFolder.Checked)
            {
                txtFolderName.Text = txtProjectName.Text;
            }
        }
    }
}