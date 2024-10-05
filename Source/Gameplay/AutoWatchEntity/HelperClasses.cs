using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class InfoParser {

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SignedString(this float f) {
        return f.ToString("+0.00;-0.00;0.00");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string IntSignedString(this float f) {
        return f.ToString("+0;-0;0");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ToCeilingFrames(float seconds) {
        return (int)Math.Ceiling(seconds / Engine.DeltaTime);
    }


    internal static int ToFrameData(this float seconds) {
        return ToCeilingFrames(seconds);
    }
    internal static string ToFrame(this float seconds) {
        if (seconds <= 0) {
            return "";
        }
        return ToCeilingFrames(seconds).ToString();
    }
    internal static string ToFrameAllowZero(this float seconds) {
        return ToCeilingFrames(seconds).ToString();
    }

    internal static string ToFrameMinusOne(this float seconds) {
        if (seconds <= 0) {
            return "";
        }
        return (ToCeilingFrames(seconds) - 1).ToString();
    }

    internal static string ToFrame(this int frames) {
        return frames.ToString();
    }

    internal static string PositionToAbsoluteSpeed(this Vector2 vector) {
        if (Format.Speed_UsePixelPerSecond) {
            return (vector.Length() / Engine.DeltaTime).ToString("0.00");
        }
        else { // pixel per frame
            return vector.Length().ToString("0.00");
        }
    }

    internal static string Speed2ToSpeed2(this Vector2 speed) {
        if (IsTiny(speed.X)) {
            if (IsTiny(speed.Y)) {
                return "";
            }
            return speed.Y.SpeedToSpeed();
        }
        else if (IsTiny(speed.Y)) {
            return speed.X.SpeedToSpeed();
        }
        return $"({speed.X.SpeedToSpeed()}, {speed.Y.SpeedToSpeed()})";
    }

    internal static string Speed2ToSpeed2ButBreakline(this Vector2 speed) {
        if (IsTiny(speed.X)) {
            if (IsTiny(speed.Y)) {
                return "";
            }
            return speed.Y.SpeedToSpeed();
        }
        else if (IsTiny(speed.Y)) {
            return speed.X.SpeedToSpeed();
        }
        return $"({speed.X.SpeedToSpeed()},\n {speed.Y.SpeedToSpeed()})";
    }

    internal static string SpeedToSpeed(this float speed) { // in case we do have a speed field
        if (Format.Speed_UsePixelPerSecond) {
            return speed.SignedString();
        }
        else { // pixel per frame
            return (speed * Engine.DeltaTime).SignedString();
        }
    }

    private static string PositionToSignedSpeedX(this float f) {
        if (Format.Speed_UsePixelPerSecond) {
            return (f / Engine.DeltaTime).SignedString();
        }
        else {
            return f.SignedString();
        }
    }

    internal static string Positon2ToSignedSpeed(this Vector2 deltaPosition, bool allowZero = false, bool breakline = false) {
        if (!allowZero) {
            if (IsTiny(deltaPosition.X)) {
                if (IsTiny(deltaPosition.Y)) {
                    return "";
                }
                return PositionToSignedSpeedX(deltaPosition.Y);
            }
            else if (IsTiny(deltaPosition.Y)) {
                return PositionToSignedSpeedX(deltaPosition.X);
            }
        }

        if (breakline) {
            return $"X: {PositionToSignedSpeedX(deltaPosition.X)}\nY: {PositionToSignedSpeedX(deltaPosition.Y)}";
        }
        else {
            return $"({PositionToSignedSpeedX(deltaPosition.X)}, {PositionToSignedSpeedX(deltaPosition.Y)})";
        }
    }

    internal static string OffsetToString(this Vector2 deltaPosition, bool allowZero = false, bool breakline = false) {
        if (!allowZero) {
            if (IsTiny(deltaPosition.X)) {
                if (IsTiny(deltaPosition.Y)) {
                    return "";
                }
                return SignedString(deltaPosition.Y);
            }
            else if (IsTiny(deltaPosition.Y)) {
                return SignedString(deltaPosition.X);
            }
        }
        if (breakline) {
            return $"X: {SignedString(deltaPosition.X)}\nY: {SignedString(deltaPosition.Y)}";
        }
        else {
            return $"({SignedString(deltaPosition.X)}, {SignedString(deltaPosition.Y)})";
        }
    }

    internal static string FloatVector2ToString(this Vector2 vector2) {
        return $"({SignedString(vector2.X)}, {SignedString(vector2.Y)})";
    }
    internal static string IntVector2ToString(this Vector2 vector2) {
        return $"({IntSignedString(vector2.X)}, {IntSignedString(vector2.Y)})";
    }

    internal static string AbsoluteFloatToString(this float f) {
        return f.ToString("0.00");
    }
    internal static string SignedFloatToString(this float f) {
        return SignedString(f);
    }

    internal static string SignedIntToString(this float f) {
        return IntSignedString(f);
    }

    internal const float epsilon = 1E-6f;

    internal const float Minus_epsilon = -1E-6f;

    private static bool IsTiny(float f) {
        return f < epsilon && f > Minus_epsilon;
    }
}

internal static class DashCode {
    public static readonly string[] DashCodes = ["L", "UL", "U", "UR", "R", "DR", "D", "DL", "_"];

    public static readonly Vector2[] Directions = [-Vector2.UnitX, -Vector2.One, -Vector2.UnitY, new Vector2(1f, -1f), Vector2.UnitX, Vector2.One, Vector2.UnitY, new Vector2(-1f, 1f), Vector2.Zero];

    public const int MemorialHelperOffset = 0;

    public static string ToCode(string str) {
        // return a standard form
        return str switch {
            "L" or "UL" or "U" or "UR" or "R" or "DR" or "D" or "DL" or "_" => str,
            "LU" => "UL",
            "RU" => "UR",
            "LD" => "DL",
            "RD" => "DR",
            "" => "_",
            _ => "?"
        };
    }
    public static string ToCode(Vector2 vec) {
        // this assumes vec = Calc.Sign(vec);
        vec = Calc.Sign(vec);
        return (vec.X, vec.Y) switch {
            (1, 1) => "DR",
            (1, 0) => "R",
            (1, -1) => "UR",
            (0, 1) => "D",
            (0, -1) => "U",
            (-1, 1) => "DL",
            (-1, 0) => "L",
            (-1, -1) => "UL",
            _ => "_",
        };
    }

    public static string ToCode(int angle, int offset) {
        int num = (angle + offset) % 8;
        if (num < 0) {
            num = (num + 8) % 8;
        }
        return DashCodes[num];
    }
}

internal static class CoroutineFinder {

    // note that if it's hooked, then the name will change
    // like Celeste.FallingBlock+<Sequence>d__21 -> HonlyHelper.RisingBlock+<FallingBlock_Sequence>d__5
    // even if the block itself is a FallingBlock instead of a RisingBlock
    public static bool FindCoroutineComponent(this Entity entity, string compiler_generated_class_name, out Tuple<Coroutine, System.Collections.IEnumerator> pair, bool logError = false) {
        // e.g. compiler_generated_class_name = Celeste.FallingBlock+<Sequence>d__21

        foreach (Component c in entity.Components) {
            if (c is not Coroutine coroutine) {
                continue;
            }
            if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().FullName == compiler_generated_class_name) is System.Collections.IEnumerator func) {
                pair = Tuple.Create(coroutine, func);
                return true;
            }
        }
        if (logError) {
            Logger.Log(LogLevel.Error, "TASHelper", $"AutoWatchEntity: can't find {compiler_generated_class_name} of {entity.GetEntityId()}");
        }
        pair = null;
        return false;

    }

    public static System.Collections.IEnumerator FindIEnumrator(this Coroutine coroutine, string compiler_generated_class_name) {
        if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().Name == compiler_generated_class_name) is System.Collections.IEnumerator func) {
            return func;
        }
        return null;
    }
}




