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

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DestinationTextBox.Text = Path.Combine(desktop, "TestResult.ps");

        SaveButton.Click += (_, _) =>
        {
            if (DestinationTextBox.Text.Length == 0) return;
            File.Copy(src, DestinationTextBox.Text, true);
            Close();
        };

        DestinationButton.Click += (_, _) =>
        {
            var dialog = new SaveFileDialog
            {
                AddExtension    = true,
                OverwritePrompt = true,
                Filter          = "PostScript files (*.ps)|*.ps;*.PS|All files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == DialogResult.OK) DestinationTextBox.Text = dialog.FileName;
        };
    }
}
