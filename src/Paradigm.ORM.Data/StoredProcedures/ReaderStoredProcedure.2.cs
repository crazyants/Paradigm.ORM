using System;
using System.Collections.Generic;
using Paradigm.ORM.Data.Database;
using Paradigm.ORM.Data.Descriptors;
using Paradigm.ORM.Data.Exceptions;
using Paradigm.ORM.Data.Mappers.Generic;
using Paradigm.ORM.Data.Extensions;

namespace Paradigm.ORM.Data.StoredProcedures
{
    /// <summary>
    /// Provides the means to execute data reader stored procedures returning only 2 result sets.
    /// </summary>
    /// <remarks>
    /// Instead of sending individual parameters to the procedure, the orm expects a  <see cref="TParameters"/> type
    /// containing or referencing the mapping information, where individual parameters will be mapped to properties.
    /// </remarks>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    /// <typeparam name="TResult1">The type of the first result.</typeparam>
    /// <typeparam name="TResult2">The type of the second result.</typeparam>
    /// <seealso cref="Paradigm.ORM.Data.StoredProcedures.StoredProcedureBase{TParameters}" />
    /// <seealso cref="IReaderStoredProcedure{TParameters,TResult1,TResult2}" />
    public partial class ReaderStoredProcedure<TParameters, TResult1, TResult2> : 
        StoredProcedureBase<TParameters>,
        IReaderStoredProcedure<TParameters, TResult1, TResult2>
        where TResult1 : new()
        where TResult2 : new()
    {
        #region Properties

        /// <summary>
        /// Gets or sets the first result mapper.
        /// </summary>
        private IDatabaseReaderMapper<TResult1> Mapper1 { get; set; }

        /// <summary>
        /// Gets or sets the secund result mapper.
        /// </summary>
        private IDatabaseReaderMapper<TResult2> Mapper2 { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ReaderStoredProcedure.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ReaderStoredProcedure(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReaderStoredProcedure.
        /// </summary>
        /// <param name="connector">The database connector.</param>
        public ReaderStoredProcedure(IDatabaseConnector connector) : base(connector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReaderStoredProcedure.
        /// </summary>
        /// <param name="connector">The database connector.</param>
        /// <param name="mapper1">The first result mapper.</param>
        /// <param name="mapper2">The second result mapper.</param>
        public ReaderStoredProcedure(
            IDatabaseConnector connector, 
            IDatabaseReaderMapper<TResult1> mapper1, 
            IDatabaseReaderMapper<TResult2> mapper2) : base(connector)
        {
            this.Mapper1 = mapper1;
            this.Mapper2 = mapper2;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the stored procedure and return a list of tuples.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// List of tuples.
        /// </returns>
        public Tuple<List<TResult1>, List<TResult2>> Execute(TParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("Must give parameters to execute the stored procedure.");

            this.SetParametersValue(parameters);

            using (var reader = this.Command.ExecuteReader())
            {
                var result1 = this.Mapper1.Map(reader);
                reader.NextResult();
                var result2 = this.Mapper2.Map(reader);

                return new Tuple<List<TResult1>, List<TResult2>>(result1, result2);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Executes after the initialization.
        /// </summary>
        protected override void AfterInitialize()
        {
            base.AfterInitialize();

            this.Mapper1 = this.Mapper1 ?? this.ServiceProvider.GetServiceIfAvailable<IDatabaseReaderMapper<TResult1>>(() => new DatabaseReaderMapper<TResult1>(this.Connector, new TableTypeDescriptor(typeof(TResult1))));
            this.Mapper2 = this.Mapper2 ?? this.ServiceProvider.GetServiceIfAvailable<IDatabaseReaderMapper<TResult2>>(() => new DatabaseReaderMapper<TResult2>(this.Connector, new TableTypeDescriptor(typeof(TResult2))));

            if (this.Mapper1 == null)
                throw new OrmException("The first mapper can not be null.");

            if (this.Mapper2 == null)
                throw new OrmException("The second mapper can not be null.");
        }

        #endregion
    }
}