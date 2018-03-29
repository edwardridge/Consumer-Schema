using System;
using System.IO;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using ConsumerSchema.Checker;
using GRM.Rights.Events.Clearance;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace JsonSchema.Tests
{
    public class ExampleGenerator
    {
        public static void GenerateExamples(string folderPath, params Type[] types)
        {
            var fixture = new Fixture();

            var examples = types.Select(fixture.Create).ToList();

            foreach (var example in examples)
            {
                var exampleJson = JObject.FromObject(example);

                File.WriteAllText(Path.Combine(folderPath, $"{example.GetType().Name}.json"), exampleJson.ToString());
            }
        }
    }

    public static class FixtureExtensions
    {
        public static object Create(this ISpecimenBuilder specimenBuilder, Type type)
        {
            var context = new SpecimenContext(specimenBuilder);
            return context.Resolve(type);
        }
    }
    
    public class TestSchemaDefinition
    {
        [Test]
        public void GenerateExampleJsonFiles()
        {
            ExampleGenerator.GenerateExamples(GenerateSchemaDefinitionsTest.PathOfSchemaDefinitionsFolder, typeof(SampleClearanceSignedEvent), typeof(SideArtistLabelWaiverClearanceSignedEvent));
        }

        [Test]
        public void TestSchemaDefintion()
        {
            var schemaResults = new SchemaChecker().CheckSchemas(GenerateSchemaDefinitionsTest.PathOfSchemaDefinitionsFolder);

            var hasErrors = schemaResults.HasErrors();

            Assert.IsFalse(hasErrors, $"Schema mismatches! {schemaResults.GetErrors()}");
        }
    }
}
