namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public interface ICodeProvider
    {
        void GenerateCode(ILGenerator ilGenerator, Dictionary<Type, int> localsMap);
    }
}