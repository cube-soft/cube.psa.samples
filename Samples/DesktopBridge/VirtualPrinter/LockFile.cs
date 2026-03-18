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
/// Typical call sequence per job:
/// <list type="number">
///   <item><see cref="LockAsync"/>: acquire the lock and write the print
///   data. Returns true on success (<see cref="LockFileState.Locked"/>),
///   false on failure (<see cref="LockFileState.Pending"/>).</item>
///   <item><see cref="ReleaseAsync"/>: launch the full-trust process and
///   transfer ownership of the lock file to the launcher
///   (<see cref="LockFileState.Released"/>).</item>
/// </list>
/// Dispose deletes the lock file when the state is
/// <see cref="LockFileState.Pending"/> or <see cref="LockFileState.Locked"/>
/// (i.e. the job did not complete or was not handed off to the launcher).
/// </remarks>
///
/* ------------------------------------------------------------------------- */
internal sealed class LockFile(string path) : IDisposable
{
    #region Methods

    /* --------------------------------------------------------------------- */
    ///
    /// LockAsync
    ///
    /// <summary>
    /// Acquires the lock if not already held, then executes
    /// <paramref name="action"/>. Returns true on success; false on
    /// failure. On failure the lock remains held so the action can be
    /// retried without re-acquiring.
    /// </summary>
    ///
    /// <remarks>
    /// Skips acquisition when the lock is already held
    /// (<see cref="LockFileState.Pending"/> or
    /// <see cref="LockFileState.Locked"/>). Re-acquires after a
    /// completed job (<see cref="LockFileState.Released"/>).
    /// </remarks>
    ///
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this instance has already been disposed.
    /// </exception>
    ///
    /* --------------------------------------------------------------------- */
    public async Task<bool> LockAsync(Func<Task<bool>> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _state = await CreateAsync(_state);
        var done = await action();
        if (done) _state = LockFileState.Locked;
        return done;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// ReleaseAsync
    ///
    /// <summary>
    /// Executes <paramref name="action"/> (typically launching the
    /// full-trust process) and transfers ownership of the lock file to
    /// the launcher.
    /// </summary>
    ///
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this instance has already been disposed.
    /// </exception>
    ///
    /* --------------------------------------------------------------------- */
    public async Task ReleaseAsync(Func<Task> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await action();
        _state = LockFileState.Released;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Dispose
    ///
    /// <summary>
    /// Releases the lock if still held.
    /// </summary>
    /// <remarks>
    /// Deletes the lock file when the state is
    /// <see cref="LockFileState.Pending"/> or
    /// <see cref="LockFileState.Locked"/> — i.e. the job did not
    /// complete or was not handed off to the launcher.
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
    /// CreateAsync
    ///
    /// <summary>
    /// Waits for any existing lock file to be deleted, then atomically
    /// creates the lock file by writing a temporary file and renaming it.
    /// Skips acquisition when the lock is already held.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    private async Task<LockFileState> CreateAsync(LockFileState state)
    {
        if (state == LockFileState.Pending || state == LockFileState.Locked) return state;

        var tmp = $"{path}.{Guid.NewGuid()}";
        File.WriteAllText(tmp, "{}");
        await WaitAsync();
        File.Move(tmp, path, overwrite: true);
        return LockFileState.Pending;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Dispose
    ///
    /// <summary>
    /// Deletes the lock file if the job did not complete or was not
    /// handed off. When called from the finalizer,
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
        if (_state == LockFileState.Pending || _state == LockFileState.Locked)
            try { File.Delete(path); } catch { }
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
        Idle,     // Lock not yet acquired; CreateAsync will run on next LockAsync
        Pending,  // Lock held; action failed — Dispose will delete the lock file
        Locked,   // Lock held; action succeeded — awaiting ReleaseAsync
        Released, // Ownership transferred to launcher via ReleaseAsync — Dispose is a no-op
    }
    #endregion

    #region Fields
    private bool _disposed;
    private LockFileState _state;
    #endregion
}
