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
using System.Text.RegularExpressions;

namespace ResConverter
{
    public partial class Wizard : Form
    {
        const string THEME_FOLDER = "\\skin";
        const string TEMPLATE_FOLDER = "\\project\\template";
        const string DATA_FOLDER = "\\data";
        const string PROJECT_FOLDER = "\\project";

        const string UI_LAYOUT = "macro\\ui*.tjs";
        const string UI_SETTING = "macro\\setting.tjs";
        const string UI_CONFIG = "Config.tjs";

        const string NAME_DEFAULT_THEME = "Ĭ������";
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

        // ģ��Ļ�������
        class ProjectProperty
        {
            public string readme = string.Empty;

            public string title
            {
                get
                {
                    // ��ȡ����
                    string ret = null;
                    if (_setting != null)
                    {
                        ret = _setting.GetString("title");
                    }
                    return ret == null ? string.Empty : ret;
                }
            }
            
            public int width
            {
                get
                {
                    // ��ȡԤ����
                    double ret = double.NaN;
                    if (_setting != null)
                    {
                        ret = _setting.GetNumber("width");
                    }
                    return double.IsNaN(ret) ? 0 : (int)ret;
                }
            }

            public int height
            {
                get
                {
                    // ��ȡԤ��߶�
                    double ret = double.NaN;
                    if (_setting != null)
                    {
                        ret = _setting.GetNumber("height");
                    }
                    return double.IsNaN(ret) ? 0 : (int)ret;
                }
            }
            
            TjsDict _setting = null;

            public void LoadSetting(string file)
            {
                _setting = null;

                if (File.Exists(file))
                {
                    using (StreamReader r = new StreamReader(file))
                    {
                        TjsParser parser = new TjsParser();
                        TjsDict setting = parser.Parse(r) as TjsDict;
                        _setting = setting;
                    }
                }
            }
        }

        // ��Ŀ�����ö���
        class WizardConfig
        {
            #region ���ݳ�Ա
            private string _baseFolder = string.Empty; // nvlmaker��Ŀ¼
            private string _themeName = string.Empty; // ����Ŀ¼��

            public int _height; // �ֱ���-�߶�
            public int _width;  // �ֱ���-���

            private string _projectName = string.Empty;     // ��Ŀ����
            private string _projectFolder = string.Empty;   // ��ĿĿ¼������ȡ������ΪĿ¼

            // Ŀǰ���žͰ�Ĭ����
            private string _scaler = ResFile.SCALER_DEFAULT; // ���Ų��ԣ�Ŀǰֻ������:(
            private string _quality = ResFile.QUALITY_DEFAULT;   // ����������Ĭ���Ǹ�

            // �����ϴζ�ȡ���������ԣ������ζ�ȡ
            private ProjectProperty _themeInfo = null;
            #endregion

            // nvlmaker��·��
            public string BaseFolder
            {
                get
                {
                    // �����Ŀ¼����·������������β�� ��\��
                    return _baseFolder;
                }
                set
                {
                    // �����£���֤��Ϊ��ָ���հ��ִ�
                    _baseFolder = (value == null ? string.Empty : value.Trim());
                }
            }

            // ����ģ��·��
            public string BaseTemplateFolder
            {
                get
                {
                    return this.BaseFolder + TEMPLATE_FOLDER;
                }
            }

            // ��������
            public string ThemeName
            {
                get
                {
                    return _themeName;
                }
                set
                {
                    // �����£���֤��Ϊ��ָ���հ��ִ�
                    string themeName = (value == null ? string.Empty : value.Trim());

                    // ���������������Ԥ��������
                    if(themeName != _themeName)
                    {
                        this._themeName = themeName;
                        this._themeInfo = null;
                    }
                }
            }

            // ����·��
            public string ThemeFolder
            {
                get
                {
                    // 0�����ִ���ʾû��ʹ������
                    if(_themeName.Length == 0)
                    {
                        return this.BaseTemplateFolder;
                    }
                    else
                    {
                        // ��������Ŀ¼�͸�Ŀ¼
                        return this.BaseFolder + THEME_FOLDER + "\\" + _themeName;
                    }
                }
            }

            // ���������ļ�
            public string ThemeSetting
            {
                get
                {
                    return Path.Combine(this.ThemeDataFolder, UI_SETTING);
                }
            }

