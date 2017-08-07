using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MD.Framework.Utility.Core.Predicate
{
    public class ConditionTree 
    {
		public Guid Id { get; internal set; }

        public ConditionTree()
        {
            ChildrenConditions = new List<ConditionTree>();
        }

		public string SelectorString { get; set; }
		
		public OperatorEnum OperationType { get; set; }
		
		public string SerializedValue { get; set; }

        private object _value;
		public object Value {

            get => _value;

		    set
            {
                _value = value;
                SerializedValue = JsonConvert.SerializeObject(_value, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
            } 
        }

		public LogicalOperatorEnum NextLogicalOperator { get; set; }
		
		public List<ConditionTree> ChildrenConditions { get; set; }
    }

}
