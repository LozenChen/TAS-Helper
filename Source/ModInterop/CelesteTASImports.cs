using MonoMod.ModInterop;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class CelesteTasImporter {

    [Initialize(depth: int.MaxValue - 1)]
    public static void InitializeAtFirst() {
        typeof(CelesteTasImports).ModInterop();
    }
}

[ModImportName("CelesteTAS")]
internal static class CelesteTasImports {
    public delegate void AddSettingsRestoreHandlerDelegate(EverestModule module, (Func<object> Backup, Action<object> Restore)? handler);
    public delegate void RemoveSettingsRestoreHandlerDelegate(EverestModule module);
    public delegate void DrawAccurateLineDelegate(Vector2 from, Vector2 to, Color color);

    /// Checks if a TAS is active (i.e. running / paused / etc.)
    public static Func<bool> IsTasActive = null!;

    /// Checks if a TAS is currently actively running (i.e. not paused)
    public static Func<bool> IsTasRunning = null!;

    /// Checks if the current TAS is being recorded with TAS Recorder
    public static Func<bool> IsTasRecording = null!;

    /// Registers custom delegates for backing up and restoring mod setting before / after running a TAS
    /// A `null` handler causes the settings to not be backed up and later restored
    public static AddSettingsRestoreHandlerDelegate AddSettingsRestoreHandler = null!;

    /// De-registers a previously registered handler for the module
    public static RemoveSettingsRestoreHandlerDelegate RemoveSettingsRestoreHandler = null!;

    #region GroupCounter

    public static Func<int> GetGroupCounter = null!;

    public static Action<int> SetGroupCounter = null!;

    #endregion

    #region Savestates

    public delegate object? GetLatestSavestateForFrameDelegate(int frame);
    public delegate int GetSavestateFrameDelegate(object savestate);
    public delegate bool LoadSavestateDelegate(object savestate);

    /// Provides an opaque savestate-handle to the latest savestate before or at the specified frame.
    /// Returns null if no savestate is found
    public static GetLatestSavestateForFrameDelegate GetLatestSavestateForFrame = null!;

    /// Provides the frame into the TAS for the specified savestate-handle
    public static GetSavestateFrameDelegate GetSavestateFrame = null!;

    /// Attempts to load the specified savestate-handle. Returns whether it was successful
    public static LoadSavestateDelegate LoadSavestate = null!;

    #endregion

    #region Rendering

    /// <summary>
    /// Draws an exact line, filling all pixels the line actually intersects. <br/>
    /// Based on the logic of <see cref="Collide.RectToLine(float,float,float,float,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)">Collide.RectToLine</see> and with the assumption that other colliders are grid-aligned.
    /// </summary>
    ///
    /// <remarks>
    /// Available since CelesteTAS v3.44.0
    /// </remarks>
    public static DrawAccurateLineDelegate DrawAccurateLine = null!;

    #endregion
}