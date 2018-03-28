using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using ConsumerSchema.Checker;
using ConsumerSchema.Core;
using GRM.Rights.Events.Clearance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NUnit.Framework;
using UMG.EAI.DealMessagingClassLibrary.deal;

namespace JsonSchema.Tests
{
    public class ExampleGenerator
    {
        public static List<object> GetExamples()
        {
            var examples = new List<object>();

            var fixture = new Fixture();

            var sampleClearanceSignedEvent = fixture.Create<SampleClearanceSignedEvent>();
            var sideArtistEvent = fixture.Create<SideArtistLabelWaiverClearanceSignedEvent>();

            examples.Add(sampleClearanceSignedEvent);
            examples.Add(sideArtistEvent);

            return examples;
        }
    }

    public class TestSchemaDefinition
    {
        [Test]
        public void GenerateExampleJsonFiles()
        {
            var examples = ExampleGenerator.GetExamples();


            foreach (var example in examples)
            {
                var exampleJson = JObject.FromObject(example);

                File.WriteAllText(Path.Combine(GenerateSchemaDefinitionsTest.PathOfSchemaDefinitionsFolder, $"{example.GetType().Name}.json"), exampleJson.ToString());
            }
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
