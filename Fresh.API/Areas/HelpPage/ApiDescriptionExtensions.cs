// ———————————————————————–
// <copyright file="ApiDescriptionExtensions.cs" company="EDXLSharp">
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
// ———————————————————————–

using System;
using System.Text;
using System.Web;
using System.Web.Http.Description;

namespace Fresh.API.Areas.HelpPage
{
  public static class ApiDescriptionExtensions
  {
    /// <summary>
    /// Generates an URI-friendly ID for the <see cref="ApiDescription"/>. E.g. "Get-Values-id_name" instead of "GetValues/{id}?name={name}"
    /// </summary>
    /// <param name="description">The <see cref="ApiDescription"/>.</param>
    /// <returns>The ID as a string.</returns>
    public static string GetFriendlyId(this ApiDescription description)
    {
      string path = description.RelativePath;
      string[] urlParts = path.Split('?');
      string localPath = urlParts[0];
      string queryKeyString = null;
      if (urlParts.Length > 1)
      {
        string query = urlParts[1];
        string[] queryKeys = HttpUtility.ParseQueryString(query).AllKeys;
        queryKeyString = string.Join("_", queryKeys);
      }

      StringBuilder friendlyPath = new StringBuilder();
      friendlyPath.AppendFormat("{0}-{1}", description.HttpMethod.Method, localPath.Replace("/", "-").Replace("{", string.Empty).Replace("}", string.Empty));
      if (queryKeyString != null)
      {
        friendlyPath.AppendFormat("_{0}", queryKeyString.Replace('.', '-'));
      }

      return friendlyPath.ToString();
    }
  }
}