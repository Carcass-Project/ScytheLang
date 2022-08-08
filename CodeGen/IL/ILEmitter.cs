using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scythe.Nodes.Bound;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

namespace Scythe.CodeGen.IL
{
    /// <summary>
    /// Emitter for CIL, because I hate my life even more.
    /// </summary>
    public class ILEmitter
    {
        public ILProcessor ilProcessor;
        public MethodDefinition mainMethod;
        public ModuleDefinition module;
        public List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
        public Dictionary<string, object> variables = new Dictionary<string, object>();

        public object Visit(BoundExpression expr) => expr switch
        {
            BoundIntLiteralExpr => this.Visit(expr as BoundIntLiteralExpr),
            BoundStringLiteralExpr => this.Visit(expr as BoundStringLiteralExpr),
            BoundFloatLiteralExpr => this.Visit(expr as BoundFloatLiteralExpr),
            _ => throw new Exception("Unknown Bound Expression.")
        };

        public BoundExpression Visit(BoundIntLiteralExpr ex)
        {
            ilProcessor.Emit(OpCodes.Ldc_I4, ex.Literal);
            return ex;
        }

        public BoundExpression Visit(BoundFloatLiteralExpr ex)
        {
            ilProcessor.Emit(OpCodes.Ldc_R4, ex.Literal);
            return ex;
        }

        public BoundExpression Visit(BoundStringLiteralExpr ex)
        {
            ilProcessor.Emit(OpCodes.Ldstr, ex.Literal);
            return ex;
        }

        public object Visit(BoundStatement stmt) => stmt switch{
            
            _ => throw new Exception("Unknown Bound Statement.")
        };

        TypeReference ResolveType(AssemblyDefinition assembly, string metadataName)
        {
            var foundTypes = assemblies.SelectMany(a => a.Modules)
                                       .SelectMany(m => m.Types)
                                       .Where(t => t.FullName == metadataName)
                                       .ToArray();
            if (foundTypes.Length == 1)
            {
                return assembly.MainModule.ImportReference(foundTypes[0]);
            }
            else
            {
                // something went wrong
                return null;
            }
        }

        public AssemblyDefinition Emit(string pkgName, List<Nodes.Bound.BoundStatement> nodes)
        {
            var assembly = AssemblyDefinition.CreateAssembly(
                    new AssemblyNameDefinition(pkgName, new Version(6, 0, 0, 0)), pkgName, ModuleKind.Console);

            module = assembly.MainModule;

            assemblies.Add(AssemblyDefinition.ReadAssembly("output\\NETLibraries\\System.Runtime.dll"));
            assemblies.Add(AssemblyDefinition.ReadAssembly("output\\NETLibraries\\System.Runtime.Extensions.dll"));
            assemblies.Add(AssemblyDefinition.ReadAssembly("output\\NETLibraries\\System.Private.CoreLib.dll"));

            var ObjectTypeReference = ResolveType(assembly, "System.Object");
            var VoidTypeReference = ResolveType(assembly, "System.Void");

            // module.RuntimeVersion = "v6.0.0.0";
            var programType = new TypeDefinition(
            "Project",
            "Program",
            TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed,
            ObjectTypeReference);

            mainMethod = new MethodDefinition(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static,
            VoidTypeReference);

            programType.Methods.Add(mainMethod);


            module.Types.Add(programType);
            ilProcessor = mainMethod.Body.GetILProcessor();



            ilProcessor.Emit(OpCodes.Nop);
            ilProcessor.Emit(OpCodes.Ret);

            assembly.EntryPoint = mainMethod;

            return assembly;
        }
    }
}
