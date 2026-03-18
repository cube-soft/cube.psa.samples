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
/// LockFile
///
/// <summary>
/// Manages exclusive access between the virtual printer and the launcher
/// via a lock file (settings.json). The lock is acquired atomically by
/// writing the file and released by deleting it.
/// </summary>
///
/// <remarks>
/// <see cref="InvokeAsync"/> may be called multiple times on the same
/// instance. Internal state determines whether lock acquisition is
/// needed:
/// <list type="bullet">
///   <item><see cref="LockFileState.Idle"/> or <see cref="LockFileState.Transferred"/>:
///   acquire the lock before executing the action.</item>
///   <item><see cref="LockFileState.Pending"/>: the lock is already held
///   (previous action failed); execute the action without re-acquiring.
///   </item>
/// </list>
/// Dispose deletes the lock file only when the state is
/// <see cref="LockFileState.Pending"/> (i.e. an action failed and the
/// lock was never transferred to the launcher).
/// </remarks>
///
/* ------------------------------------------------------------------------- */
internal sealed class LockFile(string path) : IDisposable
{
    #region Methods

    /* --------------------------------------------------------------------- */
    ///
    /// InvokeAsync
    ///
    /// <summary>
    /// Executes <paramref name="action"/> under the lock. Returns true
    /// on success, at which point ownership of the lock file is
    /// transferred to the launcher. Returns false on failure; the lock
    /// remains held so the action can be retried without re-acquiring.
    /// </summary>
    /// <remarks>
    /// Acquires the lock first unless it is already held from a previous
    /// failed action.
    /// </remarks>
    ///
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this instance has already been disposed.
    /// </exception>
    ///
    /* --------------------------------------------------------------------- */
    public async Task<bool> InvokeAsync(Func<Task<bool>> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_state != LockFileState.Pending) await AcquireAsync();

        var succeeded = await action();
        _state = succeeded ? LockFileState.Transferred : LockFileState.Pending;

        return succeeded;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Dispose
    ///
    /// <summary>
    /// Releases the lock if still held.
    /// </summary>
    /// <remarks>
    /// The lock file is deleted only when an action previously failed and
    /// ownership was never transferred to the launcher.
    /// </remarks>
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
    ~LockFile() => Dispose(false);

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
        var tmp = $"{path}.{Guid.NewGuid()}";
        File.WriteAllText(tmp, "{}");
        await WaitAsync();
        File.Move(tmp, path, overwrite: true);
        _state = LockFileState.Pending;
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
    /// <remarks>
    /// Deletes the lock file only when the state is
    /// <see cref="LockFileState.Pending"/> (action failed; ownership
    /// not yet transferred to the launcher).
    /// </remarks>
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
        if (_state == LockFileState.Pending)
        {
            try { File.Delete(path); } catch { }
        }
        _state = LockFileState.Idle;
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
            try { File.Delete(path); } catch { }
        }
    }

    #endregion

    #region Types
    private enum LockFileState
    {
        Idle,        // Lock not yet acquired; AcquireAsync will run on next InvokeAsync
        Pending,     // Lock held; action failed — Dispose will delete the lock file
        Transferred, // Ownership transferred to launcher — Dispose is a no-op
    }
    #endregion

    #region Fields
    private bool _disposed;
    private LockFileState _state;
    #endregion
}
