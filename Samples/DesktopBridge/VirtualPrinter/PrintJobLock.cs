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
namespace Cube.Psa.DesktopBridge.VirtualPrinter;

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/* ------------------------------------------------------------------------- */
///
/// PrintJobLock
///
/// <summary>
/// Manages exclusive access between the virtual printer and the launcher
/// via a lock file (settings.json). The lock is acquired atomically by
/// writing the file and released by deleting it.
/// </summary>
///
/* ------------------------------------------------------------------------- */
internal sealed class PrintJobLock(string path)
{
    /* --------------------------------------------------------------------- */
    ///
    /// IsAcquired
    ///
    /// <summary>
    /// Gets a value indicating whether the lock is currently held.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public bool IsAcquired { get; private set; }

    /* --------------------------------------------------------------------- */
    ///
    /// AcquireAsync
    ///
    /// <summary>
    /// Waits until the lock file is absent, then atomically creates it.
    /// If the wait exceeds 30 seconds, the stale lock file is forcibly
    /// removed before proceeding.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public async Task AcquireAsync()
    {
        await WaitForReleaseAsync();

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, "{}");
        File.Move(tmp, path, overwrite: true);
        IsAcquired = true;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Release
    ///
    /// <summary>
    /// Releases the lock by deleting the lock file.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public void Release()
    {
        try { File.Delete(path); } catch { }
        IsAcquired = false;
    }

    /* --------------------------------------------------------------------- */

    private async Task WaitForReleaseAsync()
    {
        var released = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        watcher.Deleted += (_, _) => released.TrySetResult(true);

        if (!File.Exists(path)) return;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await released.Task.WaitAsync(cts.Token); }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[{nameof(PrintJobLock)}] Timed out waiting for lock release; removing stale file.");
            try { File.Delete(path); } catch { }
        }
    }
}
