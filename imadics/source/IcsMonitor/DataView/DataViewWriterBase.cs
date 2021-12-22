﻿using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Traffix.DataView
{
    /// <summary>
    /// Abstract base class for custom data view writers.
    /// </summary>
    public abstract class DataViewWriterBase : IDataViewWriter
    {
        private bool _disposed;

        protected IndentedTextWriter Writer { get; }

        protected DataViewSchema Schema { get; }

        protected DataViewSchema.Column[] Columns { get; }

        protected DataViewWriterBase(TextWriter writer, DataViewSchema schema)
        {
            Writer = new IndentedTextWriter(writer, "  ");
            Schema = schema;
            Columns = schema.Where(col => !col.IsHidden).ToArray();

        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                CleanUp();
                Writer.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static object GetStringValueForColumn(DataViewSchema.Column column, DataViewRowCursor cursor)
        {
            switch (column.Type)
            {
                case BooleanDataViewType _: return GetValue<bool>(cursor, column);
                case NumberDataViewType number:
                    if (number.RawType == typeof(Byte)) return GetValue<byte>(cursor, column);
                    if (number.RawType == typeof(Double)) return GetValue<double>(cursor, column);
                    if (number.RawType == typeof(Int16)) return GetValue<short>(cursor, column);
                    if (number.RawType == typeof(Int32)) return GetValue<int>(cursor, column);
                    if (number.RawType == typeof(Int64)) return GetValue<long>(cursor, column);
                    if (number.RawType == typeof(SByte)) return GetValue<sbyte>(cursor, column);
                    if (number.RawType == typeof(Single)) return GetValue<float>(cursor, column);
                    if (number.RawType == typeof(UInt16)) return GetValue<ushort>(cursor, column);
                    if (number.RawType == typeof(UInt32)) return GetValue<uint>(cursor, column);
                    if (number.RawType == typeof(UInt64)) return GetValue<ulong>(cursor, column);
                    break;
                case TextDataViewType _:                return GetTextValue(cursor, column);
                case DateTimeDataViewType _:            return GetValue<DateTime>(cursor, column);
                case DateTimeOffsetDataViewType _:      return GetValue<DateTimeOffset>(cursor, column);
                case TimeSpanDataViewType _:            return GetValue<TimeSpan>(cursor, column);
                case VectorDataViewType t:              return GetVectorValue(cursor, column, t.ItemType);
            }
            throw new NotSupportedException($"The data view type {column.Type} is not supported");
        }

        protected static object GetVectorValue(DataViewRowCursor cursor, DataViewSchema.Column column, PrimitiveDataViewType itemType)
        {
            if (itemType.RawType == typeof(float))
            {
                var vector = GetValue<VBuffer<float>>(cursor, column).GetValues().ToArray();
                return vector;
            }
            if (itemType.RawType == typeof(double))
            {
                var vector = GetValue<VBuffer<double>>(cursor, column).GetValues().ToArray();
                return vector;
            }
            throw new NotSupportedException($"The data view type {itemType} is not supported");
        }
        protected static T GetValue<T>(DataViewRowCursor cursor, DataViewSchema.Column column)
        {
            T value = default;
            cursor.GetGetter<T>(column)(ref value);
            return value;
        }
        protected static string GetTextValue(DataViewRowCursor cursor, DataViewSchema.Column column)
        {
            var value = GetValue<ReadOnlyMemory<char>>(cursor, column);
            return value.ToString();
        }
        protected static IEnumerable<KeyValuePair<string, object>> GetValues(DataViewRowCursor cursor, DataViewSchema.Column[] columns)
        {
            foreach (var column in columns)
            {
                yield return KeyValuePair.Create(column.Name, GetStringValueForColumn(column, cursor));
            }
        }
        /// <summary>
        /// Implement to write the specific header of the document.
        /// </summary>
        protected abstract void WriteHeader();
        /// <summary>
        /// Implement to write the specific footer of the document.
        /// </summary>
        protected abstract void WriteFooter();
        /// <summary>
        /// Implement to write a sinlge row/record of the document.
        /// </summary>
        /// <param name="values"></param>
        protected abstract void WriteRow(IEnumerable<KeyValuePair<string, object>> values);

        /// <summary>
        /// Called before the writer is closed and disposed.
        /// </summary>
        protected virtual void CleanUp()
        {

        }

        /// <summary>
        /// Can be used to append the dataview to the current writer. 
        /// </summary>
        /// <param name="dataview">The dataview to append. It can be null if header or footer needs to be written.</param>
        /// <param name="writeHeader">true if header should be written before the dataview rows.</param>
        /// <param name="writeFooter">true if footer should be written after the dataview rows.</param>
        public int AppendDataView(IDataView dataview)
        {
            var rowCount = 0;
            if (dataview != null)
            {
                using var cursor = dataview.GetRowCursor(Columns);
                while (cursor.MoveNext())
                {
                    var values = GetValues(cursor, Columns);
                    WriteRow(values);
                    rowCount++;
                }
            }
            return rowCount;
        }

        /// <summary>
        /// Writes the beginning of the document.
        /// </summary>
        public void BeginDocument()
        {
            WriteHeader();
        }

        /// <summary>
        /// Writes the end of the document.
        /// </summary>
        public void EndDocument()
        {
            WriteFooter();
        }

        protected ExpandoObject GetExpandoScheme(IEnumerable<KeyValuePair<string, object>> values)
        {
            var obj = new ExpandoObject();
            foreach (var item in values)
            {
                ((IDictionary<String, Object>)obj).Add(item.Key, item.Value);
            }
            return obj;
        }
        protected ExpandoObject GetExpando(IEnumerable<KeyValuePair<string, object>> values)
        {
            var obj = new ExpandoObject();
            foreach (var item in values)
            {
                var value = JsonSerializer.Serialize(item.Value);
                ((IDictionary<String, Object>)obj).Add(item.Key, value);
            }
            return obj;
        }
    }
}