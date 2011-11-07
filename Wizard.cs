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
        class ProjectConfig
        {
            #region ���ݳ�Ա
            public string _baseFolder = string.Empty; // nvlmaker��Ŀ¼
            public string _themeFolder = string.Empty; // ����Ŀ¼��

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
                    // �����£���֤��Ϊ��
                    _baseFolder = (_baseFolder == null ? string.Empty : _baseFolder.Trim());
                    // �����Ŀ¼����·������������β�� ��\��
                    return Path.GetFullPath(_baseFolder);
                }
            }

            // ����Ŀ¼
            public string ThemeFolder
            {
                get
                {
                    // �����£���֤��Ϊ��
                    _themeFolder = (_themeFolder == null ? string.Empty : _themeFolder.Trim());
                    // ��������Ŀ¼�͸�Ŀ¼
                    return Path.Combine(this.BaseFolder, _themeFolder);
                }
            }

            public string ThemeConfig
            {
                get
                {
                    return Path.Combine(this.ThemeFolder, "Config.tjs");
                }
            }

            // Ŀ�깤��Ŀ¼
            public string ProjectFolder
            {
                get
                {
                    // �����£���֤��Ϊ��
                    _projectName = (_projectName == null ? string.Empty : _projectName.Trim());
                    _projectFolder = (_projectFolder == null ? string.Empty : _projectFolder.Trim());

                    if(!string.IsNullOrEmpty(_projectFolder))
                    {
                        return Path.Combine(this.BaseFolder, "project\\" + _projectFolder);
                    }
                    else
                    {
                        return Path.Combine(this.BaseFolder, "project\\" + _projectName);
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

                    path = this.ThemeFolder;
                    if (string.IsNullOrEmpty(_themeFolder) || !Directory.Exists(path))
                    {
                        if (output != null) output.WriteLine("��������Ŀ¼�����ڡ�");
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
                        if (output != null) output.WriteLine("������Ч�Ĺ������ơ�");
                        return false;
                    }
                    else if (Directory.Exists(path))
                    {
                        if (output != null) output.WriteLine("���󣺹���Ŀ¼�Ѵ��ڣ����������������������·����");
                        return false;
                    }

                    path = this.ThemeConfig;
                    if(string.IsNullOrEmpty(path) || !File.Exists(path))
                    {
                        if (output != null) output.WriteLine("���棺����ȱ�������ļ�");
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
                sb.AppendFormat("���⣺{0}\n", this._themeFolder);
                sb.AppendFormat("��Ŀ���ƣ�{0}\n", this._projectName);
                sb.AppendFormat("�ֱ��ʣ�{0}x{1}\n", this._width, this._height);
                sb.AppendFormat("===��ϸ����===\n");
                sb.AppendFormat("�����Ŀ¼��{0}\n", this.BaseFolder);
                sb.AppendFormat("���Ų��ԣ�{0}\n", this._scaler);
                sb.AppendFormat("����������{0}\n", this._quality);
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

            // �趨����ʱ�Ĺ���·��Ϊ�����Ŀ¼
            _curConfig._baseFolder = Directory.GetCurrentDirectory();
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
            
        }

        void OnStep2()
        {
            //
        }

        void OnStep3()
        {
            //
        }

        void OnStep4()
        {
            // ���ݵ�ǰ�������ɱ���
            StringWriter otuput = new StringWriter();
            
            btnOK.Enabled = _curConfig.IsReady(otuput);
            btnOK.BringToFront();
            btnOK.Focus();

            txtReport.Text = otuput.ToString();
        }
    }
}