            // ���������Ŀ¼
            public string ThemeDataFolder
            {
                get
                {
                    // 0�����ִ���ʾû��ʹ������
                    if (_themeName.Length == 0)
                    {
                        return this.ThemeFolder + DATA_FOLDER; 
                    }
                    else
                    {
                        return this.ThemeFolder;
                    }
                }
            }

            // Ŀ����Ŀ·��
            public string ProjectFolder
            {
                get
                {
                    if (_projectFolder.Length == 0)
                    {
                        return this.BaseFolder + PROJECT_FOLDER + "\\" + _projectName;
                    }
                    else
                    {
                        return this.BaseFolder + PROJECT_FOLDER + "\\" + _projectFolder;
                    }
                }
                set
                {
                    // 0�����ִ���ʾû�е���������ĿĿ¼
                    _projectFolder = (value == null ? string.Empty : value.Trim());
                }
            }

            // Ŀ����Ŀ����·��
            public string ProjectDataFolder
            {
                get
                {
                    return this.ProjectFolder + DATA_FOLDER;
                }
            }

            // Ŀ����Ŀ����
            public string ProjectName
            {
                get
                {
                    return _projectName;
                }
                set
                {
                    // �����£���֤��Ϊ��ָ���հ��ִ�
                    _projectName = (value == null ? string.Empty : value.Trim());
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
                            if (output != null) output.WriteLine("��������Ŀ¼�����ڡ�");
                            return false;
                        }

                        path = this.ThemeSetting;
                        if (string.IsNullOrEmpty(path) || !File.Exists(path))
                        {
                            if (output != null) output.WriteLine("���棺����ȱ�������ļ�");
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

            // �������õ��������ɱ���
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("== ��Ŀ�����嵥 =="); sb.Append(Environment.NewLine);

                sb.Append(Environment.NewLine);
                string theme = this._themeName;
                if (string.IsNullOrEmpty(theme)) theme = NAME_DEFAULT_THEME;
                sb.AppendFormat("��ѡ���⣺{0}", theme); sb.Append(Environment.NewLine);
                sb.AppendFormat("�ֱ����趨��{0}x{1}", this._width, this._height); sb.Append(Environment.NewLine);
                
                sb.Append(Environment.NewLine);
                sb.AppendFormat("��Ŀ���ƣ�{0}", this._projectName);sb.Append(Environment.NewLine);
                sb.AppendFormat("��Ŀλ�ã�{0}", this.ProjectFolder); sb.Append(Environment.NewLine);
                
                sb.Append(Environment.NewLine);
                sb.AppendFormat("���Ų��ԣ�{0}", this._scaler); sb.Append(Environment.NewLine);
                sb.AppendFormat("����������{0}", this._quality); sb.Append(Environment.NewLine);
                sb.AppendFormat("NVLMakerĿ¼��{0}", this.BaseFolder);sb.Append(Environment.NewLine);
                return sb.ToString();
            }

            // ��ȡ��ѡ���������
            public ProjectProperty ReadThemeInfo()
            {
                // ֱ�ӷ��ض�ȡֵ
                if (this._themeInfo != null)
                {
                    return this._themeInfo;
                }

                ProjectProperty info = new ProjectProperty();
                this._themeInfo = info;

                // ��ȡreadme�ļ���Ϊ��ʾ����
                try
                {
                    string readmefile = Path.Combine(this.ThemeDataFolder, "Readme.txt");
                    if (File.Exists(readmefile))
                    {
                        using (StreamReader r = new StreamReader(readmefile))
                        {
                            info.readme = r.ReadToEnd();
                        }
                    }
                }
                catch (System.Exception e)
                {
                    // ����Ĳ�����
                    this._themeInfo = null;
                    info.readme = e.Message;
                }

                // ��ȡ�����ļ�
                try
                {
                    info.LoadSetting(this.ThemeSetting);
                }
                catch (System.Exception e)
                {
                    // ����Ĳ�����
                    this._themeInfo = null;
                    info.readme = e.Message;
                }
                
                return info;
            }

            // ��ȡ����ģ�������
            public ProjectProperty ReadBaseTemplateInfo()
            {
                // ���ѡ����Ĭ�ϵ����⣬�򷵻���������
                if(this.BaseTemplateFolder == this.ThemeFolder)
                {
                    return this.ReadThemeInfo();
                }

                // ����Ͳ���readme�ˣ�Ҳ�������棬ÿ�ε��ö����ļ���һ��
                string file = Path.Combine(this.BaseTemplateFolder + DATA_FOLDER, UI_SETTING);
                ProjectProperty info = new ProjectProperty();
                try
                {
                    info.LoadSetting(file);
                }
                catch (System.Exception e)
                {
                    info.readme = e.Message;
                }
                return info;
            }

            #region ���ߺ���
            public static void ModifyDict(TjsDict dict, int sw, int sh, int dw, int dh)
            {

            }

            public static void ModifyLayout(string dataPath, int sw, int sh, int dh, int dw)
            {
                // ����layout
                string[] layouts = Directory.GetFiles(dataPath, UI_LAYOUT);
                foreach (string layout in layouts)
                {
                    TjsParser parser = new TjsParser();
                    TjsDict setting = null;
                    using (StreamReader r = new StreamReader(layout))
                    {
                        setting = parser.Parse(r) as TjsDict;
                    }

                    if (setting != null)
                    {
                        ModifyDict(setting, sw, sh, dw, dh);
                    }

                    using (StreamWriter w = new StreamWriter(layout, false, Encoding.Unicode))
                    {
                        w.Write(setting.ToString());
                    }
                }
            }

            public static void ModifyConfig(string dataPath, string title, int dh, int dw)
            {
                // ����config
                string configFile = Path.Combine(dataPath, UI_CONFIG);
                if (File.Exists(configFile))
                {
                    Regex regTitle = new Regex(@"\s*;\s*System.title\s*=");
                    Regex regW = new Regex(@"\s*;\s*scWidth\s*=");
                    Regex regH = new Regex(@"\s*;\s*scHeight\s*=");

                    StringBuilder buf = new StringBuilder();
                    using (StreamReader r = new StreamReader(configFile))
                    {
                        while (!r.EndOfStream)
                        {
                            string line = r.ReadLine();
                            if (regTitle.IsMatch(line))
                            {
                                buf.AppendLine(string.Format(";System.title = \"{0}\";", title));
                            }
                            else if (regW.IsMatch(line))
                            {
                                buf.AppendLine(string.Format(";scWidth = {0};", dw));
                            }
                            else if (regH.IsMatch(line))
                            {
                                buf.AppendLine(string.Format(";scHeight = {0};", dh));
                            }
                            else
                            {
                                buf.AppendLine(line);
                            }
                        }
                    }

                    using (StreamWriter w = new StreamWriter(configFile, false, Encoding.Unicode))
                    {
                        w.Write(buf.ToString());
                    }
                }
            }

            public static void ModifySetting(string dataPath, string title, int dh, int dw)
            {
                // ����setting
                string settingFile = Path.Combine(dataPath, UI_SETTING);
                if (File.Exists(settingFile))
                {
                    TjsParser parser = new TjsParser();

                    TjsDict setting = null;
                    using (StreamReader r = new StreamReader(settingFile))
                    {
                        setting = parser.Parse(r) as TjsDict;
                    }

                    if (setting != null)
                    {
                        setting.SetString("title", title);
                        setting.SetNumber("width", dw);
                        setting.SetNumber("height", dh);
                        using (StreamWriter w = new StreamWriter(settingFile, false, Encoding.Unicode))
                        {
                            w.Write(setting.ToString());
                        }
                    }
                }
            }
            #endregion
        }

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

