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
    public class SchemaChecker
    {
        private JSchemaReaderSettings settings;

        public SchemaChecker()
        {
            this.settings = new JSchemaReaderSettings();

            //ED: Comment me out
            //this.settings.Validators = new List<JsonValidator>() { new GuidFormatValidator() };
        }

        internal SchemaResults CheckSchemasByProvidingDefinitions(IEnumerable<SchemaDefinition> schemaDefinitions, Type[] typesOfMessagesToCheck)
        {
            var examples = GenerateExampleMessages(typesOfMessagesToCheck);

            return CheckSchemaDefinitionsMatchExamples(schemaDefinitions, examples);
        }

        public SchemaResults CheckSchemas(string folderPath, Type[] typesOfMessagesToCheck)
        {
            var examples = GenerateExampleMessages(typesOfMessagesToCheck);

            return CheckSchemas(folderPath, examples);
        }
        
        internal SchemaResults CheckSchemas(string folderPath, IEnumerable<SchemaExample> examples)
        {
            var schemaDefinitions = GetSchemaDefinitions(folderPath);
            
            return CheckSchemaDefinitionsMatchExamples(schemaDefinitions, examples);
        }

        private static List<SchemaExample> GenerateExampleMessages(IEnumerable<Type> messageTypesToCheck)
        {
            var examples = new List<SchemaExample>();
            
            foreach (var messageTypeToCheck in messageTypesToCheck)
            {
                var example = ExampleGenerator.GenerateExample(messageTypeToCheck);
                examples.Add(new SchemaExample()
                {
                    SchemaName = messageTypeToCheck.Name,
                    Example = JObject.Parse(example)
                });
            }
            return examples;
        }

        private static List<SchemaDefinition> GetSchemaDefinitions(string folderPath)
        {
            var schemaDefinitionFiles = Directory.GetFiles(folderPath, "*.schema.json");

            var schemaDefinitions = new List<SchemaDefinition>();

            foreach (var schemaDefinitionFile in schemaDefinitionFiles)
            {
                var schemaDefintionsFromFile = File.ReadAllText(schemaDefinitionFile);
                schemaDefinitions.Add(JsonConvert.DeserializeObject<SchemaDefinition>(schemaDefintionsFromFile));
            }

            return schemaDefinitions;
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

    public class ExampleGenerator
    {
        public static string GenerateExample(Type type)
        {
            var fixture = new Fixture();

            var example = fixture.Create(type);
            var exampleAsJObect = JObject.FromObject(example);
            return exampleAsJObect.ToString();
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

    public class SchemaResults
    {
        private List<SchemaResult> schemaResults;

        public SchemaResults()
        {
            this.schemaResults = new List<SchemaResult>();
        }
        
        internal void AddResult(SchemaResult result)
        {
            this.schemaResults.Add(result);
        }

        public string GetErrorsSummary()
        {
            if (this.HasErrors())
            {
                return "";
            }
            else
            {
                return $"All valid for {this.Consumer}";
            }
        }

        public string Consumer { get; set; }

        public IEnumerable<string> GetErrors()
        {
            return schemaResults.Select(s => s.GetResult());
        }

        public bool HasErrors()
        {
            return schemaResults.Any(s => s.IsValid == false);
        }
    }

    public class SchemaResult
    {
        public string Class { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public bool IsValid { get; set; }

        public string Consumer { get; set; }

        public string GetResult()
        {
            string errors;
            if (this.Errors.Any())
            {
                errors = string.Join(",", this.Errors);
            }
            else
            {
                errors = "All valid!";
            }
            return $"Class Name: {this.Class}. Consumer: {this.Consumer}. Errors: {errors}";
        }

        public static SchemaResult CreateSuccess(string schemaName, string consumer)
        {
            return new SchemaResult()
            {
                Class = schemaName,
                IsValid = true,
                Consumer = consumer
            };
        }

        public static SchemaResult CreateFailure(string schemaName, string consumer, params string[] errors)
        {
            return new SchemaResult()
            {
                Errors = errors.ToList(),
                IsValid = false,
                Class = schemaName,
                Consumer = consumer
            };
        }
    }
}
