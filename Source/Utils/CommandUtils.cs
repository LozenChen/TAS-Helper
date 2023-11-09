using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Utils.CommandUtils;

public static class CommandUtils {

    [Load]
    private static void Load() {
        On.Monocle.Commands.BuildCommandsList += OnBuildCommandsList;
    }

    [Unload]
    private static void Unload() {
        On.Monocle.Commands.BuildCommandsList -= OnBuildCommandsList;
    }

    [Initialize]
    private static void Initialize() {
        Monocle.Engine.Commands.ReloadCommandsList(); // I'm not sure if Monocle.Engine.Commands is initialized before our OnHook applied... so, just in case.
    }

    private static void OnBuildCommandsList(On.Monocle.Commands.orig_BuildCommandsList orig, Monocle.Commands Commands) {
        CollectCommandEx(Commands);
        orig(Commands);
    }
    private static void CollectCommandEx(Monocle.Commands Commands) {
        IEnumerable<MethodInfo> methods = typeof(CommandUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetCustomAttribute<Command_StringParameter>() != null && info.GetParameters().Length == 1 && info.GetParameters()[0].ParameterType == typeof(string)));
        foreach (MethodInfo method in methods) {
            Command_StringParameter commandData = method.GetCustomAttribute<Command_StringParameter>();
            ParameterInfo parameterInfo = method.GetParameters()[0];
            Monocle.Commands.CommandInfo dictionaryValue = default;

            dictionaryValue.Help = commandData.Help;
            dictionaryValue.Usage = "[" + parameterInfo.Name + ":string]";

            if (!parameterInfo.HasDefaultValue) {
                dictionaryValue.Action = [MethodImpl(MethodImplOptions.NoInlining)] (string[] args) => {
                    // in case you need to type two consecutive spaces or more, you can type \s instead
                    Commands.InvokeMethod(method, new object[] { string.Join(" ", args).Replace("\\s", " ") });
                };
            }
            else {
                string defaultPara = (string)parameterInfo.DefaultValue;
                dictionaryValue.Action = [MethodImpl(MethodImplOptions.NoInlining)] (string[] args) => {
                    string str = string.Join(" ", args).Replace("\\s", " ");
                    if (defaultPara.IsNullOrEmpty()) {
                        str = defaultPara;
                    }
                    Commands.InvokeMethod(method, new object[] { str });
                };
            }
            Commands.commands[commandData.Name.ToLowerInvariant()] = dictionaryValue;
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal class Command_StringParameter : Attribute {
    // it seems Monocle.Command attribute doesn't support string[] args parameters
    // and we can't type a string with spaces correctly
    // so this attribute help with those commands which accept a string parameter arg where arg may contain spaces

    // can only apply to func of form
    // public/private static TValue func(string arg){...}
    public string Name;

    public string Help;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Command_StringParameter(string name, string help) {
        Name = name;
        Help = help;
    }
}