            string[] layouts = Directory.GetFiles(_curConfig.ThemeDataFolder, UI_LAYOUT);

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
            if(MessageBox.Show("��ʼ������Ŀ��", "ȷ��", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
            lstTemplate.Items.Add(NAME_DEFAULT_THEME);

            try
            {
                string lastSelect = _curConfig.ThemeName.ToLower();
                string root = _curConfig.BaseFolder;
                string[] themes = Directory.GetDirectories(root + THEME_FOLDER);
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

            txtResolution.Text = "ͼƬԭʼ�ֱ������£�";

            // �ڶ�����˵�����ڣ�ĿǰҲֻ����ôһ�����Կ�����ʾ
            txtResolution.Text += string.Format("{0}{0}=== ��ѡ���� ==={0}�ֱ���: {1}x{2}",
                                               Environment.NewLine, info.width, info.height);

            ProjectProperty baseInfo = _curConfig.ReadBaseTemplateInfo();
            if(baseInfo != info)
            {
                txtResolution.Text += string.Format("{0}{0}=== Ĭ������ ==={0}�ֱ���: {1}x{2}",
                                                    Environment.NewLine, baseInfo.width, baseInfo.height);
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
            txtReport.Text = otuput.ToString();

            btnOK.BringToFront();
            btnOK.Show();
            btnOK.Focus();
            btnExit.Hide();
        }

        void OnBuild()
        {
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

                txtReport.Text += "��Ŀ������ϣ�";
            }
            catch (System.Exception e)
            {
                // ��ʾ����ԭ��
                txtReport.Text += e.Message;

                // �ָ���ť
                btnCancel.Enabled = true;
                btnPrev.Enabled = true;
            }
        }

        #region ���̴�������
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
            int sw = baseInfo.width, sh = baseInfo.height;
            ConvertFiles(template, sw, sh, project, dw, dh);

            // �����������꣬д����Ŀ����
            AdjustSettings(sw, sh);

            // ���ѡ���˷�Ĭ�����⣬�ٴ�����Ŀ¼�����ļ�����Ŀ�����ļ���
            if (_curConfig.ThemeFolder != template)
            {
                // ��ȡ��ѡ��������
                ProjectProperty themeInfo = _curConfig.ReadThemeInfo();
                sw = themeInfo.width; sh = themeInfo.height;

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
            // ���洰�ڱ���
            string title = this.Text;

            // Դ�ļ��б�
            List<string> srcFiles = new List<string>();
            try
            {
                // ����Ŀ¼����ȡ�ļ��б�
                CreateDir(srcPath, destPath, srcFiles);

                // ת��ͼƬ�ļ��������ļ�ֱ�ӿ���
                ResConfig resource = new ResConfig();
                resource.path = srcPath;
                resource.name = NAME_DEFAULT_THEME;

                int cutLen = srcPath.Length;
                foreach (string srcfile in srcFiles)
                {
                    // �ص�ģ��Ŀ¼�Ծ���ȡ���·��
                    string relFile = srcfile.Substring(cutLen + 1);

                    // ȡ����չ��
                    string ext = Path.GetExtension(relFile).ToLower();

                    if ( (sw != dw || sh != dh) && // ��������Դ�ļ���ͬ�ǾͲ���ת����
                         (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp") )
                    {
                        // ��ͼƬ����ӵ�ת������
                        resource.files.Add(new ResFile(relFile));
                    }
                    else
                    {
                        // ֱ�ӿ���
                        this.BeginInvoke(new ThreadStart(delegate()
                        {
                            this.Text = string.Format("{0}: ����{1}", title, relFile);
                        }));

                        File.Copy(srcfile, Path.Combine(destPath, relFile), true);
                    }
                }

                this.BeginInvoke(new ThreadStart(delegate()
                {
                    this.Text = string.Format("{0}: ͼƬת���С���", title);
                }));

                if (resource.files.Count > 0)
                {
                    ResConverter conv = new ResConverter();
                    conv.Start(resource, destPath, sw, sh, dw, dh);
                }

            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // �ָ����ڱ���
            this.Invoke(new ThreadStart(delegate()
            {
                this.Text = title;
            }));
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
                this.BeginInvoke(new ThreadStart(delegate()
                {
                    this.txtReport.Text += "�޸�setting.tjsʧ�ܣ�" + Environment.NewLine;
                }));
            }

            try
            {
                WizardConfig.ModifyConfig(dataPath, title, dh, dw);
            }
            catch (System.Exception e)
            {
                this.BeginInvoke(new ThreadStart(delegate()
                {
                    this.txtReport.Text += "�޸�Config.tjsʧ�ܣ�" + Environment.NewLine;
                }));
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
                    this.BeginInvoke(new ThreadStart(delegate()
                    {
                        this.txtReport.Text += "�޸Ľ��沼���ļ�ʧ�ܣ�" + Environment.NewLine;
                    }));
                }
            }
        }
        #endregion

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
            catch (System.Exception) { }
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
    }
}