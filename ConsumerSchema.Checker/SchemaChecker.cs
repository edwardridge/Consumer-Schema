using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsumerSchema.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

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

        public SchemaResults CheckSchemas(string folderPath)
        {
            var schemaDefinitions = GetSchemaDefinitions(folderPath);

            var examples = GetSchemaExamples(folderPath);

            return CheckSchemaDefinitionsMatchExamples(schemaDefinitions, examples);
        }

        private static List<SchemaExample> GetSchemaExamples(string folderPath)
        {
            var examples = new List<SchemaExample>();

            var exampleFiles = Directory.GetFiles(folderPath, "*.json").Where(w => w.Contains("schema") == false);

            foreach (var exampleFile in exampleFiles)
            {
                var exampleString = File.ReadAllText(exampleFile);
                examples.Add(new SchemaExample()
                {
                    SchemaName = Path.GetFileNameWithoutExtension(exampleFile),
                    Example = JObject.Parse(exampleString)
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

            var exampleJson = JObject.FromObject(example.Example);

            var schemaResult = CheckSchema(exampleJson, schemaDefinition);

            return schemaResult;
        }

        private SchemaResult CheckSchema(JObject exampleJson, SchemaDefinition definition)
        {
            var jSchema = JSchema.Parse(definition.Schema.ToString(), settings);

            var isValid = exampleJson.IsValid(jSchema, out IList<string> errors);

            if (isValid)
            {
                return SchemaResult.CreateSuccess(definition.SchemaName);
            }

            return SchemaResult.CreateFailure(definition.SchemaName, errors.ToArray());
        }
    }

    public class SchemaExample
    {
        public object Example { get; set; }

        public string SchemaName { get; set; }
    }

    public class SchemaResults
    {
        private List<SchemaResult> schemaResults;

        public SchemaResults()
        {
            this.schemaResults = new List<SchemaResult>();
        }

        public SchemaResults(List<SchemaResult> schemaResults)
        {
            this.schemaResults = schemaResults;
        }

        public void AddSuccess(string schemaName)
        {
            this.schemaResults.Add(SchemaResult.CreateSuccess(schemaName));
        }

        public void AddFailure(string schemaName, params string[] errors)
        {
            this.schemaResults.Add(SchemaResult.CreateFailure(schemaName, errors));
        }

        public void AddResult(SchemaResult result)
        {
            this.schemaResults.Add(result);
        }

        public string GetErrors()
        {
            return string.Join(",", schemaResults.SelectMany(s => s.Errors).ToList());
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

        public override string ToString()
        {
            return $"Class Name: {this.Class}. Errors: {string.Join(",", this.Errors)}";
        }

        public static SchemaResult CreateSuccess(string schemaName)
        {
            return new SchemaResult()
            {
                Class = schemaName,
                IsValid = true
            };
        }

        public static SchemaResult CreateFailure(string schemaName, params string[] errors)
        {
            return new SchemaResult()
            {
                Errors = errors.ToList(),
                IsValid = false,
                Class = schemaName
            };
        }
    }
}
