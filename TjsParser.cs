using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ResConverter
{
    class TjsParser
    {
        #region Tjs��������
        public enum TjsType
        {
            Number,
            String,
            Array,
            Dictionary,
        }

        public class TjsValue
        {
            public TjsType t;
        }

        public class TjsString : TjsValue
        {
            public string val;

            public TjsString(string val)
            {
                this.val = val;
                this.t = TjsType.String;
            }
        }

        public class TjsNumber : TjsValue
        {
            public double val;

            public TjsNumber(double val)
            {
                this.val = val;
                this.t = TjsType.Number;
            }
        }

        public class TjsArray : TjsValue
        {
            public List<TjsValue> val;

            public TjsArray(List<TjsValue> val)
            {
                this.val = val;
                this.t = TjsType.Array;
            }
        }

        public class TjsDict : TjsValue
        {
            public Dictionary<string, TjsValue> val;

            public TjsDict(Dictionary<string, TjsValue> val)
            {
                this.val = val;
                this.t = TjsType.Dictionary;
            }
        }
        #endregion

        #region �����������
        public enum TokenType
        {
            Unknow,
            String,
            Number,
            Symbol,
        }

        public class Token
        {
            public string val = string.Empty;
            public TokenType t = TokenType.Unknow;
        }
        #endregion

        #region ���������н���Tjs����
        Regex _regNumber = new Regex(@"[0-9\.]");
        Regex _regNonChar = new Regex(@"\s");

        const int BUFFER_SIZE = 8192;
        // �����ȡ��������
        char[] _buffer = new char[BUFFER_SIZE];
        // ָ��buffer�н�Ҫ��ȡ���ַ�����ʼ״̬�趨Buffer����
        int _pos = BUFFER_SIZE;
        // buffer�е�ʵ����Ч����
        int _len = 0;

        // ��ȡ�����������bufferδ���Ĳ���
        void UpdateBuffer(TextReader r)
        {
            for (int i = _pos; i < _len; i++)
            {
                _buffer[i - _pos] = _buffer[i];
            }

            // �����µ���ʼ��
            int start = _len > _pos ? _len - _pos : 0;

            _pos = 0;
            _len = start;
            if (r.Peek() >= 0)
            {
                _len += r.ReadBlock(_buffer, start, _buffer.Length - start);
            }
        }

        public Token GetNext(TextReader r)
        {
            // ���buffer��������Ҫ����
            if (_pos >= _buffer.Length)
            {
                UpdateBuffer(r);
            }

            TokenType t = TokenType.Unknow;
            int head = _pos; // ָ���һ����Ч�ַ�
            int tail = -1;  // ָ�����һ����Ч�ַ�

            StringBuilder stored = new StringBuilder();

            while (_pos < _len)
            {
                char cur = _buffer[_pos++];

                //
                // ʹ��if-else����ΪҪ��break���˳�whileѭ��
                //
                if(t == TokenType.Unknow)
                {
                    // ���������Ч�ַ�
                    if (_regNonChar.IsMatch(cur.ToString())) { head++; }
                    // �����ַ���
                    else if (cur == '"') { t = TokenType.String; }
                    // ��������
                    else if (_regNumber.IsMatch(cur.ToString())) { t = TokenType.Number; }
                    // �������Ϊ����
                    else { t = TokenType.Symbol; }
                }
                else if(t == TokenType.String)
                {
                    // �Դ���Ϊ��β
                    if (cur == '"') { tail = _pos - 1; break; }
                }
                else if (t == TokenType.Number)
                {
                    // ���������Ч�ַ�
                    if (_regNonChar.IsMatch(cur.ToString())) { tail = _pos - 2; break; }
                    // ��������ַ�
                    else if (!_regNumber.IsMatch(cur.ToString())) { _pos--; tail = _pos - 1; break; }
                }
                else if(t == TokenType.Symbol)
                {
                    // ���������Ч�ַ�
                    if (_regNonChar.IsMatch(cur.ToString())) { tail = _pos - 2; break; }
                }

                // ����Ƿ�buffer����
                if (_pos >= _buffer.Length)
                {
                    if (_pos > head)
                    {
                        // ��buffer��δ���token���д���
                        stored.Append(_buffer, head, _pos - head);
                    }

                    UpdateBuffer(r);
                    head = _pos;
                }
            }

            // ׷�ӵ�ǰ���
            if (tail >= head)
            {
                stored.Append(_buffer, head, tail - head + 1);
            }

            // ��������ֵ
            Token token = new Token();
            token.t = t;
            if (stored.Length > 0)
            {
                token.val = stored.ToString();
            }
            return token;
        }
        #endregion

        public TjsValue Parse(TextReader r)
        {
            return null;
        }
    }
}
