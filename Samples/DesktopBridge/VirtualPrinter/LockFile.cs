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
/// 1. LockAsync  — acquire the lock and write the print data.
///    Returns true on success, false on failure.
/// 2. ReleaseAsync — launch the full-trust process and transfer
///    ownership of the lock file to the launcher.
/// Dispose deletes the lock file when the job did not complete or
/// was not handed off to the launcher (HalfLocked or Locked state).
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
    /// Skips acquisition when the lock is already held (HalfLocked or
    /// Locked state). Re-acquires after a completed job (Released state).
    /// TODO: Released 後の再呼び出しを Idle と同等に扱ってよいか要検討。
    /// 意図しない再利用を防ぐため、Released 後は Dispose 後と同様に
    /// ObjectDisposedException を投げるべきかもしれない。
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
    /// Deletes the lock file when the job did not complete or was not
    /// handed off to the launcher (HalfLocked or Locked state).
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
        if (IsLocked(state)) return state;

        var tmp = $"{path}.{Guid.NewGuid()}";
        File.WriteAllText(tmp, "{}");
        await WaitAsync();
        File.Move(tmp, path, overwrite: true);
        return LockFileState.HalfLocked;
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
        if (IsLocked(_state))
        {
            try { File.Delete(path); } catch { }
        }
        _state = LockFileState.Idle;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// IsLocked
    ///
    /// <summary>
    /// Determines whether the lock file is currently held by this
    /// instance and must be deleted on Dispose.
    /// </summary>
    /// <remarks>
    /// Two distinct half-locked states exist: HalfLocked (lock acquired
    /// but action failed) and Locked (action succeeded but ReleaseAsync
    /// not yet called). In both cases the lock file is on disk and this
    /// instance is responsible for cleaning it up.
    /// </remarks>
    ///
    /* --------------------------------------------------------------------- */
    private bool IsLocked(LockFileState state) =>
        state == LockFileState.HalfLocked || state == LockFileState.Locked;

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

    #region Internal Types
    private enum LockFileState
    {
        Idle,       // Lock not yet acquired; CreateAsync will run on next LockAsync
        HalfLocked, // Lock held; action failed — awaiting retry or Dispose
        Locked,     // Lock held; action succeeded — awaiting ReleaseAsync
        Released,   // Ownership transferred to launcher via ReleaseAsync — Dispose is a no-op
    }
    #endregion

    #region Fields
    private bool _disposed;
    private LockFileState _state;
    #endregion
}
