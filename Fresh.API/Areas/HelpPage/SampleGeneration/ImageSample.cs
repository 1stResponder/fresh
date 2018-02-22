// ������������������������
// <copyright file="ImageSample.cs" company="EDXLSharp">
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
  /// This represents an image sample on the help page. There's a display template named ImageSample associated with this class.
  /// </summary>
  public class ImageSample
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageSample"/> class.
    /// </summary>
    /// <param name="src">The URL of an image.</param>
    public ImageSample(string src)
    {
      if (src == null)
      {
        throw new ArgumentNullException("src");
      }

      this.Src = src;
    }

    public string Src { get; private set; }

    public override bool Equals(object obj)
    {
      ImageSample other = obj as ImageSample;
      return other != null && this.Src == other.Src;
    }

    public override int GetHashCode()
    {
      return this.Src.GetHashCode();
    }

    public override string ToString()
    {
      return this.Src;
    }
  }
}