using System;
using System.Collections.Generic;
using System.Text;
using Tjs;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ResConverter
{
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
        // һЩ����
        public const string THEME_FOLDER = "\\skin";
        public const string TEMPLATE_FOLDER = "\\project\\template";
        public const string DATA_FOLDER = "\\data";
        public const string PROJECT_FOLDER = "\\project";

        public const string UI_LAYOUT = "macro\\ui*.tjs";
        public const string UI_SETTING = "macro\\setting.tjs";
        public const string UI_CONFIG = "Config.tjs";

        public const int DEFAULT_WIDTH = 1024;
        public const int DEFAULT_HEIGHT = 768;

        public const string NAME_DEFAULT_THEME = "Ĭ������";

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

        public bool IsDefaultTheme
        {
            get
            {
                return string.IsNullOrEmpty(this.ThemeName);
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
                if (this.IsDefaultTheme)
                {
                    return this.BaseTemplateFolder;
                }
                else
                {
                    // ��������Ŀ¼�͸�Ŀ¼
                    return this.BaseFolder + THEME_FOLDER + "\\" + this.ThemeName;
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
                if (this.IsDefaultTheme)
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

                ProjectProperty info = ReadThemeInfo();
                if(info == null || info.height <= 0 || info.width <= 0)
                {
                    if (output != null) output.WriteLine("���棺����ֱ��ʴ���");
                }

                ProjectProperty baseInfo = ReadBaseTemplateInfo();
                if (baseInfo != info && (baseInfo == null || baseInfo.height <= 0 || baseInfo.width <= 0))
                {
                    if (output != null) output.WriteLine("���棺Ĭ������ֱ��ʴ���");
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
            string theme = (this.IsDefaultTheme) ? this.ThemeName : NAME_DEFAULT_THEME;
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
            double num = dict.GetNumber("left");
            if(!double.IsNaN(num))
            {
                num = num * dw / sw;
                dict.SetNumber("left", Math.Floor(num));
            }

            num = dict.GetNumber("top");
            if (!double.IsNaN(num))
            {
                num = num * dh / sh;
                dict.SetNumber("top", Math.Floor(num));
            }

            num = dict.GetNumber("x");
            if (!double.IsNaN(num))
            {
                num = num * dw / sw;
                dict.SetNumber("x", Math.Floor(num));
            }

            num = dict.GetNumber("y");
            if (!double.IsNaN(num))
            {
                num = num * dh / sh;
                dict.SetNumber("y", Math.Floor(num));
            }

            // �޸�locate����
            TjsValue v = null;
            if (dict.val.TryGetValue("locate", out v))
            {
                TjsArray locate = v as TjsArray;
                if(locate != null)
                {
                    List<TjsValue> locatenew = new List<TjsValue>();
                    foreach (TjsValue pos in locate.val)
                    {
                        TjsArray xy = pos as TjsArray;
                        if(xy != null && xy.val.Count == 2)
                        {
                            TjsNumber x = xy.val[0] as TjsNumber;
                            TjsNumber y = xy.val[1] as TjsNumber;
                            if(x != null && y != null)
                            {
                                List<TjsValue> posnew = new List<TjsValue>();
                                posnew.Add(new TjsNumber(Math.Floor(x.val * dw / sw)));
                                posnew.Add(new TjsNumber(Math.Floor(y.val * dh / sh)));
                                locatenew.Add(new TjsArray(posnew));
                            }
                            else
                            {
                                Debug.Assert(false, "invalid pos element");
                            }
                        }
                        else
                        {
                            Debug.Assert(false, "invalid pos array");
                        }
                    }

                    dict.val["locate"] = new TjsArray(locatenew);
                }
            }

            foreach (KeyValuePair<string, TjsValue> kv in dict.val)
            {
                TjsDict inner = kv.Value as TjsDict;
                if(inner != null)
                {
                    ModifyDict(inner, sw, sh, dw, dh);
                }
            }
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
}
