//
//      Copyright (C) 2012-2014 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cassandra.Data.Linq
{
    public class CqlQuerySingleElement<TEntity> : CqlQueryBase<TEntity>
    {
        internal CqlQuerySingleElement(Expression expression, IQueryProvider table)
            : base(expression, table)
        {
        }


        protected override string GetCql(out object[] values)
        {
            var visitor = new CqlExpressionVisitor();
            visitor.Evaluate(Expression);
            return visitor.GetSelect(out values);
        }

        public override string ToString()
        {
            var visitor = new CqlExpressionVisitor();
            visitor.Evaluate(Expression);
            object[] _;
            return visitor.GetSelect(out _, false);
        }

        public new CqlQuerySingleElement<TEntity> SetConsistencyLevel(ConsistencyLevel? consistencyLevel)
        {
            base.SetConsistencyLevel(consistencyLevel);
            return this;
        }

        public new CqlQuerySingleElement<TEntity> SetSerialConsistencyLevel(ConsistencyLevel consistencyLevel)
        {
            base.SetSerialConsistencyLevel(consistencyLevel);
            return this;
        }

        public new Task<TEntity> ExecuteAsync()
        {
            return base.ExecuteAsync().ContinueWith(t => 
            {
                return t.Result.FirstOrDefault();
            });
        }

        public new IAsyncResult BeginExecute(AsyncCallback callback, object state)
        {
            return ExecuteAsync().ToApm(callback, state);
        }

        public new TEntity EndExecute(IAsyncResult ar)
        {
            var task = (Task<TEntity>)ar;
            return task.Result;
        }

        /// <summary>
        /// Evaluates the Linq query, executes the cql statement and returns the first result.
        /// </summary>
        public new TEntity Execute()
        {
            var config = GetTable().GetSession().GetConfiguration();
            var task = ExecuteAsync();
            return TaskHelper.WaitToComplete(task, config.ClientOptions.QueryAbortTimeout);
        }
    }
}