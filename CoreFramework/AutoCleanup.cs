using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework
{
    public class AutoCleanup : IDisposable
    {
        private bool disposed;
        private readonly Action executeOnDispose;

        /// <summary>
        /// Constructs an <see cref=”AutoCleanup”/> object,
        /// immediately executing a delegate function, and
        /// then guaranteeing the execution of a second
        /// deletate function when disposed.
        /// </summary>
        /// <param name=”executeOnConstruct”>
        /// The delegate function to execute during
        /// construction.
        /// </param>
        /// <param name=”executeOnDispose”>
        /// The delegate function to execute when disposed.
        /// </param>
        public AutoCleanup(Action executeOnConstruct,
            Action executeOnDispose)
        {
            if (null != executeOnConstruct)
            {
                executeOnConstruct();
            }
            this.executeOnDispose = executeOnDispose;
        }

        /// <summary>
        /// Constructs an <see cref=”AutoCleanup”/> object,
        /// guaranteeing the execution of a provided delegate
        /// function when disposed.
        /// </summary>
        /// <param name=”executeOnDispose”>
        /// The delegate function to execute when disposed.
        /// </param>
        public AutoCleanup(Action executeOnDispose)
        {
            this.executeOnDispose = executeOnDispose;
        }

        #region IDisposable Members
        /// <summary>
        /// Disposes the <see cref=”AutoCleanup”/> object,
        /// executing the delegate function provided in the
        /// constructor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //
        // Internal implementation of the Dispose() method.
        // See the MSDN documentation on the IDisposable
        // interface for a detailed explanation of this
        // pattern.
        //
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                //
                // When disposing is true, release all
                // managed resources.
                //
                if (disposing)
                {
                    if (null != this.executeOnDispose)
                    {
                        this.executeOnDispose();
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }

}
