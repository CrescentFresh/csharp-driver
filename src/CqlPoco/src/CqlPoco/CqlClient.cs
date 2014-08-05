﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using CqlPoco.Mapping;
using CqlPoco.Statements;
using CqlPoco.TypeConversion;

namespace CqlPoco
{
    /// <summary>
    /// The default CQL client implementation which uses the DataStax driver ISession provided in the constructor
    /// for running queries against a Cassandra cluster.
    /// </summary>
    internal class CqlClient : ICqlClient
    {
        private readonly ISession _session;
        private readonly MapperFactory _mapperFactory;
        private readonly StatementFactory _statementFactory;
        private readonly CqlStringGenerator _cqlGenerator;

        public CqlClient(ISession session, MapperFactory mapperFactory, StatementFactory statementFactory, CqlStringGenerator cqlGenerator)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (mapperFactory == null) throw new ArgumentNullException("mapperFactory");
            if (statementFactory == null) throw new ArgumentNullException("statementFactory");
            if (cqlGenerator == null) throw new ArgumentNullException("cqlGenerator");

            _session = session;
            _mapperFactory = mapperFactory;
            _statementFactory = statementFactory;
            _cqlGenerator = cqlGenerator;
        }

        public Task<List<T>> FetchAsync<T>()
        {
            // Just pass an empty string for the CQL and let if be auto-generated
            return FetchAsync<T>(string.Empty);
        }

        public async Task<List<T>> FetchAsync<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            RowSet rows = await _session.ExecuteAsync(statement).ConfigureAwait(false);
            
