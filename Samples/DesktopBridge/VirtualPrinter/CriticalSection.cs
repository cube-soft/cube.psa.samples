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
/// <see cref="InvokeAsync"/> may be called multiple times on the same
/// instance. When the lock is not yet held, it is acquired first; when
/// the lock is already held (e.g. a previous action returned false),
/// the action is executed directly without re-acquiring. On success
/// (action returns true), ownership of the lock file is transferred to
/// the launcher. On failure (false or exception), the lock remains held
/// and Dispose will delete it.
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
    /// Executes <paramref name="action"/> under the lock. If the lock is
    /// not yet held, it is acquired first by atomically creating the lock
    /// file (waiting up to 30 seconds for any existing lock to be
    /// released). If the lock is already held, the action is executed
    /// directly without re-acquiring.
    /// Returns true when the action succeeds and ownership has been
    /// transferred to the launcher; false when it fails and the lock is
    /// still held by this instance.
    /// </summary>
    ///
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this instance has already been disposed.
    /// </exception>
    ///
    /* --------------------------------------------------------------------- */
    public async Task<bool> InvokeAsync(Func<Task<bool>> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_locked) await AcquireAsync();

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
    /// AcquireAsync
    ///
    /// <summary>
    /// Waits for any existing lock file to be deleted, then atomically
    /// creates the lock file by writing a temporary file and renaming it.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    private async Task AcquireAsync()
    {
        var tmp = $"{src}.{Guid.NewGuid()}";
        File.WriteAllText(tmp, "{}");
        await WaitAsync();
        File.Move(tmp, src, overwrite: true);
        _locked = true;
    }

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
        _disposed = true;
        if (!_locked) return;
        try { File.Delete(src); } catch { }
        _locked = false;
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
