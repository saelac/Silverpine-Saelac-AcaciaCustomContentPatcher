using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AcaciaCustomContentPatcher
{
    public static class Patcher
    {
        public static IEnumerable<string> TargetDLLs { get; } =
            new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            TypeDefinition editor = assembly.MainModule.Types.Single(
                type => type.Name == "GeneralCustomContentEditorUI");

            int patchedChecks = 0;
            patchedChecks += PatchCallback(
                editor,
                "<>c__DisplayClass7_0",
                "<EditCustomContentDefinition>b__5");
            patchedChecks += PatchCallback(
                editor,
                "<>c__DisplayClass7_1",
                "<EditCustomContentDefinition>b__7");

            if (patchedChecks != 2)
            {
                throw new InvalidOperationException(
                    $"Expected to patch 2 Acacia editor restrictions, patched {patchedChecks}.");
            }

            PatchWhitespacePersonalityValidation(assembly.MainModule, editor);
        }

        private static int PatchCallback(
            TypeDefinition editor,
            string nestedTypeName,
            string methodName)
        {
            TypeDefinition callbacks = editor.NestedTypes.Single(
                type => type.Name == nestedTypeName);
            MethodDefinition callback = callbacks.Methods.Single(
                method => method.Name == methodName);

            int patched = 0;
            foreach (Instruction instruction in callback.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldstr &&
                    instruction.Operand is string value &&
                    value == "Acacia")
                {
                    instruction.Operand =
                        "__AcaciaCustomContent_EditorRestrictionDisabled__";
                    patched++;
                }
            }

            return patched;
        }

        private static void PatchWhitespacePersonalityValidation(
            ModuleDefinition module,
            TypeDefinition editor)
        {
            TypeDefinition callbacks = editor.NestedTypes.Single(
                type => type.Name == "<>c__DisplayClass7_0");
            MethodDefinition saveCallback = callbacks.Methods.Single(
                method => method.Name == "<EditCustomContentDefinition>b__5");
            IList<Instruction> instructions = saveCallback.Body.Instructions;

            Instruction errorMessage = instructions.Single(
                instruction => instruction.OpCode == OpCodes.Ldstr &&
                               instruction.Operand is string value &&
                               value == "At least two personality traits must be defined.");
            int errorIndex = instructions.IndexOf(errorMessage);

            // Find the string != "" test immediately before the trait-count test.
            int inequalityIndex = -1;
            for (int i = errorIndex - 1; i >= 1; i--)
            {
                if (instructions[i].OpCode == OpCodes.Call &&
                    instructions[i].Operand is MethodReference called &&
                    called.DeclaringType.FullName == "System.String" &&
                    called.Name == "op_Inequality")
                {
                    inequalityIndex = i;
                    break;
                }
            }

            if (inequalityIndex < 1 ||
                instructions[inequalityIndex - 1].OpCode != OpCodes.Ldstr ||
                (string)instructions[inequalityIndex - 1].Operand != "")
            {
                throw new InvalidOperationException(
                    "Couldn't locate personality-trait blank-field validation.");
            }

            Instruction branch = instructions[inequalityIndex + 1];
            if (branch.OpCode != OpCodes.Brfalse &&
                branch.OpCode != OpCodes.Brfalse_S)
            {
                throw new InvalidOperationException(
                    "Unexpected personality-trait validation branch.");
            }

            // Base-NPC overrides should preserve their original traits when the
            // replacement field is null, empty, or contains only invisible whitespace.
            instructions[inequalityIndex - 1].OpCode = OpCodes.Nop;
            instructions[inequalityIndex - 1].Operand = null;
            instructions[inequalityIndex].Operand = module.ImportReference(
                typeof(string).GetMethod(
                    nameof(string.IsNullOrWhiteSpace),
                    new[] { typeof(string) }));
            branch.OpCode = branch.OpCode == OpCodes.Brfalse
                ? OpCodes.Brtrue
                : OpCodes.Brtrue_S;
        }
    }
}
