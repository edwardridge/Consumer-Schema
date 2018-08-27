using System;
using System.IO;
using System.Linq;
using ConsumerSchema.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;

namespace ConsumerSchema.Generator
{
    public class SchemaGenerator
    {
        JSchemaGenerator generator;

        public SchemaGenerator()
        {
            this.generator = new JSchemaGenerator();

            //ED: Comment me out
            //generator.GenerationProviders.Add(new GuidSchemaGenerationProvider());
        }

        public SchemaDefinition GenerateSchemaDefinition(string consumerName, Type type)
        {
            return CreateSchemaDefinition(type, consumerName);
        }

        public void GenerateSchemaDefinitionsToFile(string consumerName, string folderPath, params Type[] types)
        {
            var schemaDefinitions = types.Select(s => CreateSchemaDefinition(s, consumerName)).ToList();

            foreach (var schemaDefinition in schemaDefinitions)
            {
                var pathOfSchemaDefinition = Path.Combine(folderPath, $"{schemaDefinition.SchemaName}.schema.json");

                var json = JsonConvert.SerializeObject(schemaDefinition);

                File.WriteAllText(pathOfSchemaDefinition, json);
            }
        }

        private SchemaDefinition CreateSchemaDefinition(Type type, string consumerName)
        {
            var schema = generator.Generate(type);

            var schemaDefinition = new SchemaDefinition()
            {
                Schema = schema,
                SchemaName = type.Name,
                ConsumerName = consumerName
            };

            return schemaDefinition;
        }
    }
}
