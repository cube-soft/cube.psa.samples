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

/* ------------------------------------------------------------------------- */
///
/// Metadata
///
/// <summary>
/// Represents the information shared across processes for a single print
/// job. Carries the data needed for the virtual printer and the launcher
/// to coordinate without direct communication.
/// </summary>
///
/* ------------------------------------------------------------------------- */
public sealed class Metadata
{
    #region Constants

    /* --------------------------------------------------------------------- */
    ///
    /// DirectoryName
    ///
    /// <summary>
    /// The name of the publisher cache subfolder used to exchange data
    /// between the virtual printer and the launcher.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string DirectoryName = "printing";

    /* --------------------------------------------------------------------- */
    ///
    /// FileName
    ///
    /// <summary>
    /// The name of the metadata file. Also used as the lock file that
    /// coordinates exclusive access between the virtual printer and the
    /// launcher.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string FileName = "metadata.json";

    /* --------------------------------------------------------------------- */
    ///
    /// SourceFileName
    ///
    /// <summary>
    /// The name of the file that contains the raw print data written by
    /// the virtual printer and read by the launcher.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string SourceFileName = "source.dat";

    #endregion
}
