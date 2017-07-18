namespace UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using QuickInject;

    internal sealed class GetA2CodeProvider : ICodeProvider
    {
        public void GenerateCode(ILGenerator ilGenerator, Dictionary<Type, int> localsMap)
        {
            ilGenerator.Emit(OpCodes.Ldloc, localsMap[typeof(C)]);
            ilGenerator.Emit(OpCodes.Call, typeof(C).GetMethod("GetA2"));
        }
    }
}