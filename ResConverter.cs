using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using System.IO;
using System.Xml.Serialization;

namespace ResConverter
{
    public class ResFile
    {
        // �ļ����Կ�ѡֵ
        public static readonly string SCALER_DEFAULT = "auto";
        public static readonly string QUALITY_DEFAULT = "high";
        public static readonly string QUALITY_NORMAL = "low";
        public static readonly string QUALITY_LOW = "normal";

        // �ļ�·��
        [XmlAttribute]
        public string path = string.Empty;

        // ���Ų���
        [XmlAttribute]
        public string scaler = SCALER_DEFAULT;

        // ��������
        [XmlAttribute]
        public string quality = QUALITY_DEFAULT;
    }

    [XmlRootAttribute("Config", IsNullable = false)]
    public class ResConfig
    {
        // ֻ�Ǹ����ֶ��ѣ��������д
        [XmlAttribute]
        public string name = "Ĭ����Դ�ļ��б�";

        [XmlElement("File")]
        public List<ResFile> files = new List<ResFile>();

        // ��¼��Դ�ļ��ĸ�Ŀ¼��һ�������Դ�ļ����ڵ�Ŀ¼
        [XmlIgnoreAttribute]
        public string path = string.Empty;

        // ���ļ�������ResConfig����
        static public ResConfig Load(string filename)
        {
            try
            {
                using (StreamReader r = new StreamReader(string.Format(filename)))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ResConfig));
                    ResConfig obj = (ResConfig)serializer.Deserialize(r);
                    r.Close();

                    obj.path = filename;
                    return obj;
                }
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        // �ѵ�ǰResConfig���󴢴浽�ļ���
        public bool Save(string filename)
        {
            try
            {
                using (StreamWriter w = new StreamWriter(string.Format(filename)))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ResConfig));
                    serializer.Serialize(w, this);
                    w.Close();
                    return true;
                }
            }
            catch (System.Exception e)
            {
                return false;
            }
        }
    }

    class ResConverter
    {
        enum Quality
        {
            LOW,
            NORMAL,
            HIGH,
        }

        // ��������ת��ָ������Դ
        public void Start(ResConfig config, int destWidth, int destHeight, string destFolder)
        {
            if (destWidth <= 0 || destHeight <= 0) return;

            // ��rootΪ��Ŀ¼��������ͼƬ������
            string baseDir = Path.GetDirectoryName(config.path);
            foreach (ResFile file in config.files)
            {
                string inputFile = Path.GetFullPath(Path.Combine(baseDir, file.path));
                string destFile = Path.GetFullPath(Path.Combine(destFolder, file.path));

                if (destFile == inputFile)
                {
                    // ����ͬ���ļ�����
                    continue;
                }

                Quality q = Quality.HIGH;
                if (file.quality.ToLower() == ResFile.QUALITY_LOW) q = Quality.LOW;
                else if (file.quality.ToLower() == ResFile.QUALITY_NORMAL) q = Quality.NORMAL;

                try
                {
                    Bitmap source = new Bitmap(inputFile);
                    Bitmap dest = Scale(source, destWidth, destHeight, q,
                                        CalcRects(file, destWidth, destHeight));

                    dest.Save(destFile, source.RawFormat);

                    // ת�����

                }
                catch (System.Exception e)
                {
                    // ת�����ִ���
                }
            }
        }

        // ���ݲ��Լ�������ӳ��
        Dictionary<Rectangle, Rectangle> CalcRects(ResFile file, int destWidth, int destHeight)
        {
            // ���ݲ��Լ�������ӳ��
            return null;
        }

        // ���ݸ���������ӳ���������������ԴͼƬ���ŵ�Ŀ���С
        Bitmap Scale(Image source, int destWidth, int destHeight, Quality q,
                                Dictionary<Rectangle, Rectangle> rects)
        {
            if (destWidth <= 0 || destHeight <= 0) return null;

            Bitmap dest = new Bitmap(destWidth, destHeight);
            using (Graphics g = Graphics.FromImage(dest))
            {
                // ���������������������㷨
                switch (q)
                {
                    case Quality.LOW:
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.Default;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                        break;
                    case Quality.NORMAL:
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        break;
                    case Quality.HIGH:
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        break;
                }

                if (rects == null || rects.Count == 0)
                {
                    // ֱ�����ŵ�ָ���Ĵ�С
                    g.DrawImage(source, 0, 0, destWidth, destHeight);
                }
                else
                {
                    // ���չ滮����������
                    foreach (KeyValuePair<Rectangle, Rectangle> kp in rects)
                    {
                        if (kp.Value.Left >= destWidth || kp.Value.Top >= destHeight)
                        {
                            // ������Ч����
                            continue;
                        }

                        g.DrawImage(source, kp.Value, kp.Key, GraphicsUnit.Pixel);
                    }
                }
            }

            return dest;
        }
    }
}
