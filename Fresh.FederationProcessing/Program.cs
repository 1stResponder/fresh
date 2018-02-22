// ———————————————————————–
// <copyright file="Program.cs" company="The MITRE Corporation">
//    Copyright (c) 2010 The MITRE Corporation. All rights reserved.
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
/////////////////////////////////////////////////////////////////////////////////////////////////
// Program.cs - Wrapper class for hosting a windows service
// Project: EDXLSharp_AWSRouter- FederationProcessing
//
// Language:    C#, .NET 4.0
// Platform:    Windows 7, VS 2010
// Author:      Don McGarry The MITRE Corporation
/////////////////////////////////////////////////////////////////////////////////////////////////

// Copyright (c) 2010 The MITRE Corporation. All rights reserved.
//
// NOTICE
// This software was produced for the U. S. Government
// under Contract No. FA8721-09-C-0001, and is
// subject to the Rights in Noncommercial Computer Software
// and Noncommercial Computer Software Documentation Clause
// (DFARS) 252.227-7014 (JUN 1995)

using System.ServiceProcess;

namespace FederationProcessing
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      ServiceBase[] ServicesToRun;
      ServicesToRun = new ServiceBase[] 
            { 
                new FederationProcessing() 
            };
      ServiceBase.Run(ServicesToRun);
    }
  }
}
