/* ------------------------------------------------------------------------- */
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/* ------------------------------------------------------------------------- */
///
/// CriticalSection
///
/// <summary>
/// Manages exclusive access between the virtual printer and the launcher
/// via a lock file (settings.json). The lock is acquired atomically by
/// writing the file and released by deleting it.
///
/// Use <see cref="InvokeAsync"/> to acquire the lock, execute an action,
/// and automatically transfer ownership to the launcher on success.
/// Implements <see cref="IDisposable"/>: Dispose deletes the lock file
/// if still held (i.e., the action failed or threw).
/// </summary>
///
/* ------------------------------------------------------------------------- */
internal sealed class CriticalSection(string src) : IDisposable
{
    #region Methods

    /* --------------------------------------------------------------------- */
    ///
    /// InvokeAsync
    ///
    /// <summary>
    /// Waits until the lock file is absent, atomically creates it, then
    /// invokes <paramref name="action"/>. If the wait exceeds 30 seconds,
    /// the stale lock file is forcibly removed before proceeding.
    /// On success (<paramref name="action"/> returns true), ownership of
    /// the lock file is transferred to the launcher and Dispose will not
    /// delete it. On failure or exception, Dispose deletes the lock file.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public async Task<bool> InvokeAsync(Func<Task<bool>> action)
    {
        var tmp = $"{src}.{Guid.NewGuid()}";
        File.WriteAllText(tmp, "{}");

        await WaitAsync();

        File.Move(tmp, src, overwrite: true);
        _locked = true;
        var succeeded = await action();
        if (succeeded) _locked = false;

        return succeeded;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Dispose
    ///
    /// <summary>
    /// Releases the lock if still held.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Finalizer
    ///
    /// <summary>
    /// Ensures the lock file is deleted even if Dispose is not called.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    ~CriticalSection() => Dispose(false);

    #endregion

    #region Implementations

    /* --------------------------------------------------------------------- */
    ///
    /// Dispose
    ///
    /// <summary>
    /// Releases the lock if still held. When called from the finalizer,
    /// <paramref name="disposing"/> is false and only unmanaged resources
    /// are released; managed resources are released when true.
    /// </summary>
    ///
    /// <param name="disposing">
    /// true if called from <see cref="Dispose()"/>; false if called from
    /// the finalizer.
    /// </param>
    ///
    /* --------------------------------------------------------------------- */
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (!_locked) return;
        try { File.Delete(src); } catch { }
        _locked = false;
        _disposed = true;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// WaitAsync
    ///
    /// <summary>
    /// Waits for the lock file to be deleted. If the file is not present,
    /// returns immediately. If the wait exceeds 30 seconds, forcibly
    /// deletes the stale lock file before returning.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    private async Task WaitAsync()
    {
        var released = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(Path.GetDirectoryName(src)!, Path.GetFileName(src))
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        watcher.Deleted += (_, _) => released.TrySetResult(true);

        if (!File.Exists(src)) return;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await released.Task.WaitAsync(cts.Token); }
        catch (OperationCanceledException)
        {
            try { File.Delete(src); } catch { }
        }
    }

    #endregion

    #region Fields
    private bool _disposed;
    private bool _locked;
    #endregion
}
