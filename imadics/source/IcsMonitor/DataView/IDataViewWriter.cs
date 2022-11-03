using Microsoft.ML;
using System;

namespace Traffix.DataView
{
    /// <summary>
    /// A common interface for implementations of data view writers. 
    /// </summary>
    public interface IDataViewWriter : IDisposable
    {

        /// <summary>
        /// Writes the start of the document to the underlying text writer.
        /// </summary>
        public void BeginDocument();

        /// <summary>
        /// Writes the end of the document to the underlying text writer.
        /// </summary>
        public void EndDocument();

        /// <summary>
        /// Writes the content of the data view using the underlying writer.
        /// </summary>
        /// <param name="dataview"></param>
        public int AppendDataView(IDataView dataview);
    }
}