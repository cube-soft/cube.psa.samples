﻿/* ------------------------------------------------------------------------- */
//
// Copyright (c) 2010 CubeSoft, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
/* ------------------------------------------------------------------------- */
namespace Cube.Psa.DesktopBridge;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

/* ------------------------------------------------------------------------- */
///
/// Program
///
/// <summary>
/// Represents the main program.
/// </summary>
///
/* ------------------------------------------------------------------------- */
internal class Program
{
    /* --------------------------------------------------------------------- */
    ///
    /// Main
    ///
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    static async Task Main()
    {
        var dir = CacheFolder.Get();
        if (dir is null) return;

        var src  = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dat");
        var meta = default(Metadata);

        try
        {
            var raw = Path.Combine(dir.Path, Metadata.SourceFileName);
            if (!File.Exists(raw)) return;

            File.Move(raw, src);
            meta = await Metadata.LoadAsync(Path.Combine(dir.Path, Metadata.FileName));
            File.Delete(Path.Combine(dir.Path, Metadata.FileName));
        }
        finally { File.Delete(Path.Combine(dir.Path, Metadata.LockFileName)); }
            
        try { await (Process.Start(Create(src, meta))?.WaitForExitAsync() ?? Task.CompletedTask); }
        finally { if (File.Exists(src)) File.Delete(src); }
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Create
    ///
    /// <summary>
    /// Creates a new instance of the ProcessStartInfo class.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    static ProcessStartInfo Create(string src, Metadata? metadata)
    {
        var dest = new ProcessStartInfo
        {
            FileName = "CubePsaApp.exe",
            UseShellExecute = false,
        };

        dest.ArgumentList.Add(src);

        if (metadata is not null)
        {
            dest.ArgumentList.Add("-JobTitle");
            dest.ArgumentList.Add(metadata.JobTitle);
            dest.ArgumentList.Add("-SessionID");
            dest.ArgumentList.Add(metadata.SessionId);
            dest.ArgumentList.Add("-AppName");
            dest.ArgumentList.Add(metadata.AppName);
        }

        return dest;
    }
}
