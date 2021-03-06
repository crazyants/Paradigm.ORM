﻿using System;
using FluentAssertions;
using Paradigm.ORM.Data.DatabaseAccess;
using Paradigm.ORM.Data.Extensions;
using Paradigm.ORM.Data.Querying;
using Paradigm.ORM.Tests.Fixtures;
using Paradigm.ORM.Tests.Fixtures.PostgreSql;
using Paradigm.ORM.Tests.Mocks.PostgreSql;
using Npgsql;
using NUnit.Framework;

namespace Paradigm.ORM.Tests.Tests.Queries.PostgreSql
{
    [TestFixture]
    public class PostgreSqlQueryTests
    {
        private QueryFixtureBase Fixture { get; }
        
        public PostgreSqlQueryTests()
        {
            Fixture = Activator.CreateInstance(typeof(PostgreSqlQueryFixture)) as QueryFixtureBase;
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Fixture.CreateDatabase();
            Fixture.CreateParentTable();
            Fixture.CreateChildTable();

            using (var databaseAccess = new DatabaseAccess(Fixture.Connector, typeof(SingleKeyParentTable)))
            {
                databaseAccess.Insert(Fixture.CreateNewEntity());
                databaseAccess.Insert(Fixture.CreateNewEntity2());
            }
        }

        [Test]
        public void ShouldRetrieveTwoEntities()
        {
            var result = Fixture.Connector.Query<SingleKeyParentTable>();
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            var entity1 = result.Find(x => x.Name.StartsWith("Test Parent 1"));
            entity1.IsActive.Should().Be(true);
            entity1.Childs.Should().BeNull();
            entity1.CreatedDate.Should().Be(new DateTime(2017, 4, 12));
            entity1.Amount.Should().Be(30.34m);

            var entity2 = result.Find(x => x.Name.StartsWith("Test Parent 2"));
            entity2.IsActive.Should().Be(false);
            entity2.Childs.Should().BeNull();
            entity2.CreatedDate.Should().Be(new DateTime(2017, 6, 21));
            entity2.Amount.Should().Be(215.5m);
        }

        [Test]
        public void ShouldThrowPostgreSqlException()
        {
            Action result = () => Fixture.Connector.Query<AllColumnsClass>();
            result.ShouldThrow<PostgresException>().WithMessage("42P01: relation \"AllColumns\" does not exist");
        }

        [Test]
        public void QueryWithWhere()
        {
            var result = Fixture.Connector.Query<SingleKeyParentTable>(Fixture.WhereClause);
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            result[0].Name.Should().StartWith("Test Parent 1");
            result[0].IsActive.Should().Be(true);
            result[0].Childs.Should().BeNull();
            result[0].CreatedDate.Should().Be(new DateTime(2017, 4, 12));
            result[0].Amount.Should().Be(30.34m);
        }

        [Test]
        public void QueryWithNotMatchingWhere()
        {
            var result = Fixture.Connector.Query<SingleKeyParentTable>("\"Name\" like 'Non Existant Entity'");
            result.Should().NotBeNull();
            result.Should().HaveCount(0);
        }

        [Test]
        public void QueryObjectMustBeReutilizable()
        {
            var query = new Query<SingleKeyParentTable>(Fixture.Connector);

            var result = query.Execute();
            var result2 = query.Execute();

            result.Should().NotBeNull();
            result2.Should().NotBeNull();
            result2.Should().HaveSameCount(result);

            query.Dispose();
        }

        [Test]
        public void WhereClauseShouldNotRemainInObject()
        {
            var query = new Query<SingleKeyParentTable>(Fixture.Connector);

            var result = query.Execute();
            var result2 = query.Execute(Fixture.WhereClause);

            result.Should().NotBeNull();
            result2.Should().NotBeNull();
            result2.Count.Should().NotBe(result.Count);

            query.Dispose();
        }

        [Test]
        public void WhereClauseShouldNotRemainInObject2()
        {
            var query = new Query<SingleKeyParentTable>(Fixture.Connector);

            var result = query.Execute(Fixture.WhereClause);
            var result2 = query.Execute();

            result.Should().NotBeNull();
            result2.Should().NotBeNull();
            result2.Count.Should().NotBe(result.Count);

            query.Dispose();
        }

        [Test]
        public void DisposingTwoTimesShouldBeOk()
        {
            var query = new Query<SingleKeyParentTable>(Fixture.Connector);

            var result = query.Execute();
            
            query.Dispose();
            query.Dispose();
        }

        [Test]
        public void ShouldNotUseDisposedQueryObject()
        {
            var query = new Query<SingleKeyParentTable>(Fixture.Connector);

            var result = query.Execute();
            query.Dispose();

            Action executeQuery = () => query.Execute();
            executeQuery.ShouldThrow<NullReferenceException>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Fixture.DropDatabase();
            Fixture.Dispose();
        }
    }
}
