﻿using System.Linq;
using NUnit.Framework;
using OrigoDB.Core.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace OrigoDB.Core.Test
{
    /// <summary>
    /// This is what is generated, if it doesn't compile here it won't compile dynamically :)
    /// </summary>
    class CompiledQuery
    {

        public static object QueryExpr(TestModel db, System.String @arg0)
        {
            return (from customer in db.Customers where customer.Name.StartsWith(@arg0) orderby customer.Name select customer.Name).First();
        }

        public static object Execute(Engine<TestModel> engine, params object[] args)
        {
            return engine.Execute<TestModel, object>(model => QueryExpr(model, (System.String)args[0]));
        }
    }


    [TestFixture]
    public class CachingLinqCompilerTest
    {

        private EngineConfiguration Config()
        {
            return new EngineConfiguration().ForIsolatedTest();
        }

        [Test]
        public void CanCompileAllQueries()
        {
            int failedQueries = 0;
            for (int i = 0; i < allQueries.Length; i++)
            {
                try
                {
                    var target = new CachingLinqCompiler(typeof(TestModel));
                    var args = new object[] { "a string", 42 };
                    target.GetCompiledQuery(allQueries[i], args);

                }
                catch (Exception ex)
                {
                    failedQueries++;
                    Trace.WriteLine(string.Format("Query id {0} failed", i));
                    Trace.WriteLine(ex);
                }
            }
            Assert.AreEqual(0, failedQueries);
        }

        [Test]
        public void RepeatedQueryIsCached()
        {
			var target = new CachingLinqCompiler(typeof(TestModel));
            var query = FirstCustomersNameStartingWithArg0;
            var args = new object[] { "H" };
            Assert.AreEqual(0, target.CompilerInvocations);
            target.GetCompiledQuery(query, args);
            Assert.AreEqual(1, target.CompilerInvocations);
            target.GetCompiledQuery(query, args);
            Assert.AreEqual(1, target.CompilerInvocations);
        }

        [Test]
        public void RepeatedQueryIsCachedWhenParametersDiffer()
        {
            var target = new CachingLinqCompiler(typeof(TestModel));
            var query = FirstCustomersNameStartingWithArg0;
            Assert.AreEqual(0, target.CompilerInvocations);
            target.GetCompiledQuery(query, new object[] { "H" });
            Assert.AreEqual(1, target.CompilerInvocations);
            target.GetCompiledQuery(query, new object[] { "R" });
            Assert.AreEqual(1, target.CompilerInvocations);
        }

        [Test]
        public void RepeatedQueryIsCompiledWhenCompilationIsForced()
        {
			var target = new CachingLinqCompiler(typeof(TestModel));
            target.ForceCompilation = true;
            var query = allQueries[0];
            var args = new object[] { "a" };
            Assert.AreEqual(0, target.CompilerInvocations);
            target.GetCompiledQuery(query, args);
            Assert.AreEqual(1, target.CompilerInvocations);
            target.GetCompiledQuery(query, args);
            Assert.AreEqual(2, target.CompilerInvocations);
        }

        [Test]
        public void CanExecuteListQuery()
        {
            var model = new TestModel();
            model.AddCustomer("Zippy");
            model.AddCustomer("Droozy");
            var engine = Engine.Create(model, Config());
            var list = engine.Execute<TestModel, List<string>>(ListOfCustomerNames);
            Assert.AreEqual(list.Count, 2);
            Assert.AreEqual(list[0], "Zippy");
            Assert.AreEqual(list[1], "Droozy");
            engine.Close();
        }

        [Test]
        public void CanCompileInjectedEvilCode()
        {
            var evilCode =
                @"new DoEvil();
        }

        private class DoEvil
        {
            public DoEvil()
            {
                new System.IO.DirectoryInfo(""c colon backslash"").Delete(true);
            }
        //";

			var compiler = new CachingLinqCompiler(typeof(TestModel));
            compiler.GetCompiledQuery(evilCode, new object[0]);
        }

        [Test]
        public void CanExecuteQueryWithNewDifferentParameters()
        {
            string expected0 = "Homer Simpson";
            string expected1 = "Robert Friberg";

            var query = allQueries[0];

            var model = new TestModel();
            model.AddCustomer(expected0);
            model.AddCustomer(expected1);
            Engine<TestModel> engine = Engine.Create(model, Config());

            string actual = engine.Execute<TestModel, string>(query, "Ho");
            Assert.AreEqual(expected0, actual);
            actual = (string)engine.Execute(query, "Ro");
            Assert.AreEqual(actual, expected1);
            engine.Close();
        }

        [Test]
        [ExpectedException(typeof(NullReferenceException))]
        public void CompilationFailsWhenPassedNullArgument()
        {
            var args = new object[] { null };
			var compiler = new CachingLinqCompiler(typeof(TestModel));
            compiler.GetCompiledQuery(FirstCustomersNameStartingWithArg0, args);
        }

        [Test]
        [ExpectedException(typeof(TargetInvocationException))]
        public void ExecutionFailsForMismatchedArgumentTypeOnSecondInvocation()
        {
            Engine<TestModel> engine = null;
            try
            {
                var args = new object[] { "H" };
                var model = new TestModel();
                model.AddCustomer("Homer Simpson");
                engine = Engine.Create(model, Config());
                engine.Execute(FirstCustomersNameStartingWithArg0, args);

                args[0] = 42; //boxed int

                //cached query generated code casts args[0] to string. should throw
                engine.Execute(FirstCustomersNameStartingWithArg0, args);
            }
            finally
            {
                if (engine != null) engine.Close();
            }


        }

        [Test]
        public void CanExecuteFirstCustomerStartingWithArg0()
        {
            var expected = "Homer Simpson";
            var query = FirstCustomersNameStartingWithArg0;
            var model = new TestModel();
            model.AddCustomer(expected);
            Engine<TestModel> engine = Engine.Create(model, Config());

            string actual = engine.Execute<TestModel, string>(query, "Ho");
            Assert.AreEqual(expected, actual);
        }

        private const string FirstCustomersNameStartingWithArg0 =
           @"(from customer in db.Customers " +
            "where customer.Name.StartsWith(@arg0) " +
            "orderby customer.Name " +
            "select customer.Name).First()";

        private const string ListOfCustomerNames =
            @"(from customer in db.Customers " +
            "select customer.Name).ToList()";

        string[] allQueries = new[]{
            FirstCustomersNameStartingWithArg0,
            ListOfCustomerNames};
    }
}
