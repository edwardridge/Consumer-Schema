using AutoFixture;
using AutoFixture.Kernel;
using ConsumerSchema.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsumerSchema.Checker
{
    public interface IGetSchemaDefinitions
    {
        IEnumerable<SchemaDefinition> GetSchemaDefinitions();
    }

    public class GetSchemaDefintionsFromFolder : IGetSchemaDefinitions
    {
        private readonly string folderPath;

        public GetSchemaDefintionsFromFolder(string folderPath)
        {
            this.folderPath = folderPath;
        }

        public IEnumerable<SchemaDefinition> GetSchemaDefinitions()
        {
            var schemaDefinitionFiles = Directory.GetFiles(this.folderPath, "*.schema.json");

            var schemaDefinitions = new List<SchemaDefinition>();

            foreach (var schemaDefinitionFile in schemaDefinitionFiles)
            {
                var schemaDefintionsFromFile = File.ReadAllText(schemaDefinitionFile);
                schemaDefinitions.Add(JsonConvert.DeserializeObject<SchemaDefinition>(schemaDefintionsFromFile));
            }

            return schemaDefinitions;
        }
    }

    public class SchemaChecker
    {
        private JSchemaReaderSettings settings;
        private IExampleGenerator exampleGenerator;

        public SchemaChecker()
        {
            this.settings = new JSchemaReaderSettings();
            this.exampleGenerator = new ExampleGenerator();
            //ED: Comment me out
            //this.settings.Validators = new List<JsonValidator>() { new GuidFormatValidator() };
        }
        
        public SchemaResults CheckSchemas(IGetSchemaDefinitions getSchemaDefinitions, Type[] typesOfMessagesToCheck)
        {
            var examples = this.GenerateExampleMessages(typesOfMessagesToCheck);

            var schemaDefinitions = getSchemaDefinitions.GetSchemaDefinitions();

            return CheckSchemaDefinitionsMatchExamples(schemaDefinitions, examples);
        }

        private IEnumerable<SchemaExample> GenerateExampleMessages(IEnumerable<Type> messageTypesToCheck)
        {
            var examples = new List<SchemaExample>();
            
            foreach (var messageTypeToCheck in messageTypesToCheck)
            {
                var example = this.exampleGenerator.GenerateExample(messageTypeToCheck);
                examples.Add(new SchemaExample()
                {
                    SchemaName = messageTypeToCheck.Name,
                    Example = example
                });
            }
            return examples;
        }

        private SchemaResults CheckSchemaDefinitionsMatchExamples(IEnumerable<SchemaDefinition> schemaDefinitions, IEnumerable<SchemaExample> examples)
        {
            var result = new SchemaResults();

            foreach (var schemaDefinition in schemaDefinitions)
            {
                result.AddResult(CheckSchemaAgainstExamples(schemaDefinition, examples));
            }
            
            return result;
        }

        private SchemaResult CheckSchemaAgainstExamples(SchemaDefinition schemaDefinition, IEnumerable<SchemaExample> examples)
        {
            var example = examples.FirstOrDefault(e => schemaDefinition.SchemaName == e.SchemaName);

            if (example == null)
            {
                return SchemaResult.CreateFailure(schemaDefinition.SchemaName, $"Cannot find example for {schemaDefinition.SchemaName}");
            }

            var schemaResult = CheckSchema(example.Example, schemaDefinition);

            return schemaResult;
        }

        private SchemaResult CheckSchema(JObject exampleJson, SchemaDefinition definition)
        {
            var jSchema = JSchema.Parse(definition.Schema.ToString(), settings);

            var isValid = exampleJson.IsValid(jSchema, out IList<string> errors);

            if (isValid)
            {
                return SchemaResult.CreateSuccess(definition.SchemaName, definition.ConsumerName);
            }

            return SchemaResult.CreateFailure(definition.SchemaName, definition.ConsumerName, errors.ToArray());
        }
    }

    public interface IExampleGenerator
    {
        JObject GenerateExample(Type type);
    }

    public class ExampleGenerator : IExampleGenerator
    {
        public JObject GenerateExample(Type type)
        {
            var fixture = new Fixture();

            var example = fixture.Create(type);
            var exampleAsJObect = JObject.FromObject(example);
            return exampleAsJObect;
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

    public class SchemaExample
    {
        public JObject Example { get; set; }

        public string SchemaName { get; set; }
    }
}
