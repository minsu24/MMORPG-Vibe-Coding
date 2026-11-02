using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class GuardChecks
{
    private static int Main()
    {
        string folder = "Artifacts/AdminSaveProtection";
        using var stream = File.OpenRead(folder + "/Assembly-CSharp.dll");
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();
        var codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null))
            .ToDictionary(o => unchecked((ushort)o.Value));
        string Name(EntityHandle handle)
        {
            if (handle.Kind == HandleKind.MethodDefinition) return md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)handle).Name);
            if (handle.Kind == HandleKind.MemberReference) return md.GetString(md.GetMemberReference((MemberReferenceHandle)handle).Name);
            return handle.Kind.ToString();
        }
        var positions = new Dictionary<string, int>();
        List<string> Read(string typeName, string methodName)
        {
            var type = md.TypeDefinitions.Select(h => md.GetTypeDefinition(h)).Single(t => md.GetString(t.Name) == typeName);
            var method = type.GetMethods().Select(h => md.GetMethodDefinition(h)).Single(m => md.GetString(m.Name) == methodName);
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
            var result = new List<string>();
            for (int p = 0; p < il.Length;)
            {
                int position = p;
                ushort value = il[p++]; if (value == 0xfe) value = (ushort)(0xfe00 | il[p++]);
                var code = codes[value]; string item = code.Name;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget: item = code.Name + "@" + (p + 1 + (sbyte)il[p]); p++; break;
                    case OperandType.InlineBrTarget: item = code.Name + "@" + (p + 4 + BitConverter.ToInt32(il, p)); p += 4; break;
                    case OperandType.ShortInlineI: case OperandType.ShortInlineVar: p++; break;
                    case OperandType.InlineVar: p += 2; break;
                    case OperandType.InlineI8: case OperandType.InlineR: p += 8; break;
                    case OperandType.InlineSwitch: int count = BitConverter.ToInt32(il, p); p += 4 + count * 4; break;
                    default:
                        int token = BitConverter.ToInt32(il, p);
                        if (code.OperandType == OperandType.InlineMethod) item = Name(MetadataTokens.EntityHandle(token));
                        if (code.OperandType == OperandType.InlineString) item = "string:" + md.GetUserString(MetadataTokens.UserStringHandle(token & 0xffffff));
                        p += 4; break;
                }
                result.Add(item);
                positions.TryAdd(methodName + ":" + item, position);
            }
            return result;
        }
        File.WriteAllText(folder + "/Validation.txt", "Compiled IL save protection checks; Unity gameplay was not executed.\n");
        void Check(bool ok, string message)
        {
            File.AppendAllText(folder + "/Validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new Exception(message);
        }
        void Before(string type, string method, string guard, string action)
        {
            var il = Read(type, method); int g = il.IndexOf(guard), a = il.IndexOf(action);
            Check(g >= 0 && a > g, type + "." + method + ": " + guard + " precedes " + action);
        }
        var save = Read("SaveGameService", "SaveNow");
        File.WriteAllLines(folder + "/SaveNow-IL.txt", save);
        int flag = save.IndexOf("get_IsAdminSession"), capture = save.IndexOf("Capture");
        Check(flag == 0 && capture > flag && save.Take(capture).Contains("ret") && save.Take(capture).Contains("ldc.i4.0"),
            "SaveNow checks administrator protection first and returns false before capturing or writing state");
        Before("SaveGameService", "Update", "get_IsAdminSession", "get_unscaledTime");
        Before("SaveGameService", "BeginAdminSession", "get_IsAdminSession", "SaveNow");
        Before("SaveGameService", "BeginAdminSession", "SaveNow", "set_IsAdminSession");
        Before("SaveGameService", "Attach", "set_IsAdminSession", "Read");
        Check(Read("SaveGameService", "ResetStatics").Contains("set_IsAdminSession"), "new play sessions reset the protection flag");
        foreach (var method in new[] { "OnApplicationPause", "OnApplicationFocus", "OnApplicationQuit", "PrepareSelection" })
            Check(Read("SaveGameService", method).Contains("SaveNow"), method + " uses the guarded SaveNow entry point");
        Before("PlayerAdminController", "DrawWindow", "BeginAdminSession", "AddExperience");
        Before("PlayerAdminController", "DrawWindow", "BeginAdminSession", "RestoreHealth");
        Before("PlayerAdminController", "DrawWindow", "BeginAdminSession", "KillForTesting");
        Before("PlayerAdminController", "ChangeScene", "BeginAdminSession", "TryLoad");
        Before("PlayerAdminController", "TeleportToSavedPosition", "BeginAdminSession", "TeleportToAndRetarget");
        Check(Read("PlayerAdminController", "SetGhostMode").Contains("BeginAdminSession"), "enabling ghost movement protects the save");
        Check(!Read("PlayerAdminController", "OnGUI").Contains("BeginAdminSession"), "displaying the administrator panel alone does not disable saving");
        Check(!Read("PlayerAdminController", "SetGhostMode").Contains("set_IsAdminSession"), "turning ghost mode off cannot clear administrator protection");
        Console.Write(File.ReadAllText(folder + "/Validation.txt"));
        return 0;
    }
}
