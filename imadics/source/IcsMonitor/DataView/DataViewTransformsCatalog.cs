using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System.Linq;

namespace Traffix.DataView
{
    /// <summary>
    /// The extension class implementing project's specific transformers.  
    /// </summary>
    public static class TraffixTransformsCatalog
    {
        /// <summary>
        /// Provides a transformer that creates a feature vector from the given columns. It converts all values in the source columns 
        /// to floating-point numbers before creating the feature vector column. 
        /// </summary>
        /// <param name="featureColumnName">The name of the resulting feature vector column.</param>
        /// <param name="sourceColumns">The array of source columns.</param>
        /// <returns>The transformer that can be a part of the ML pipeline.</returns>
        public static EstimatorChain<ColumnConcatenatingTransformer> CreateFeatureVector(this TransformsCatalog transforms, DataViewSchema schema, string featureColumnName, params string[] sourceColumns)
        {
            var dataviewType = schema.First().Type;

            var convertors = sourceColumns.Select(columnName => transforms.Conversion.ConvertType(columnName, outputKind: DataKind.Single));
            var pipeline = convertors.Aggregate(new EstimatorChain<TypeConvertingTransformer>(), (x, y) => x.Append(y)).Append(transforms.Concatenate(featureColumnName, sourceColumns));
            return pipeline;
        }
    }
}