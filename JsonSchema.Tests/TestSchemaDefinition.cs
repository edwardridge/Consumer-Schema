using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AutoFixture;
using AutoFixture.Kernel;
using ConsumerSchema.Checker;
using ConsumerSchema.Core;
using ConsumerSchema.Generator;
using GRM.Rights.Events.Clearance;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;

namespace JsonSchema.Tests
{
    public class TestSchemaDefinition
    {
        public string GetGeneratedSchemaWithOneStringProperty()
        {
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""PropertyOne"": {
      ""type"": [
        ""string"",
        ""null""
      ]
    }
  },
  ""required"": [
    ""PropertyOne""
  ]
}";
        }

        public string GetGeneratedSchemaWithOneSubclassProperty()
        {
            return @"{
  ""definitions"": {
    ""ThisIsTheSubclass"": {
      ""type"": [
        ""object"",
        ""null""
      ]
    }
  },
  ""type"": ""object"",
  ""properties"": {
    ""PropertyOne"": {
      ""$ref"": ""#/definitions/ThisIsTheSubclass""
    }
  },
  ""required"": [
    ""PropertyOne""
  ]
}

";
        }

        [Test]
        public void ShouldPass()
        {
            var generatedSchema = this.GetGeneratedSchemaWithOneStringProperty();
            
            var schemaDefinitions = new List<SchemaDefinition>()
            {
                new SchemaDefinition()
                {
                    Schema = JSchema.Parse(generatedSchema),
                    ConsumerName = "Test",
                    SchemaName = "ExampleMessageThatShouldPass"
                }
            };
            var schemaResults = new SchemaChecker().CheckSchemasByProvidingDefinitions(schemaDefinitions, new []{ typeof(ExampleMessageThatShouldPass) });

            var hasErrors = schemaResults.HasErrors();

            Assert.IsFalse(hasErrors, $"Schema mismatches! {schemaResults.GetErrors()}");
        }

        [Test]
        public void ShouldFailBecauseOfPropertyName()
        {
            var generatedSchema = this.GetGeneratedSchemaWithOneStringProperty();

            var schemaDefinitions = new List<SchemaDefinition>()
            {
                new SchemaDefinition()
                {
                    Schema = JSchema.Parse(generatedSchema),
                    ConsumerName = "Test",
                    SchemaName = typeof(ExampleMessageThatShouldFailBecauseOfPropertyName).Name
                }
            };
            var schemaResults = new SchemaChecker().CheckSchemasByProvidingDefinitions(schemaDefinitions, new[] { typeof(ExampleMessageThatShouldFailBecauseOfPropertyName) });

            var hasErrors = schemaResults.HasErrors();

            Assert.IsTrue(hasErrors);
            var errors = schemaResults.GetErrors();
            Assert.AreEqual(errors.First(), "Class Name: ExampleMessageThatShouldFailBecauseOfPropertyName. Consumer: Test. Errors: Required properties are missing from object: PropertyOne. Path '', line 1, position 1.");
        }

        [Test]
        public void ShouldFailBecauseOfPropertyType()
        {
            var generatedSchema = this.GetGeneratedSchemaWithOneStringProperty();

            var schemaDefinitions = GenerateTestSchemaDefinitions(generatedSchema, typeof(ExampleMessageThatShouldFailBecauseOfPropertyType));
            var schemaResults = new SchemaChecker().CheckSchemasByProvidingDefinitions(schemaDefinitions, new[] { typeof(ExampleMessageThatShouldFailBecauseOfPropertyType) });

            var hasErrors = schemaResults.HasErrors();

            Assert.IsTrue(hasErrors);
            var errors = schemaResults.GetErrors();
            var regexMatch = new Regex("Invalid type. Expected String, Null but got Integer. Path 'PropertyOne', line 2, position [19|20].");
            var result = regexMatch.Match(errors.First());
            Assert.AreEqual(result.Success, true);
        }

        [Test]
        public void ShouldFailBecauseSubclassIsMissing()
        {
            var schema = this.GetGeneratedSchemaWithOneSubclassProperty();
            var schemaDefinitions = GenerateTestSchemaDefinitions(schema, typeof(ExampleMessageThatShouldFailBecauseOfSubClass));
            var schemaResults = new SchemaChecker().CheckSchemasByProvidingDefinitions(schemaDefinitions, new[] { typeof(ExampleMessageThatShouldFailBecauseOfSubClass) });

            var hasErrors = schemaResults.HasErrors();

            Assert.IsTrue(hasErrors);
            var errors = schemaResults.GetErrors();
            Assert.AreEqual(errors.First(), "Class Name: ExampleMessageThatShouldFailBecauseOfSubClass. Consumer: Test. Errors: Required properties are missing from object: PropertyOne. Path '', line 1, position 1.");
        }

        [Test]
        public void ShouldPassWithSubclass()
        {
            var schema = this.GetGeneratedSchemaWithOneSubclassProperty();
            var schemaDefinitions = GenerateTestSchemaDefinitions(schema, typeof(ExampleMessageThatShouldPassWithSubClass));
            var schemaResults = new SchemaChecker().CheckSchemasByProvidingDefinitions(schemaDefinitions, new[] { typeof(ExampleMessageThatShouldPassWithSubClass) });

            var hasErrors = schemaResults.HasErrors();

            Assert.IsFalse(hasErrors);
        }

        private static List<SchemaDefinition> GenerateTestSchemaDefinitions(string generatedSchema, Type type)
        {
            var schemaDefinitions = new List<SchemaDefinition>()
            {
                new SchemaDefinition()
                {
                    Schema = JSchema.Parse(generatedSchema),
                    ConsumerName = "Test",
                    SchemaName = type.Name
                }
            };
            return schemaDefinitions;
        }

        public class ExampleMessageThatShouldPass
        {
            public string PropertyOne { get; set; }
        }

        public class ExampleMessageThatShouldFailBecauseOfPropertyName
        {
            public string PropertyTwo { get; set; }
        }

        public class ExampleMessageThatShouldFailBecauseOfPropertyType
        {
            public int PropertyOne { get; set; }
        }

        public class ExampleMessageThatShouldFailBecauseOfSubClass
        {
            //This is deliberately commented out to show that it is missing!
            //public ThisIsTheSubclass PropertyOne { get; set; }
        }

        public class ExampleMessageThatShouldPassWithSubClass
        {
            public ThisIsTheSubclass PropertyOne { get; set; }
        }

        public class ThisIsTheSubclass
        {

        }
    }
}