            // Map to return type
            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return rows.Select(mapper).ToList();
        }
        
        public async Task<T> SingleAsync<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            RowSet rows = await _session.ExecuteAsync(statement).ConfigureAwait(false);

            Row row = rows.Single();

            // Map to return type
            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }
        
        public async Task<T> SingleOrDefaultAsync<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            RowSet rows = await _session.ExecuteAsync(statement).ConfigureAwait(false);

            Row row = rows.SingleOrDefault();

            // Map to return type or return default
            if (row == null) 
                return default(T);

            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }

        public async Task<T> FirstAsync<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            RowSet rows = await _session.ExecuteAsync(statement).ConfigureAwait(false);

            Row row = rows.First();

            // Map to return type
            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }

        public async Task<T> FirstOrDefaultAsync<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            RowSet rows = await _session.ExecuteAsync(statement).ConfigureAwait(false);

            Row row = rows.FirstOrDefault();

            // Map to return type or return default
            if (row == null)
                return default(T);

            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }
        
        public async Task InsertAsync<T>(T poco)
        {
            // Get statement and bind values from POCO
            string cql = _cqlGenerator.GenerateInsert<T>();
            Func<T, object[]> getBindValues = _mapperFactory.GetValueCollector<T>(cql);
            object[] values = getBindValues(poco);

            // Execute the statement
            Statement statement = await _statementFactory.GetStatementAsync(cql, values).ConfigureAwait(false);
            await _session.ExecuteAsync(statement).ConfigureAwait(false);
        }
        
        public async Task UpdateAsync<T>(T poco)
        {
            // Get statement and bind values from POCO
            string cql = _cqlGenerator.GenerateUpdate<T>();
            Func<T, object[]> getBindValues = _mapperFactory.GetValueCollector<T>(cql);
            object[] values = getBindValues(poco);

            // Execute
            Statement statement = await _statementFactory.GetStatementAsync(cql, values).ConfigureAwait(false);
            await _session.ExecuteAsync(statement).ConfigureAwait(false);
        }

        public async Task UpdateAsync<T>(string cql, params object[] args)
        {
            cql = _cqlGenerator.PrependUpdate<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            await _session.ExecuteAsync(statement).ConfigureAwait(false);
        }

        public async Task DeleteAsync<T>(T poco)
        {
            // Get the statement and bind values from POCO
            string cql = _cqlGenerator.GenerateDelete<T>();
            Func<T, object[]> getBindValues = _mapperFactory.GetValueCollector<T>(cql, primaryKeyValuesOnly: true);
            object[] values = getBindValues(poco);

            // Execute
            Statement statement = await _statementFactory.GetStatementAsync(cql, values).ConfigureAwait(false);
            await _session.ExecuteAsync(statement).ConfigureAwait(false);
        }

        public async Task DeleteAsync<T>(string cql, params object[] args)
        {
            cql = _cqlGenerator.PrependDelete<T>(cql);
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            await _session.ExecuteAsync(statement).ConfigureAwait(false);
        }

        public async Task ExecuteAsync(string cql, params object[] args)
        {
            Statement statement = await _statementFactory.GetStatementAsync(cql, args).ConfigureAwait(false);
            await _session.ExecuteAsync(statement).ConfigureAwait(false);
        }

        public ICqlBatch CreateBatch()
        {
            return new CqlBatch(_mapperFactory, _cqlGenerator);
        }

        public void Execute(ICqlBatch batch)
        {
            if (batch == null) throw new ArgumentNullException("batch");

            BatchStatement batchStatement = _statementFactory.GetBatchStatement(batch.Statements);
            _session.Execute(batchStatement);
        }

        public async Task ExecuteAsync(ICqlBatch batch)
        {
            if (batch == null) throw new ArgumentNullException("batch");

            BatchStatement batchStatement = await _statementFactory.GetBatchStatementAsync(batch.Statements);
            await _session.ExecuteAsync(batchStatement);
        }

        public TDatabase ConvertCqlArgument<TValue, TDatabase>(TValue value)
        {
            return _mapperFactory.TypeConverter.ConvertCqlArgument<TValue, TDatabase>(value);
        }

        public List<T> Fetch<T>()
        {
            // Just let the SQL be auto-generated
            return Fetch<T>(string.Empty);
        }

        public List<T> Fetch<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            RowSet rows = _session.Execute(statement);

            // Map to return type
            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return rows.Select(mapper).ToList();
        }

        public T Single<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            RowSet rows = _session.Execute(statement);

            Row row = rows.Single();

            // Map to return type
            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }

        public T SingleOrDefault<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            RowSet rows = _session.Execute(statement);

            Row row = rows.SingleOrDefault();

            // Map to return type or return default
            if (row == null)
                return default(T);

            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }

        public T First<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            RowSet rows = _session.Execute(statement);

            Row row = rows.First();

            // Map to return type
            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }

        public T FirstOrDefault<T>(string cql, params object[] args)
        {
            // Get the statement to execute and execute it
            cql = _cqlGenerator.AddSelect<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            RowSet rows = _session.Execute(statement);

            Row row = rows.FirstOrDefault();

            // Map to return type or return default
            if (row == null)
                return default(T);

            Func<Row, T> mapper = _mapperFactory.GetMapper<T>(cql, rows);
            return mapper(row);
        }

        public void Insert<T>(T poco)
        {
            // Get statement and bind values from POCO
            string cql = _cqlGenerator.GenerateInsert<T>();
            Func<T, object[]> getBindValues = _mapperFactory.GetValueCollector<T>(cql);
            object[] values = getBindValues(poco);

            // Execute the statement
            Statement statement = _statementFactory.GetStatement(cql, values);
            _session.Execute(statement);
        }

        public void Update<T>(T poco)
        {
            // Get statement and bind values from POCO
            string cql = _cqlGenerator.GenerateUpdate<T>();
            Func<T, object[]> getBindValues = _mapperFactory.GetValueCollector<T>(cql);
            object[] values = getBindValues(poco);

            // Execute
            Statement statement = _statementFactory.GetStatement(cql, values);
            _session.Execute(statement);
        }

        public void Update<T>(string cql, params object[] args)
        {
            cql = _cqlGenerator.PrependUpdate<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            _session.Execute(statement);
        }

        public void Delete<T>(T poco)
        {
            // Get the statement and bind values from POCO
            string cql = _cqlGenerator.GenerateDelete<T>();
            Func<T, object[]> getBindValues = _mapperFactory.GetValueCollector<T>(cql, primaryKeyValuesOnly: true);
            object[] values = getBindValues(poco);

            // Execute
            Statement statement = _statementFactory.GetStatement(cql, values);
            _session.Execute(statement);
        }

        public void Delete<T>(string cql, params object[] args)
        {
            cql = _cqlGenerator.PrependDelete<T>(cql);
            Statement statement = _statementFactory.GetStatement(cql, args);
            _session.Execute(statement);
        }

        public void Execute(string cql, params object[] args)
        {
            Statement statement = _statementFactory.GetStatement(cql, args);
            _session.Execute(statement);
        }
    }
}