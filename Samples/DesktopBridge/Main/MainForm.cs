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
namespace Cube.Psa.DesktopBridge;

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

/* ------------------------------------------------------------------------- */
///
/// MainForm
///
/// <summary>
/// Represents the main windows.
/// </summary>
///
/* ------------------------------------------------------------------------- */
public partial class MainForm : Form
{
    /* --------------------------------------------------------------------- */
    ///
    /// MainForm
    ///
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public MainForm(string src)
    {
        InitializeComponent();

        var debug = new StringBuilder()
            .AppendLine("https://github.com/cube-soft/cube.psa.samples")
            .AppendLine(src);
        DebugTextBox.Text = debug.ToString();

        DestinationTextBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "TestResult.ps"
        );

        SaveButton.Click += (_, _) => Hook(() =>
        {
            if (DestinationTextBox.Text.Length == 0) return;
            File.Copy(src, DestinationTextBox.Text, true);
            Close();
        }, debug);

        DestinationButton.Click += (_, _) => Hook(() =>
        {
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                OverwritePrompt = true,
                Filter = "PostScript files (*.ps)|*.ps;*.PS|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == DialogResult.OK) DestinationTextBox.Text = dialog.FileName;
        }, debug);
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Hook
    ///
    /// <summary>
    /// Invokes the specified action and catches any exception that occurs.
    /// If an exception is thrown, the error details are appended to the
    /// provided debug buffer and displayed in the debug text box.
    /// </summary>
    ///
    /// <param name="action">The action to invoke.</param>
    /// <param name="debug">Debug buffer.</param>
    ///
    /* --------------------------------------------------------------------- */
    private void Hook(Action action, StringBuilder debug)
    {
        try { action(); }
        catch (Exception err)
        {
            debug.AppendLine(err.ToString());
            DebugTextBox.Text = debug.ToString();
        }
    }
}
