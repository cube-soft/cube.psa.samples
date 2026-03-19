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

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

/* ------------------------------------------------------------------------- */
///
/// Metadata
///
/// <summary>
/// Represents the information shared across processes for a print job.
/// Carries the data needed for the virtual printer and the launcher
/// to coordinate without direct communication.
/// </summary>
///
/* ------------------------------------------------------------------------- */
public sealed class Metadata
{
    #region Methods

    /* --------------------------------------------------------------------- */
    ///
    /// LoadAsync
    ///
    /// <summary>
    /// Loads a Metadata instance from the specified JSON file. Returns
    /// null if the file does not exist or cannot be deserialized.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public static async Task<Metadata?> LoadAsync(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Metadata>(stream, _options);
        }
        catch { return null; }
    }

    /* --------------------------------------------------------------------- */
    ///
    /// SaveAsync
    ///
    /// <summary>
    /// Saves this instance to the specified path as a JSON file.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public async Task SaveAsync(string path)
    {
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, this, _options);
    }

    #endregion

    #region Properteis

    /* --------------------------------------------------------------------- */
    ///
    /// JobTitle
    ///
    /// <summary>
    /// Gets or sets the title for this print job.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public string JobTitle { get; set; } = string.Empty;

    /* --------------------------------------------------------------------- */
    ///
    /// SessionId
    ///
    /// <summary>
    /// Gets or sets the session ID for this print job.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public string SessionId { get; set; } = string.Empty;

    /* --------------------------------------------------------------------- */
    ///
    /// AppName
    ///
    /// <summary>
    /// Gets or sets the display name of the external application that
    /// initiated the print job.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public string AppName { get; set; } = string.Empty;

    #endregion

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
    /// The name of the metadata file written by the virtual printer and
    /// read by the launcher.
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

    /* --------------------------------------------------------------------- */
    ///
    /// LockFileName
    ///
    /// <summary>
    /// The name of the lock file used to coordinate exclusive access
    /// between the virtual printer and the launcher.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public const string LockFileName = "job.lock";

    #endregion

    #region Fields
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    #endregion
}
