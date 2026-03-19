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
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Graphics.Printing.Workflow;
using Windows.Storage;
using Windows.Storage.Streams;

/* ------------------------------------------------------------------------- */
///
/// PsaVirtualPrinterTask
///
/// <summary>
/// Minimal implementation of the PrintSupportVirtualPrinterWorkflow feature.
/// </summary>
///
/* ------------------------------------------------------------------------- */
public sealed class PsaVirtualPrinterTask : IBackgroundTask
{
    /* --------------------------------------------------------------------- */
    ///
    /// Run
    ///
    /// <summary>
    /// Performs the work of a background task.
    /// </summary>
    ///
    /// <param name="task">
    /// An interface to an instance of the background task.
    /// </param>
    ///
    /* --------------------------------------------------------------------- */
    public void Run(IBackgroundTaskInstance task)
    {
        var deferral = task?.GetDeferral();
        if (task is null || deferral is null) return;
        task.Canceled += (_, _) => deferral.Complete();

        var details = task?.TriggerDetails as PrintWorkflowVirtualPrinterTriggerDetails;
        var session = details?.VirtualPrinterSession;
        if (session is null) return;

        session.VirtualPrinterDataAvailable += async (_, e) =>
        {
            var done = false;
            try { done = await InvokeAsync(e); }
            finally
            {
                e.CompleteJob(done ? PrintWorkflowSubmittedStatus.Succeeded : PrintWorkflowSubmittedStatus.Failed);
                deferral.Complete();
            }
        };

        session.Start();
    }

    /* --------------------------------------------------------------------- */
    ///
    /// InvokeAsync
    ///
    /// <summary>
    /// Writes the incoming print data to the shared publisher cache and
    /// launches the full-trust launcher process to handle conversion.
    /// </summary>
    ///
    /// <param name="e">
    /// Event arguments providing access to the incoming XPS content.
    /// </param>
    ///
    /// <returns>
    /// true on success; false otherwise.
    /// </returns>
    ///
    /* --------------------------------------------------------------------- */
    private static async Task<bool> InvokeAsync(PrintWorkflowVirtualPrinterDataAvailableEventArgs e)
    {
        var dir = ApplicationData.Current.GetPublisherCacheFolder(Metadata.DirectoryName);
        if (dir is null) return false;

        var metadata = new Metadata
        {
            JobTitle  = e.Configuration.JobTitle,
            SessionId = e.Configuration.SessionId,
            AppName   = e.Configuration.SourceAppDisplayName,
        };

        using var file = new LockFile(Path.Combine(dir.Path, Metadata.FileName));
        var done = await file.LockAsync(async () =>
        {
            var dest = await dir.CreateFileAsync(Metadata.SourceFileName, CreationCollisionOption.ReplaceExisting);
            if (dest is null) return false;

            using var s = await dest.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAndCloseAsync(e.SourceContent.GetInputStream(), s.GetOutputStreamAt(s.Size));
            return true;
        }, metadata);

        if (done) await file.ReleaseAsync(LaunchAsync);
        return done;
    }

    /* --------------------------------------------------------------------- */
    ///
    /// LaunchAsync
    ///
    /// <summary>
    /// Launches the full-trust launcher process to handle conversion.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    private static async Task LaunchAsync() => await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Launcher");
}
