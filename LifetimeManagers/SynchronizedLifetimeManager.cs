// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System.Threading;

    public abstract class SynchronizedLifetimeManager : ContextInvariantLifetimeManager
    {
        private readonly object lockObj = new object();

        public override object GetValue()
        {
            Monitor.Enter(this.lockObj);
            object result = this.SynchronizedGetValue();
            if (result != null)
            {
                Monitor.Exit(this.lockObj);
            }

            return result;
        }

        public void Recover() => this.TryExit();

        public override void SetValue(object newValue)
        {
            this.SynchronizedSetValue(newValue);
            this.TryExit();
        }

        public override void RemoveValue()
        {
        }

        protected abstract object SynchronizedGetValue();

        protected abstract void SynchronizedSetValue(object newValue);

        private void TryExit()
        {
            // Prevent first chance exception when abandoning a lock that has not been entered
            if (Monitor.IsEntered(this.lockObj))
            {
                try
                {
                    Monitor.Exit(this.lockObj);
                }
                catch (SynchronizationLockException)
                {
                    // Noop here - we don't hold the lock and that's ok.
                }
            }
        }
    }
}