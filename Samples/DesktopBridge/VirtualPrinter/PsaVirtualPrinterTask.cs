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
/// Minimal implementation of the Windows.PrintSupportVirtualPrinterWorkflow
/// feature for XPS-to-PDF conversion.
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
            var status = PrintWorkflowSubmittedStatus.Failed;
            try { status = await ProcessJobAsync(e); }
            finally
            {
                e.CompleteJob(status);
                deferral.Complete();
            }
        };

        session.Start();
    }

    /* --------------------------------------------------------------------- */
    ///
    /// ProcessJobAsync
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
    /// <see cref="PrintWorkflowSubmittedStatus.Succeeded"/> on success;
    /// <see cref="PrintWorkflowSubmittedStatus.Failed"/> otherwise.
    /// </returns>
    ///
    /* --------------------------------------------------------------------- */
    private static async Task<PrintWorkflowSubmittedStatus> ProcessJobAsync(PrintWorkflowVirtualPrinterDataAvailableEventArgs e)
    {
        var dir = ApplicationData.Current.GetPublisherCacheFolder("printing");
        if (dir is null) return PrintWorkflowSubmittedStatus.Failed;

        using var file = new LockFile(Path.Combine(dir.Path, "settings.json"));
        var locked = await file.LockAsync(async () =>
        {
            var dest = await dir.CreateFileAsync("source.ps", CreationCollisionOption.ReplaceExisting);
            if (dest is null) return false;

            using var stream = await dest.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAndCloseAsync(e.SourceContent.GetInputStream(), stream.GetOutputStreamAt(stream.Size));

            return true;
        });

        if (locked)
        {
            await file.ReleaseAsync(async () => await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Launcher"));
            return PrintWorkflowSubmittedStatus.Succeeded;
        }
        else return PrintWorkflowSubmittedStatus.Failed;
    }
}
