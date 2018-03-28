using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsumerSchema.Generator;
using GRM.Rights.Events.Clearance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NUnit.Framework;

namespace JsonSchema.Tests
{
    public class GenerateSchemaDefinitionsTest
    {

        public static string PathOfSchemaDefinitionsFolder = "D:/temp/SchemaDefinitions";

        [Test]
        public void GenerateSchemaDefinition()
        {
            var schemaGenerator = new SchemaGenerator();
            schemaGenerator.GenerateSchemaDefinitions("LPI Rights Service", PathOfSchemaDefinitionsFolder, typeof(SampleClearanceSignedEvent), typeof(SideArtistLabelWaiverClearanceSignedEvent));
        }
    }
    
    public class GuidSchemaGenerationProvider : JSchemaGenerationProvider
    {
        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            if (context.ObjectType == typeof(Guid))
            {
                return CreateSchemaWithFormat(context.ObjectType, context.Required, "guid");
            }

            return null;
        }

        private JSchema CreateSchemaWithFormat(Type type, Required required, string format)
        {
            var generator = new JSchemaGenerator();
            var schema = generator.Generate(type, required != Required.Always);
            schema.Format = format;

            return schema;
        }
    }
}
