using Newtonsoft.Json.Schema;

namespace ConsumerSchema.Core
{
    public class SchemaDefinition
    {
        public JSchema Schema { get; set; }

        public string SchemaName { get; set; }

        public string ConsumerName { get; set; }
    }
}
