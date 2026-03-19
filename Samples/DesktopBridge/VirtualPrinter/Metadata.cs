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
/// Represents metadata of a printing job.
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
    /// Represents the cache directory name.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string DirectoryName = "printing";

    /* --------------------------------------------------------------------- */
    ///
    /// FileName
    ///
    /// <summary>
    /// Represents the filename that contains the metadata of a job.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string FileName = "metadata.json";

    /* --------------------------------------------------------------------- */
    ///
    /// SourceFileName
    ///
    /// <summary>
    /// Represents the filename that contains the printing data.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string SourceFileName = "source.dat";

    #endregion
}
