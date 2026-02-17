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
namespace Cube.Psa.Minimal.VirtualPrinter;

using System;
using Windows.ApplicationModel.Background;
using Windows.Graphics.Printing.Workflow;
using Windows.Storage;

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

            try
            {
                var src = e.SourceContent;
                if (src.ContentType != "application/oxps") throw new NotSupportedException("Microsoft only provides conversion from XPS");

                var file = await e.GetTargetFileAsync();
                var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                var converter = e.GetPdlConverter(PrintWorkflowPdlConversionType.XpsToPdf);
                await converter.ConvertPdlAsync(e.GetJobPrintTicket(), src.GetInputStream(), stream.GetOutputStreamAt(0));

                status = PrintWorkflowSubmittedStatus.Succeeded;
            }
            finally
            {
                e.CompleteJob(status);
                deferral.Complete();
            }
        };

        session.Start();
    }
}
