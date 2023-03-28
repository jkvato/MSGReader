//
// TextContainer.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2013-2023 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System.Text;

namespace MsgReader.Rtf;

/// <summary>
///     Rtf plain text container
/// </summary>
internal class TextContainer
{
    #region Fields
    private readonly ByteBuffer _byteBuffer = new();
    private readonly StringBuilder _stringBuilder = new();
    private readonly Document _domDocument;
    #endregion

    #region Properties
    /// <summary>
    ///     text value
    /// </summary>
    public string Text
    {
        get
        {
            CheckBuffer();
            return _stringBuilder.ToString();
        }
    }
    #endregion

    #region Constructor
    /// <summary>
    ///     Initialize instance
    /// </summary>
    /// <param name="document">owner document</param>
    public TextContainer(Document document)
    {
        _domDocument = document;
    }
    #endregion

    #region Append
    /// <summary>
    ///     Append text content
    /// </summary>
    /// <param name="text"></param>
    public void Append(string text)
    {
        if (string.IsNullOrEmpty(text) == false)
        {
            CheckBuffer();
            _stringBuilder.Append(text);
        }
    }
    #endregion

    #region Accept
    /// <summary>
    ///     Accept rtf token
    /// </summary>
    /// <param name="token">RTF token</param>
    /// <param name="reader"></param>
    public void Accept(Token token, Reader reader)
    {
        if (token == null) return;

        if (token.Type == RtfTokenType.Text)
        {
            if (reader != null)
                if (token.Key[0] == '?')
                    if (reader.LastToken != null)
                        if (reader.LastToken.Type == RtfTokenType.Keyword
                            && reader.LastToken.Key == "u"
                            && reader.LastToken.HasParam)
                        {
                            if (token.Key.Length > 0)
                                CheckBuffer();
                            return;
                        }

            CheckBuffer();
            _stringBuilder.Append(token.Key);
            return;
        }

        if (token.Type == RtfTokenType.Control && token.Key == "'" && token.HasParam)
        {
            if (reader.CurrentLayerInfo.CheckUcValueCount())
                _byteBuffer.Add((byte)token.Param);
            return;
        }

        if (token.Key == Consts.U && token.HasParam)
        {
            // Unicode char
            CheckBuffer();
            _stringBuilder.Append((char)token.Param);
            reader.CurrentLayerInfo.UcValueCount = reader.CurrentLayerInfo.UcValue;
            return;
        }

        if (token.Key == Consts.Tab)
        {
            CheckBuffer();
            _stringBuilder.Append("\t");
            return;
        }

        if (token.Key == Consts.Emdash)
        {
            CheckBuffer();
            _stringBuilder.Append('-');
            return;
        }

        if (token.Key == "")
        {
            CheckBuffer();
            _stringBuilder.Append('-');
        }
    }
    #endregion

    #region CheckBuffer
    /// <summary>
    ///     Check if buffer still contains any text
    /// </summary>
    private void CheckBuffer()
    {
        if (_byteBuffer.Count > 0)
        {
            var text = _byteBuffer.GetString(_domDocument.RuntimeEncoding);
            _stringBuilder.Append(text);
            _byteBuffer.Clear();
        }
    }
    #endregion
}