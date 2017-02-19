using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MD.Framework.Utility
{
	[Serializable]
	[DataContract(IsReference = true)]
    public class ConditionTree 
    {
		[DataMember]
		public Guid Id { get; internal set; }

        public ConditionTree()
        {
            ChildrenConditions = new List<ConditionTree>();
        }

		[DataMember]
		public string SelectorString { get; set; }
		
		[DataMember]
		public OperatorEnum OperationType { get; set; }
		
		[DataMember]
		public string SerializedValue { get; set; }

        private object _value;
		public object Value {

            get
            {
				return _value;
            }
            
			set
            {
                _value = value;
                SerializedValue = JsonConvert.SerializeObject(_value, new JsonSerializerSettings
				{
					PreserveReferencesHandling = PreserveReferencesHandling.Objects
				}).ToString(CultureInfo.InvariantCulture);
            } 
        }

		[DataMember]
		public LogicalOperatorEnum NextLogicalOperator { get; set; }
		
		[DataMember]
		public List<ConditionTree> ChildrenConditions { get; set; }
    }

}
