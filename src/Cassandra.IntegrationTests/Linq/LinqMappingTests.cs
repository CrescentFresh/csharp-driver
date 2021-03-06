﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra.Data.Linq;
using NUnit.Framework;

namespace Cassandra.IntegrationTests.Linq
{
    [Category("short")]
    public class LinqMappingTests : SingleNodeClusterTest
    {
        private const string TableName = "linqalltypestable";

        [Test]
        public void CreateTableTest()
        {
            var table = Session.GetTable<AllTypesEntity>(TableName);
            table.CreateIfNotExists();
            Assert.DoesNotThrow(() => 
                Session.Execute("SELECT * FROM " + TableName));
        }

        [Test]
        public void MappingAsyncTest()
        {
            var table = Session.GetTable<AllTypesEntity>(TableName);
            const int length = 100;
            var tasks = new List<Task>(length);
            for (var i = 0; i < length; i++)
            {
                var query = table.Insert(new AllTypesEntity
                {
                    Id = Guid.NewGuid(),
                    BooleanValue = i%2 == 1,
                    DateTimeValue = DateTime.Now,
                    DateTimeOffsetValue = DateTimeOffset.Now.AddDays(-299),
                    DecimalValue = 101.110M*i,
                    DoubleValue = -344.512*i,
                    FloatValue = 23.1F*i,
                    NullableIntValue = null,
                    Int64Value = 100 * i,
                    IntValue = -90 * i,
                    StringValue = i.ToString(CultureInfo.InvariantCulture),
                    TimeUuidValue = TimeUuid.NewId(),
                    MapSample = new Dictionary<string, float> { { "i", i / 0.1f } },
                    ListSample = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
                });
                query.SetRetryPolicy(DowngradingConsistencyRetryPolicy.Instance);
                tasks.Add(query.ExecuteAsync());
            }
            Task.WaitAll(tasks.ToArray());
            var entities = (from e in table select e).Execute().ToArray();
            Assert.AreEqual(length, entities.Length);
        }

        public class AllTypesEntity
        {
            public bool BooleanValue { get; set; }
            public DateTime DateTimeValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
            public decimal DecimalValue { get; set; }
            public double DoubleValue { get; set; }
            public float FloatValue { get; set; }
            public int? NullableIntValue { get; set; }
            public Int64 Int64Value { get; set; }
            public int IntValue { get; set; }
            public string StringValue { get; set; }
            [PartitionKey]
            public Guid Id { get; set; }
            public TimeUuid TimeUuidValue { get; set; }
            public TimeUuid? NullableTimeUuidValue { get; set; }
            public Dictionary<string, float> MapSample { get; set; }
            public List<Guid> ListSample { get; set; }
            public List<Guid> ListSample2 { get; set; }
        }
    }
}
