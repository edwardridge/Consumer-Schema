﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Schema;

namespace ConsumerSchema.Checker
{
    public class SchemaResults
    {
        public List<SchemaResult> SchemaResultsList;

        public SchemaResults()
        {
            this.SchemaResultsList = new List<SchemaResult>();
        }
        
        internal void AddResult(SchemaResult result)
        {
            this.SchemaResultsList.Add(result);
        }

        public string GetErrorsSummary()
        {
            if (this.HasErrors())
            {
                return $@"Schema definition checking has failed. This indicates a recent refactoring would break a consumer of one of your messages. 
Errors: {string.Join(Environment.NewLine, this.GetErrors())}";
            }
            else
            {
                return $"All valid";
            }
        }

        public IEnumerable<string> GetErrors()
        {
            return SchemaResultsList.Select(s => s.GetResult());
        }

        public bool HasErrors()
        {
            return SchemaResultsList.Any(s => s.IsValid == false);
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

    public class SchemaDefinition
    {
        public JSchema Schema { get; set; }

        public string SchemaName { get; set; }

        public string ConsumerName { get; set; }
    }
}