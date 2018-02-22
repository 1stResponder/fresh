// ������������������������
// <copyright file="TextSample.cs" company="EDXLSharp">
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// ������������������������

using System;

namespace Fresh.API.Areas.HelpPage
{
  /// <summary>
  /// This represents a preformatted text sample on the help page. There's a display template named TextSample associated with this class.
  /// </summary>
  public class TextSample
  {
    public TextSample(string text)
    {
      if (text == null)
      {
        throw new ArgumentNullException("text");
      }

      this.Text = text;
    }

    public string Text { get; private set; }

    public override bool Equals(object obj)
    {
      TextSample other = obj as TextSample;
      return other != null && this.Text == other.Text;
    }

    public override int GetHashCode()
    {
      return this.Text.GetHashCode();
    }

    public override string ToString()
    {
      return this.Text;
    }
  }
}