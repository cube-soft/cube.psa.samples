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

using Windows.ApplicationModel.Background;
using Windows.Graphics.Printing.PrintSupport;

/* ------------------------------------------------------------------------- */
///
/// PsaExtensionTask
///
/// <summary>
/// Minimal implementation of the Windows.PrintSupportExtension feature.
/// </summary>
///
/// <remarks>
/// This implementation is provided only to satisfy platform requirements.
/// The application does not rely on this feature, but a definition is
/// required to avoid runtime errors. Therefore, this task simply reports
/// a resolved state.
/// </remarks>
///
/* ------------------------------------------------------------------------- */
public sealed class PsaExtensionTask : IBackgroundTask
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
    /// <remarks>
    /// The try-catch in the PrintTicketValidationRequested handler guards
    /// against a race condition specific to in-process background tasks:
    /// if the task Canceled event fires concurrently, the session may be
    /// torn down before the handler completes, causing session API calls to
    /// throw 0x3E3 (ERROR_OPERATION_ABORTED). Without a catch, this exception
    /// propagates into the host process and forces a termination — which is
    /// why a second printer selection in Edge reliably causes a crash.
    /// </remarks>
    ///
    /* --------------------------------------------------------------------- */
    public void Run(IBackgroundTaskInstance task)
    {
        var deferral = task?.GetDeferral();
        if (task is null || deferral is null) return;
        task.Canceled += (_, _) => deferral.Complete();

        var details = task.TriggerDetails as PrintSupportExtensionTriggerDetails;
        if (details?.Session is null)
        {
            deferral.Complete();
            return;
        }

        details.Session.PrintTicketValidationRequested += (_, e) =>
        {
            try { using (e.GetDeferral()) e.SetPrintTicketValidationStatus(WorkflowPrintTicketValidationStatus.Resolved); }
            catch { /* see remarks */ }
        };

        details.Session.Start();
    }
}
