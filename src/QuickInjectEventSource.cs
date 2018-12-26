// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;
    using System.Diagnostics.Tracing;

    [EventSource(Guid = "{5bf45913-60eb-5dd5-78fa-0c46de21a2fb}")]
    internal sealed class QuickInjectEventSource : EventSource
    {
        [NonEvent]
        public void RegisterType(Type from, Type to, LifetimeManager lifetime)
        {
            this.RegisterType(from.ToString(), to.ToString(), lifetime?.GetType().ToString() ?? string.Empty);
        }

        public void RegisterType(string from, string to, string lifetimeType)
        {
            this.WriteEvent(1, from, to, lifetimeType);
        }

        public void Resolve(string type)
        {
            this.WriteEvent(2, type);
        }

        public void GetService(string type)
        {
            this.WriteEvent(3, type);
        }

        [Event(4, Task = Tasks.Compilation, Opcode = EventOpcode.Start)]
        public void CompilationStart(string type)
        {
            this.WriteEvent(4, type);
        }

        [Event(5, Task = Tasks.Compilation, Opcode = EventOpcode.Stop)]
        public void CompilationStop(string type)
        {
            this.WriteEvent(5, type);
        }

        [Event(6, Task = Tasks.CompilationDependencyAnalysis, Opcode = EventOpcode.Start)]
        public void CompilationDependencyAnalysisStart(string type)
        {
            this.WriteEvent(6, type);
        }

        [Event(7, Task = Tasks.CompilationDependencyAnalysis, Opcode = EventOpcode.Stop)]
        public void CompilationDependencyAnalysisStop(string type)
        {
            this.WriteEvent(7, type);
        }

        [Event(8, Task = Tasks.CompilationCodeGeneration, Opcode = EventOpcode.Start)]
        public void CompilationCodeGenerationStart(string type)
        {
            this.WriteEvent(8, type);
        }

        [Event(9, Task = Tasks.CompilationCodeGeneration, Opcode = EventOpcode.Stop)]
        public void CompilationCodeGenerationStop(string type)
        {
            this.WriteEvent(9, type);
        }

        [Event(10, Task = Tasks.CompilationCodeCompilation, Opcode = EventOpcode.Start)]
        public void CompilationCodeCompilationStart(string type)
        {
            this.WriteEvent(10, type);
        }

        [Event(11, Task = Tasks.CompilationCodeCompilation, Opcode = EventOpcode.Stop)]
        public void CompilationCodeCompilationStop(string type)
        {
            this.WriteEvent(11, type);
        }

        [Event(12, Task = Tasks.CompilationContention, Opcode = EventOpcode.Start)]
        public void CompilationContentionStart(string type)
        {
            this.WriteEvent(12, type);
        }

        [Event(13, Task = Tasks.CompilationContention, Opcode = EventOpcode.Stop)]
        public void CompilationContentionStop(string type)
        {
            this.WriteEvent(13, type);
        }

        public void ResolveCallWithoutResolutionContext(string type)
        {
            this.WriteEvent(14, type);
        }

        internal sealed class Tasks
        {
            public const EventTask Compilation = (EventTask)1;
            public const EventTask CompilationDependencyAnalysis = (EventTask)2;
            public const EventTask CompilationCodeGeneration = (EventTask)3;
            public const EventTask CompilationCodeCompilation = (EventTask)4;
            public const EventTask CompilationContention = (EventTask)5;
        }
    }
}