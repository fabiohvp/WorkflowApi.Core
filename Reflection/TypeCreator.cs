using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace WorkflowApi.Core.Reflection
{
    public class TypeCreator : IDisposable
    {
#if (DEBUG)
        private static int Count = 0;
#endif

        private ConcurrentDictionary<string, Type> BuiltTypes;

        public virtual ModuleBuilder ModuleBuilder { get; private set; }

        public TypeCreator(string assemblyName)
        {
            BuiltTypes = new ConcurrentDictionary<string, Type>();

            var _assemblyName = new AssemblyName() { Name = assemblyName };

            ModuleBuilder = AssemblyBuilder
                .DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(_assemblyName.Name);
            //moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
        }

        public TypeCreator(ModuleBuilder moduleBuilder)
        {
            BuiltTypes = new ConcurrentDictionary<string, Type>();
            ModuleBuilder = moduleBuilder;
        }

        public virtual string GetClassName(Dictionary<string, Type> fields, string nestedPrefix = "_")
        {
#if (DEBUG)
            return "Key_" + Count++;
#endif

            //TODO: optimize the type caching -- if fields are simply reordered, that doesn't mean that they're actually different types, so this needs to be smarter
            var key = string.Empty;
            var ordered = fields.OrderBy(o => o.Key);

            foreach (var field in ordered)
            {
                key += nestedPrefix + "@" + field.Key + "@" + field.Value.Name + "@";
            }

            return key;
        }

        public void Dispose()
        {
            BuiltTypes.Clear();
            ModuleBuilder = null;
        }
    }
}